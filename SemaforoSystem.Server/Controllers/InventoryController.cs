using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Inventory;
using SemaforoSystem.Server.Models;
using SemaforoSystem.Server.Services;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class InventoryController(InventoryService inventoryService, ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  ENTRY (Goods Receipt) — delegates to InventoryService
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("entries")]
    [ProducesResponseType(typeof(InventoryTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEntry(
        [FromBody] CreateEntryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        for (int i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!line.InventoryItemDefinitionId.HasValue && !line.ProductId.HasValue)
                ModelState.AddModelError($"Lines[{i}]",
                    "Either InventoryItemDefinitionId or ProductId must be provided.");
        }
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await inventoryService.CreateEntryAsync(request, userId, ct);
            return CreatedAtAction(nameof(GetTransaction),
                new { id = result.InventoryTransactionId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GENERIC TRANSACTION (ADJUSTMENT, LOSS, DAMAGE, TRANSFER, etc.)
    // ═══════════════════════════════════════════════════════════════════

    private static readonly HashSet<string> AllowedTransactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "INITIAL_LOAD", "PURCHASE_IN", "SALE_OUT",
        "TRANSFER_OUT", "TRANSFER_IN",
        "ADJUSTMENT", "LOSS", "DAMAGE", "RETURN_IN"
    };

    [HttpPost("transactions")]
    [ProducesResponseType(typeof(InventoryTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] CreateTransactionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!AllowedTransactionTypes.Contains(request.TransactionType))
        {
            ModelState.AddModelError(nameof(request.TransactionType),
                $"Invalid transaction type. Allowed: {string.Join(", ", AllowedTransactionTypes)}");
            return BadRequest(ModelState);
        }

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var header = new InventoryTransaction
            {
                TransactionType = request.TransactionType.ToUpperInvariant(),
                TransactionDate = DateTime.UtcNow,
                Reference = request.Reference,
                Comments = request.Comments,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };
            db.InventoryTransactions.Add(header);
            await db.SaveChangesAsync(ct);

            var response = new InventoryTransactionResponse
            {
                InventoryTransactionId = header.InventoryTransactionId,
                TransactionType = header.TransactionType,
                TransactionDate = header.TransactionDate,
                Reference = header.Reference,
                Comments = header.Comments,
                UserId = header.UserId,
                Lines = [],
            };

            foreach (var lineReq in request.Lines)
            {
                var definition = await db.InventoryItemDefinitions
                    .FirstOrDefaultAsync(d => d.InventoryItemDefinitionId == lineReq.InventoryItemDefinitionId, ct)
                    ?? throw new KeyNotFoundException(
                        $"InventoryItemDefinition {lineReq.InventoryItemDefinitionId} not found.");

                // Negative stock check for outgoing movements
                if (lineReq.QtyDelta < 0)
                {
                    var balance = await db.InventoryBalances
                        .FirstOrDefaultAsync(b => b.SiteId == request.SiteId
                            && b.InventoryItemDefinitionId == lineReq.InventoryItemDefinitionId, ct);

                    int available = (balance?.OnHand ?? 0) - (balance?.Reserved ?? 0);
                    if (available < Math.Abs(lineReq.QtyDelta))
                        throw new InvalidOperationException(
                            $"Insufficient stock for {definition.SkuCode}. Available: {available}, requested: {Math.Abs(lineReq.QtyDelta)}");
                }

                var ledgerLine = new InventoryTransactionLine
                {
                    InventoryTransactionId = header.InventoryTransactionId,
                    SiteId = request.SiteId,
                    InventoryItemDefinitionId = lineReq.InventoryItemDefinitionId,
                    QtyDelta = lineReq.QtyDelta,
                    UnitCost = lineReq.UnitCost,
                    UnitPrice = lineReq.UnitPrice,
                    SourceSiteId = lineReq.SourceSiteId,
                    TargetSiteId = lineReq.TargetSiteId,
                };
                db.InventoryTransactionLines.Add(ledgerLine);
                await db.SaveChangesAsync(ct);

                var lineResponse = new InventoryTransactionLineResponse
                {
                    InventoryTransactionLineId = ledgerLine.InventoryTransactionLineId,
                    SiteId = request.SiteId,
                    InventoryItemDefinitionId = definition.InventoryItemDefinitionId,
                    SkuCode = definition.SkuCode,
                    NameSnapshot = definition.NameSnapshot,
                    QtyDelta = ledgerLine.QtyDelta,
                    UnitCost = ledgerLine.UnitCost,
                    UnitPrice = ledgerLine.UnitPrice,
                    SourceSiteId = ledgerLine.SourceSiteId,
                    TargetSiteId = ledgerLine.TargetSiteId,
                };

                // Handle serialized items if barcodes provided
                if (lineReq.Barcodes is { Count: > 0 })
                {
                    if (lineReq.Barcodes.Count != Math.Abs(lineReq.QtyDelta))
                        throw new InvalidOperationException(
                            $"Barcodes count ({lineReq.Barcodes.Count}) must match |QtyDelta| ({Math.Abs(lineReq.QtyDelta)}).");

                    lineResponse.SerialItems = [];

                    foreach (var barcode in lineReq.Barcodes)
                    {
                        var serial = await db.InventorySerialItems
                            .FirstOrDefaultAsync(s => s.Barcode == barcode, ct)
                            ?? throw new KeyNotFoundException($"Serial item with barcode '{barcode}' not found.");

                        // Link to ledger line
                        ledgerLine.InventorySerialItems.Add(serial);

                        // Update serial status/site based on transaction type
                        if (lineReq.QtyDelta < 0) // outgoing
                        {
                            if (lineReq.TargetSiteId.HasValue)
                                serial.CurrentSiteId = lineReq.TargetSiteId.Value;
                        }

                        lineResponse.SerialItems.Add(new SerialItemInfo
                        {
                            InventorySerialItemId = serial.InventorySerialItemId,
                            Barcode = serial.Barcode,
                            SerialNumber = serial.SerialNumber,
                            Status = serial.Status,
                        });
                    }
                    await db.SaveChangesAsync(ct);
                }

                // Update balance cache
                await UpsertBalanceAsync(request.SiteId, lineReq.InventoryItemDefinitionId,
                    lineReq.QtyDelta, 0, ct);

                response.Lines.Add(lineResponse);
            }

            await tx.CommitAsync(ct);
            return CreatedAtAction(nameof(GetTransaction),
                new { id = response.InventoryTransactionId }, response);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRANSACTIONS — Read
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(PagedResponse<InventoryTransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] TransactionQueryParams q, CancellationToken ct)
    {
        var query = db.InventoryTransactions
            .Include(t => t.InventoryTransactionLines)
                .ThenInclude(l => l.InventoryItemDefinition)
            .Include(t => t.InventoryTransactionLines)
                .ThenInclude(l => l.Site)
            .AsQueryable();

        if (!string.IsNullOrEmpty(q.TransactionType))
            query = query.Where(t => t.TransactionType == q.TransactionType);
        if (q.From.HasValue)
            query = query.Where(t => t.TransactionDate >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(t => t.TransactionDate <= q.To.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
            query = query.Where(t => (t.Reference != null && t.Reference.Contains(q.Search))
                || (t.Comments != null && t.Comments.Contains(q.Search)));

        query = (q.SortBy?.ToLowerInvariant()) switch
        {
            "reference" => q.SortDescending ? query.OrderByDescending(t => t.Reference) : query.OrderBy(t => t.Reference),
            "type" => q.SortDescending ? query.OrderByDescending(t => t.TransactionType) : query.OrderBy(t => t.TransactionType),
            _ => q.SortDescending ? query.OrderByDescending(t => t.TransactionDate) : query.OrderBy(t => t.TransactionDate),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        return Ok(new PagedResponse<InventoryTransactionResponse>
        {
            Items = items.Select(MapTransaction),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount,
        });
    }

    [HttpGet("transactions/{id:long}")]
    [ProducesResponseType(typeof(InventoryTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(long id, CancellationToken ct)
    {
        var result = await inventoryService.GetTransactionAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BALANCES
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("balances")]
    [ProducesResponseType(typeof(PagedResponse<BalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalances(
        [FromQuery] BalanceQueryParams q, CancellationToken ct)
    {
        var query = db.InventoryBalances
            .Include(b => b.Site)
            .Include(b => b.InventoryItemDefinition)
                .ThenInclude(d => d.Product)
            .Include(b => b.InventoryItemDefinition)
                .ThenInclude(d => d.Size)
            .AsQueryable();

        if (q.SiteId.HasValue)
            query = query.Where(b => b.SiteId == q.SiteId.Value);
        if (q.ProductId.HasValue)
            query = query.Where(b => b.InventoryItemDefinition.ProductId == q.ProductId.Value);
        if (q.IsSerialized.HasValue)
            query = query.Where(b => b.InventoryItemDefinition.IsSerialized == q.IsSerialized.Value);
        if (q.IsActive.HasValue)
            query = query.Where(b => b.InventoryItemDefinition.IsActive == q.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(b =>
                (b.InventoryItemDefinition.SkuCode != null && b.InventoryItemDefinition.SkuCode.ToLower().Contains(s))
                || (b.InventoryItemDefinition.NameSnapshot != null && b.InventoryItemDefinition.NameSnapshot.ToLower().Contains(s))
                || (b.InventoryItemDefinition.Product.Name != null && b.InventoryItemDefinition.Product.Name.ToLower().Contains(s))
                || b.Site.Name.ToLower().Contains(s));
        }

        query = (q.SortBy?.ToLowerInvariant()) switch
        {
            "site" => q.SortDescending ? query.OrderByDescending(b => b.Site.Name) : query.OrderBy(b => b.Site.Name),
            "sku" => q.SortDescending ? query.OrderByDescending(b => b.InventoryItemDefinition.SkuCode) : query.OrderBy(b => b.InventoryItemDefinition.SkuCode),
            "onhand" => q.SortDescending ? query.OrderByDescending(b => b.OnHand) : query.OrderBy(b => b.OnHand),
            "product" => q.SortDescending ? query.OrderByDescending(b => b.InventoryItemDefinition.Product.Name) : query.OrderBy(b => b.InventoryItemDefinition.Product.Name),
            _ => q.SortDescending ? query.OrderByDescending(b => b.UpdatedAt) : query.OrderBy(b => b.UpdatedAt),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        return Ok(new PagedResponse<BalanceResponse>
        {
            Items = items.Select(b => new BalanceResponse
            {
                SiteId = b.SiteId,
                SiteName = b.Site.Name,
                InventoryItemDefinitionId = b.InventoryItemDefinitionId,
                SkuCode = b.InventoryItemDefinition.SkuCode,
                NameSnapshot = b.InventoryItemDefinition.NameSnapshot,
                ProductId = b.InventoryItemDefinition.ProductId,
                ProductName = b.InventoryItemDefinition.Product?.Name,
                SizeId = b.InventoryItemDefinition.SizeId,
                SizeName = b.InventoryItemDefinition.Size?.SizeValue,
                IsSerialized = b.InventoryItemDefinition.IsSerialized,
                OnHand = b.OnHand ?? 0,
                Reserved = b.Reserved ?? 0,
                UpdatedAt = b.UpdatedAt,
            }),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount,
        });
    }

    [HttpGet("items/{itemDefinitionId:int}/balances")]
    [ProducesResponseType(typeof(IEnumerable<BalanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemBalances(int itemDefinitionId,
        [FromQuery] int? siteId, CancellationToken ct)
    {
        var query = db.InventoryBalances
            .Include(b => b.Site)
            .Include(b => b.InventoryItemDefinition)
                .ThenInclude(d => d.Product)
            .Include(b => b.InventoryItemDefinition)
                .ThenInclude(d => d.Size)
            .Where(b => b.InventoryItemDefinitionId == itemDefinitionId);

        if (siteId.HasValue)
            query = query.Where(b => b.SiteId == siteId.Value);

        var items = await query.OrderBy(b => b.Site.Name).ToListAsync(ct);

        return Ok(items.Select(b => new BalanceResponse
        {
            SiteId = b.SiteId,
            SiteName = b.Site.Name,
            InventoryItemDefinitionId = b.InventoryItemDefinitionId,
            SkuCode = b.InventoryItemDefinition.SkuCode,
            NameSnapshot = b.InventoryItemDefinition.NameSnapshot,
            ProductId = b.InventoryItemDefinition.ProductId,
            ProductName = b.InventoryItemDefinition.Product?.Name,
            SizeId = b.InventoryItemDefinition.SizeId,
            SizeName = b.InventoryItemDefinition.Size?.SizeValue,
            IsSerialized = b.InventoryItemDefinition.IsSerialized,
            OnHand = b.OnHand ?? 0,
            Reserved = b.Reserved ?? 0,
            UpdatedAt = b.UpdatedAt,
        }));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LEDGER (Kardex)
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("ledger")]
    [ProducesResponseType(typeof(PagedResponse<InventoryTransactionLineResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedger(
        [FromQuery] LedgerQueryParams q, CancellationToken ct)
    {
        var query = db.InventoryTransactionLines
            .Include(l => l.InventoryTransaction)
            .Include(l => l.InventoryItemDefinition)
            .Include(l => l.Site)
            .AsQueryable();

        if (q.SiteId.HasValue)
            query = query.Where(l => l.SiteId == q.SiteId.Value);
        if (q.InventoryItemDefinitionId.HasValue)
            query = query.Where(l => l.InventoryItemDefinitionId == q.InventoryItemDefinitionId.Value);
        if (!string.IsNullOrEmpty(q.TransactionType))
            query = query.Where(l => l.InventoryTransaction.TransactionType == q.TransactionType);
        if (q.From.HasValue)
            query = query.Where(l => l.InventoryTransaction.TransactionDate >= q.From.Value);
        if (q.To.HasValue)
            query = query.Where(l => l.InventoryTransaction.TransactionDate <= q.To.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(l =>
                (l.InventoryItemDefinition.SkuCode != null && l.InventoryItemDefinition.SkuCode.ToLower().Contains(s))
                || (l.InventoryItemDefinition.NameSnapshot != null && l.InventoryItemDefinition.NameSnapshot.ToLower().Contains(s))
                || (l.InventoryTransaction.Reference != null && l.InventoryTransaction.Reference.ToLower().Contains(s)));
        }

        query = (q.SortBy?.ToLowerInvariant()) switch
        {
            "sku" => q.SortDescending ? query.OrderByDescending(l => l.InventoryItemDefinition.SkuCode) : query.OrderBy(l => l.InventoryItemDefinition.SkuCode),
            "qty" => q.SortDescending ? query.OrderByDescending(l => l.QtyDelta) : query.OrderBy(l => l.QtyDelta),
            _ => q.SortDescending
                ? query.OrderByDescending(l => l.InventoryTransaction.TransactionDate)
                : query.OrderBy(l => l.InventoryTransaction.TransactionDate),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        return Ok(new PagedResponse<InventoryTransactionLineResponse>
        {
            Items = items.Select(l => new InventoryTransactionLineResponse
            {
                InventoryTransactionLineId = l.InventoryTransactionLineId,
                SiteId = l.SiteId,
                SiteName = l.Site?.Name,
                InventoryItemDefinitionId = l.InventoryItemDefinitionId,
                SkuCode = l.InventoryItemDefinition?.SkuCode,
                NameSnapshot = l.InventoryItemDefinition?.NameSnapshot,
                QtyDelta = l.QtyDelta,
                UnitCost = l.UnitCost,
                UnitPrice = l.UnitPrice,
                SourceSiteId = l.SourceSiteId,
                TargetSiteId = l.TargetSiteId,
            }),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount,
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ITEM DEFINITIONS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("items")]
    [ProducesResponseType(typeof(PagedResponse<ItemDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemDefinitions(
        [FromQuery] BalanceQueryParams q, CancellationToken ct)
    {
        var query = db.InventoryItemDefinitions
            .Include(d => d.Product)
            .Include(d => d.Size)
            .Include(d => d.ProductVariants)
            .AsQueryable();

        if (q.ProductId.HasValue)
            query = query.Where(d => d.ProductId == q.ProductId.Value);
        if (q.IsSerialized.HasValue)
            query = query.Where(d => d.IsSerialized == q.IsSerialized.Value);
        if (q.IsActive.HasValue)
            query = query.Where(d => d.IsActive == q.IsActive.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(d =>
                (d.SkuCode != null && d.SkuCode.ToLower().Contains(s))
                || (d.NameSnapshot != null && d.NameSnapshot.ToLower().Contains(s))
                || (d.Product.Name != null && d.Product.Name.ToLower().Contains(s)));
        }

        query = (q.SortBy?.ToLowerInvariant()) switch
        {
            "sku" => q.SortDescending ? query.OrderByDescending(d => d.SkuCode) : query.OrderBy(d => d.SkuCode),
            "product" => q.SortDescending ? query.OrderByDescending(d => d.Product.Name) : query.OrderBy(d => d.Product.Name),
            _ => q.SortDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        return Ok(new PagedResponse<ItemDefinitionResponse>
        {
            Items = items.Select(MapItemDefinition),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount,
        });
    }

    [HttpGet("items/{id:int}")]
    [ProducesResponseType(typeof(ItemDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItemDefinition(int id, CancellationToken ct)
    {
        var def = await db.InventoryItemDefinitions
            .Include(d => d.Product)
            .Include(d => d.Size)
            .Include(d => d.ProductVariants)
            .FirstOrDefaultAsync(d => d.InventoryItemDefinitionId == id, ct);

        return def is null ? NotFound() : Ok(MapItemDefinition(def));
    }

    [HttpGet("items/lookup")]
    [ProducesResponseType(typeof(IEnumerable<ItemDefinitionLookup>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemDefinitionsLookup(
        [FromQuery] int? productId, CancellationToken ct)
    {
        var query = db.InventoryItemDefinitions
            .Where(d => d.IsActive);

        if (productId.HasValue)
            query = query.Where(d => d.ProductId == productId.Value);

        var items = await query
            .OrderBy(d => d.SkuCode)
            .Select(d => new ItemDefinitionLookup
            {
                InventoryItemDefinitionId = d.InventoryItemDefinitionId,
                SkuCode = d.SkuCode,
                NameSnapshot = d.NameSnapshot,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SERIAL ITEMS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("serial")]
    [ProducesResponseType(typeof(PagedResponse<SerialItemInfo>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSerialItems(
        [FromQuery] SerialItemQueryParams q, CancellationToken ct)
    {
        var query = db.InventorySerialItems
            .Include(s => s.InventoryItemDefinition)
            .Include(s => s.CurrentSite)
            .AsQueryable();

        if (q.SiteId.HasValue)
            query = query.Where(s => s.CurrentSiteId == q.SiteId.Value);
        if (q.InventoryItemDefinitionId.HasValue)
            query = query.Where(s => s.InventoryItemDefinitionId == q.InventoryItemDefinitionId.Value);
        if (q.Status.HasValue)
            query = query.Where(s => s.Status == q.Status.Value);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.ToLower();
            query = query.Where(si => si.Barcode.ToLower().Contains(s)
                || (si.InventoryItemDefinition.NameSnapshot != null
                    && si.InventoryItemDefinition.NameSnapshot.ToLower().Contains(s)));
        }

        query = (q.SortBy?.ToLowerInvariant()) switch
        {
            "barcode" => q.SortDescending ? query.OrderByDescending(s => s.Barcode) : query.OrderBy(s => s.Barcode),
            "status" => q.SortDescending ? query.OrderByDescending(s => s.Status) : query.OrderBy(s => s.Status),
            _ => q.SortDescending ? query.OrderByDescending(s => s.CreatedAt) : query.OrderBy(s => s.CreatedAt),
        };

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);

        return Ok(new PagedResponse<SerialItemInfo>
        {
            Items = items.Select(s => new SerialItemInfo
            {
                InventorySerialItemId = s.InventorySerialItemId,
                Barcode = s.Barcode,
                SerialNumber = s.SerialNumber,
                Status = s.Status,
            }),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount,
        });
    }

    [HttpGet("serial/{barcode}")]
    [ProducesResponseType(typeof(SerialItemDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSerialByBarcode(string barcode, CancellationToken ct)
    {
        var serial = await db.InventorySerialItems
            .Include(s => s.InventoryItemDefinition)
            .Include(s => s.CurrentSite)
            .Include(s => s.InventoryTransactionLines)
                .ThenInclude(l => l.InventoryTransaction)
            .Include(s => s.InventoryTransactionLines)
                .ThenInclude(l => l.Site)
            .FirstOrDefaultAsync(s => s.Barcode == barcode, ct);

        if (serial is null)
            return NotFound(new { error = $"Serial item with barcode '{barcode}' not found." });

        return Ok(new SerialItemDetailResponse
        {
            InventorySerialItemId = serial.InventorySerialItemId,
            InventoryItemDefinitionId = serial.InventoryItemDefinitionId,
            SkuCode = serial.InventoryItemDefinition.SkuCode,
            NameSnapshot = serial.InventoryItemDefinition.NameSnapshot,
            CurrentSiteId = serial.CurrentSiteId,
            CurrentSiteName = serial.CurrentSite.Name,
            Barcode = serial.Barcode,
            SerialNumber = serial.SerialNumber,
            Status = serial.Status,
            CreatedAt = serial.CreatedAt,
            DeactivatedAt = serial.DeactivatedAt,
            Moves = serial.InventoryTransactionLines
                .OrderByDescending(l => l.InventoryTransaction.TransactionDate)
                .Select(l => new SerialMoveInfo
                {
                    InventoryTransactionLineId = l.InventoryTransactionLineId,
                    InventoryTransactionId = l.InventoryTransactionId,
                    TransactionType = l.InventoryTransaction.TransactionType,
                    TransactionDate = l.InventoryTransaction.TransactionDate,
                    Reference = l.InventoryTransaction.Reference,
                    SiteId = l.SiteId,
                    SiteName = l.Site?.Name,
                    QtyDelta = l.QtyDelta,
                }).ToList(),
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  RESERVATIONS
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("reservations")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateReservation(
        [FromBody] CreateReservationRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            // Validate stock availability
            var balance = await db.InventoryBalances
                .FirstOrDefaultAsync(b => b.SiteId == request.SiteId
                    && b.InventoryItemDefinitionId == request.InventoryItemDefinitionId, ct);

            int available = (balance?.OnHand ?? 0) - (balance?.Reserved ?? 0);
            if (available < request.Quantity)
                return BadRequest(new { error = $"Insufficient available stock. Available: {available}, requested: {request.Quantity}" });

            var definition = await db.InventoryItemDefinitions
                .FirstOrDefaultAsync(d => d.InventoryItemDefinitionId == request.InventoryItemDefinitionId, ct)
                ?? throw new KeyNotFoundException($"InventoryItemDefinition {request.InventoryItemDefinitionId} not found.");

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var reservation = new InventoryReservation
            {
                SiteId = request.SiteId,
                InventoryItemDefinitionId = request.InventoryItemDefinitionId,
                Quantity = request.Quantity,
                SaleOrderId = request.SaleOrderId,
                Status = 1, // Active
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresInMinutes.HasValue
                    ? DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes.Value)
                    : null,
            };
            db.InventoryReservations.Add(reservation);
            await db.SaveChangesAsync(ct);

            // Link serial items if provided
            List<long>? reservedSerialIds = null;
            if (request.SerialItemIds is { Count: > 0 })
            {
                if (request.SerialItemIds.Count != request.Quantity)
                    throw new InvalidOperationException(
                        $"SerialItemIds count ({request.SerialItemIds.Count}) must match Quantity ({request.Quantity}).");

                reservedSerialIds = [];
                foreach (var serialId in request.SerialItemIds)
                {
                    var serial = await db.InventorySerialItems.FindAsync([serialId], ct)
                        ?? throw new KeyNotFoundException($"Serial item {serialId} not found.");

                    if (serial.CurrentSiteId != request.SiteId)
                        throw new InvalidOperationException($"Serial item {serialId} is not at site {request.SiteId}.");
                    if (serial.Status != 1) // Not Available
                        throw new InvalidOperationException($"Serial item {serialId} is not available (status: {serial.Status}).");

                    serial.Status = 2; // Reserved
                    reservation.InventorySerialItems.Add(serial);
                    reservedSerialIds.Add(serialId);
                }
                await db.SaveChangesAsync(ct);
            }

            // Update balance reserved count
            await UpsertBalanceAsync(request.SiteId, request.InventoryItemDefinitionId,
                onHandDelta: 0, reservedDelta: request.Quantity, ct);

            await tx.CommitAsync(ct);

            return CreatedAtAction(nameof(GetReservation),
                new { id = reservation.InventoryReservationId },
                new ReservationResponse
                {
                    InventoryReservationId = reservation.InventoryReservationId,
                    SiteId = reservation.SiteId,
                    InventoryItemDefinitionId = reservation.InventoryItemDefinitionId,
                    SkuCode = definition.SkuCode,
                    NameSnapshot = definition.NameSnapshot,
                    Quantity = reservation.Quantity,
                    SaleOrderId = reservation.SaleOrderId,
                    Status = reservation.Status,
                    CreatedAt = reservation.CreatedAt,
                    ExpiresAt = reservation.ExpiresAt,
                    ReservedSerialItemIds = reservedSerialIds,
                });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("reservations/{id:long}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReservation(long id, CancellationToken ct)
    {
        var r = await db.InventoryReservations
            .Include(r => r.Site)
            .Include(r => r.InventoryItemDefinition)
            .Include(r => r.InventorySerialItems)
            .FirstOrDefaultAsync(r => r.InventoryReservationId == id, ct);

        if (r is null) return NotFound();

        return Ok(new ReservationResponse
        {
            InventoryReservationId = r.InventoryReservationId,
            SiteId = r.SiteId,
            SiteName = r.Site.Name,
            InventoryItemDefinitionId = r.InventoryItemDefinitionId,
            SkuCode = r.InventoryItemDefinition.SkuCode,
            NameSnapshot = r.InventoryItemDefinition.NameSnapshot,
            Quantity = r.Quantity,
            SaleOrderId = r.SaleOrderId,
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            ExpiresAt = r.ExpiresAt,
            ReservedSerialItemIds = r.InventorySerialItems.Select(s => s.InventorySerialItemId).ToList(),
        });
    }

    [HttpPost("reservations/{id:long}/commit")]
    [ProducesResponseType(typeof(InventoryTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitReservation(long id, CancellationToken ct)
    {
        var reservation = await db.InventoryReservations
            .Include(r => r.InventoryItemDefinition)
            .Include(r => r.InventorySerialItems)
            .FirstOrDefaultAsync(r => r.InventoryReservationId == id, ct);

        if (reservation is null) return NotFound();
        if (reservation.Status != 1)
            return BadRequest(new { error = $"Reservation is not active (status: {reservation.Status})." });

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            // Create SALE_OUT transaction
            var header = new InventoryTransaction
            {
                TransactionType = "SALE_OUT",
                TransactionDate = DateTime.UtcNow,
                Reference = $"RES-{reservation.InventoryReservationId}",
                Comments = reservation.SaleOrderId.HasValue
                    ? $"Commit reservation for sale order {reservation.SaleOrderId}"
                    : $"Commit reservation {reservation.InventoryReservationId}",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
            };
            db.InventoryTransactions.Add(header);
            await db.SaveChangesAsync(ct);

            var ledgerLine = new InventoryTransactionLine
            {
                InventoryTransactionId = header.InventoryTransactionId,
                SiteId = reservation.SiteId,
                InventoryItemDefinitionId = reservation.InventoryItemDefinitionId,
                QtyDelta = -reservation.Quantity,
            };
            db.InventoryTransactionLines.Add(ledgerLine);
            await db.SaveChangesAsync(ct);

            // Handle serial items
            foreach (var serial in reservation.InventorySerialItems)
            {
                serial.Status = 3; // Sold
                ledgerLine.InventorySerialItems.Add(serial);
            }
            await db.SaveChangesAsync(ct);

            // Update reservation status
            reservation.Status = 2; // Committed

            // Update balance: reduce on_hand and reserved
            await UpsertBalanceAsync(reservation.SiteId, reservation.InventoryItemDefinitionId,
                onHandDelta: -reservation.Quantity, reservedDelta: -reservation.Quantity, ct);

            await tx.CommitAsync(ct);

            var result = await inventoryService.GetTransactionAsync(header.InventoryTransactionId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("reservations/{id:long}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelReservation(long id, CancellationToken ct)
    {
        var reservation = await db.InventoryReservations
            .Include(r => r.InventorySerialItems)
            .FirstOrDefaultAsync(r => r.InventoryReservationId == id, ct);

        if (reservation is null) return NotFound();
        if (reservation.Status != 1)
            return BadRequest(new { error = $"Reservation is not active (status: {reservation.Status})." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Release serial items
        foreach (var serial in reservation.InventorySerialItems)
            serial.Status = 1; // Available

        reservation.Status = 3; // Cancelled

        // Decrease reserved count
        await UpsertBalanceAsync(reservation.SiteId, reservation.InventoryItemDefinitionId,
            onHandDelta: 0, reservedDelta: -reservation.Quantity, ct);

        await tx.CommitAsync(ct);

        return Ok(new { message = "Reservation cancelled." });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LOOKUPS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("sites")]
    [ProducesResponseType(typeof(IEnumerable<SiteLookup>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSites(CancellationToken ct)
    {
        var sites = await db.Sites
            .OrderBy(s => s.Name)
            .Select(s => new SiteLookup { SiteId = s.SiteId, Name = s.Name })
            .ToListAsync(ct);
        return Ok(sites);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════════════

    private async Task UpsertBalanceAsync(
        int siteId, int definitionId, int onHandDelta, int reservedDelta, CancellationToken ct)
    {
        var balance = await db.InventoryBalances
            .FirstOrDefaultAsync(b => b.SiteId == siteId
                && b.InventoryItemDefinitionId == definitionId, ct);

        if (balance is null)
        {
            balance = new InventoryBalance
            {
                SiteId = siteId,
                InventoryItemDefinitionId = definitionId,
                OnHand = onHandDelta,
                Reserved = reservedDelta,
                UpdatedAt = DateTime.UtcNow,
            };
            db.InventoryBalances.Add(balance);
        }
        else
        {
            balance.OnHand = (balance.OnHand ?? 0) + onHandDelta;
            balance.Reserved = (balance.Reserved ?? 0) + reservedDelta;
            balance.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static InventoryTransactionResponse MapTransaction(InventoryTransaction t) => new()
    {
        InventoryTransactionId = t.InventoryTransactionId,
        TransactionType = t.TransactionType,
        TransactionDate = t.TransactionDate,
        Reference = t.Reference,
        Comments = t.Comments,
        UserId = t.UserId,
        Lines = t.InventoryTransactionLines.Select(l => new InventoryTransactionLineResponse
        {
            InventoryTransactionLineId = l.InventoryTransactionLineId,
            SiteId = l.SiteId,
            SiteName = l.Site?.Name,
            InventoryItemDefinitionId = l.InventoryItemDefinitionId,
            SkuCode = l.InventoryItemDefinition?.SkuCode,
            NameSnapshot = l.InventoryItemDefinition?.NameSnapshot,
            QtyDelta = l.QtyDelta,
            UnitCost = l.UnitCost,
            UnitPrice = l.UnitPrice,
            SourceSiteId = l.SourceSiteId,
            TargetSiteId = l.TargetSiteId,
        }).ToList(),
    };

    private static ItemDefinitionResponse MapItemDefinition(InventoryItemDefinition d) => new()
    {
        InventoryItemDefinitionId = d.InventoryItemDefinitionId,
        ProductId = d.ProductId,
        ProductName = d.Product?.Name,
        SizeId = d.SizeId,
        SizeName = d.Size?.SizeValue,
        IsSerialized = d.IsSerialized,
        SkuCode = d.SkuCode,
        NameSnapshot = d.NameSnapshot,
        IsActive = d.IsActive,
        ProductVisualDefinitionId = d.ProductVisualDefinitionId,
        CreatedAt = d.CreatedAt,
        Variants = d.ProductVariants.Select(v => new VariantInfo
        {
            ProductVariantId = v.ProductVariantId,
        }).ToList(),
    };
}
