using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Stocks;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class StocksController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static StockLineResponse MapStockToResponse(Stock s) => new()
    {
        StockId = s.StockId,
        ProductId = s.ProductId,
        ProductName = s.Product?.Name,
        SiteId = s.SiteId,
        SiteName = s.Site?.Name,
        SizeId = s.SizeId,
        SizeValue = s.Size?.SizeValue,
        VariantId = s.VariantId,
        Embroideries = s.Embroideries
            .OrderBy(e => e.Name)
            .Select(e => new StockEmbroideryInfo
            {
                EmbroideryId = e.EmbroideryId,
                Name = e.Name,
            })
            .ToList(),
        Quantity = s.Quantity,
        Barcode = s.Barcode,
        SerialNumber = s.SerialNumber,
        PriceSpecial = s.PriceSpecial,
        PriceId = s.PriceId,
        SaleDetailId = s.SaleDetailId,
        StockEntryId = s.StockEntryId,
        CreateDate = s.CreateDate,
    };

    private static StockExpenseResponse MapExpenseToResponse(StockExpense e) => new()
    {
        StockExpenseId = e.StockExpenseId,
        StockEntryId = e.StockEntryId,
        Amount = e.Amount,
        Comments = e.Comments,
    };

    private static StockEntryResponse MapEntryToResponse(StockEntry entry) => new()
    {
        StockEntryId = entry.StockEntryId,
        EntryDate = entry.EntryDate,
        UserId = entry.UserId,
        Comments = entry.Comments,
        StockCount = entry.Stocks.Count,
        ExpenseCount = entry.StockExpenses.Count,
        TotalExpenses = entry.StockExpenses.Sum(e => e.Amount),
        TotalQuantity = entry.Stocks.Sum(s => s.Quantity ?? 0),
        Stocks = entry.Stocks
            .OrderBy(s => s.StockId)
            .Select(MapStockToResponse)
            .ToList(),
        Expenses = entry.StockExpenses
            .OrderBy(e => e.StockExpenseId)
            .Select(MapExpenseToResponse)
            .ToList(),
    };

    private static StockEntrySummary MapEntryToSummary(StockEntry entry) => new()
    {
        StockEntryId = entry.StockEntryId,
        EntryDate = entry.EntryDate,
        UserId = entry.UserId,
        Comments = entry.Comments,
        StockCount = entry.Stocks.Count,
        ExpenseCount = entry.StockExpenses.Count,
        TotalExpenses = entry.StockExpenses.Sum(e => e.Amount),
        TotalQuantity = entry.Stocks.Sum(s => s.Quantity ?? 0),
    };

    // Base query with all includes for stock lines
    private IQueryable<Stock> StockBaseQuery() =>
        db.Stocks
            .Include(s => s.Product)
            .Include(s => s.Site)
            .Include(s => s.Size)
            .Include(s => s.Embroideries);

    // ═══════════════════════════════════════════════════════════════════
    //  STOCK ENTRY (header) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET list (paginated) ─────────────────

    /// <summary>Returns a paginated list of stock entries.</summary>
    [HttpGet("entries")]
    [ProducesResponseType(typeof(PagedResponse<StockEntrySummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StockEntrySummary>>> GetEntries(
        [FromQuery] StockEntryQueryParameters query,
        CancellationToken ct)
    {
        var q = db.StockEntries
            .Include(e => e.Stocks)
            .Include(e => e.StockExpenses)
            .AsNoTracking()
            .AsQueryable();

        // Filters
        if (query.FromDate.HasValue)
            q = q.Where(e => e.EntryDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            q = q.Where(e => e.EntryDate <= query.ToDate.Value);
        if (!string.IsNullOrWhiteSpace(query.UserId))
            q = q.Where(e => e.UserId == query.UserId);

        // Search (comments)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var t = term;
                q = q.Where(e => e.Comments != null && e.Comments.ToLower().Contains(t));
            }
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "entrydate" => query.SortDescending
                ? q.OrderByDescending(e => e.EntryDate)
                : q.OrderBy(e => e.EntryDate),
            "stockcount" => query.SortDescending
                ? q.OrderByDescending(e => e.Stocks.Count)
                : q.OrderBy(e => e.Stocks.Count),
            _ => q.OrderByDescending(e => e.EntryDate),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(new PagedResponse<StockEntrySummary>
        {
            Items = items.Select(MapEntryToSummary),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET entry by id ─────────────────────

    /// <summary>Returns a single stock entry with its stocks and expenses.</summary>
    [HttpGet("entries/{id:int}")]
    [ProducesResponseType(typeof(StockEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockEntryResponse>> GetEntryById(int id, CancellationToken ct)
    {
        var entry = await db.StockEntries
            .Include(e => e.Stocks).ThenInclude(s => s.Product)
            .Include(e => e.Stocks).ThenInclude(s => s.Site)
            .Include(e => e.Stocks).ThenInclude(s => s.Size)
            .Include(e => e.Stocks).ThenInclude(s => s.Embroideries)
            .Include(e => e.StockExpenses)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StockEntryId == id, ct);

        if (entry is null)
            return NotFound(new { message = $"Entrada de stock con ID {id} no encontrada." });

        return Ok(MapEntryToResponse(entry));
    }

    // ───────────────────────── POST create entry ───────────────────

    /// <summary>Creates a new stock entry.</summary>
    [HttpPost("entries")]
    [ProducesResponseType(typeof(StockEntryResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<StockEntryResponse>> CreateEntry(
        [FromBody] CreateStockEntryRequest request,
        CancellationToken ct)
    {
        var entity = new StockEntry
        {
            EntryDate = request.EntryDate ?? DateTime.UtcNow,
            UserId = User.Identity?.Name ?? "system",
            Comments = request.Comments,
        };

        db.StockEntries.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Collection(e => e.Stocks).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.StockExpenses).LoadAsync(ct);

        return CreatedAtAction(nameof(GetEntryById), new { id = entity.StockEntryId }, MapEntryToResponse(entity));
    }

    // ───────────────────────── PUT update entry ────────────────────

    /// <summary>Updates an existing stock entry header.</summary>
    [HttpPut("entries/{id:int}")]
    [ProducesResponseType(typeof(StockEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockEntryResponse>> UpdateEntry(
        int id,
        [FromBody] UpdateStockEntryRequest request,
        CancellationToken ct)
    {
        var entity = await db.StockEntries
            .Include(e => e.Stocks).ThenInclude(s => s.Product)
            .Include(e => e.Stocks).ThenInclude(s => s.Site)
            .Include(e => e.Stocks).ThenInclude(s => s.Size)
            .Include(e => e.Stocks).ThenInclude(s => s.Embroideries)
            .Include(e => e.StockExpenses)
            .FirstOrDefaultAsync(e => e.StockEntryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Entrada de stock con ID {id} no encontrada." });

        if (request.EntryDate.HasValue)
            entity.EntryDate = request.EntryDate.Value;
        entity.Comments = request.Comments;

        await db.SaveChangesAsync(ct);

        return Ok(MapEntryToResponse(entity));
    }

    // ───────────────────────── DELETE entry ─────────────────────────

    /// <summary>Deletes a stock entry and all its stocks and expenses.</summary>
    [HttpDelete("entries/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(int id, CancellationToken ct)
    {
        var entity = await db.StockEntries
            .Include(e => e.Stocks)
            .Include(e => e.StockExpenses)
            .FirstOrDefaultAsync(e => e.StockEntryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Entrada de stock con ID {id} no encontrada." });

        db.StockExpenses.RemoveRange(entity.StockExpenses);
        db.Stocks.RemoveRange(entity.Stocks);
        db.StockEntries.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STOCK (inventory lines) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET stocks (paginated) ───────────────

    /// <summary>Returns a paginated, filterable list of stock lines.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<StockLineResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StockLineResponse>>> GetStocks(
        [FromQuery] StockQueryParameters query,
        CancellationToken ct)
    {
        var q = StockBaseQuery()
            .AsNoTracking()
            .AsQueryable();

        // Filters
        if (query.ProductId.HasValue)
            q = q.Where(s => s.ProductId == query.ProductId.Value);
        if (query.SiteId.HasValue)
            q = q.Where(s => s.SiteId == query.SiteId.Value);
        if (query.SizeId.HasValue)
            q = q.Where(s => s.SizeId == query.SizeId.Value);
        if (query.EmbroideryId.HasValue)
            q = q.Where(s => s.Embroideries.Any(e => e.EmbroideryId == query.EmbroideryId.Value));
        if (query.StockEntryId.HasValue)
            q = q.Where(s => s.StockEntryId == query.StockEntryId.Value);
        if (query.HasQuantity == true)
            q = q.Where(s => s.Quantity > 0);
        else if (query.HasQuantity == false)
            q = q.Where(s => s.Quantity == null || s.Quantity <= 0);

        // Search (product name, barcode)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var t = term;
                q = q.Where(s =>
                    (s.Product.Name != null && s.Product.Name.ToLower().Contains(t)) ||
                    s.Barcode.ToLower().Contains(t));
            }
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "product" => query.SortDescending
                ? q.OrderByDescending(s => s.Product.Name)
                : q.OrderBy(s => s.Product.Name),
            "site" => query.SortDescending
                ? q.OrderByDescending(s => s.Site.Name)
                : q.OrderBy(s => s.Site.Name),
            "quantity" => query.SortDescending
                ? q.OrderByDescending(s => s.Quantity)
                : q.OrderBy(s => s.Quantity),
            "createdate" => query.SortDescending
                ? q.OrderByDescending(s => s.CreateDate)
                : q.OrderBy(s => s.CreateDate),
            "barcode" => query.SortDescending
                ? q.OrderByDescending(s => s.Barcode)
                : q.OrderBy(s => s.Barcode),
            _ => q.OrderByDescending(s => s.CreateDate),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => MapStockToResponse(s))
            .ToListAsync(ct);

        return Ok(new PagedResponse<StockLineResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET stock by id ─────────────────────

    /// <summary>Returns a single stock line.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(StockLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockLineResponse>> GetStockById(int id, CancellationToken ct)
    {
        var stock = await StockBaseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StockId == id, ct);

        if (stock is null)
            return NotFound(new { message = $"Stock con ID {id} no encontrado." });

        return Ok(MapStockToResponse(stock));
    }

    // ───────────────────────── POST create stock ───────────────────

    /// <summary>Creates a new stock line, optionally linked to a stock entry.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(StockLineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StockLineResponse>> CreateStock(
        [FromBody] CreateStockRequest request,
        CancellationToken ct)
    {
        // Validate product
        var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId, ct);
        if (!productExists)
            return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });

        // Validate site
        var siteExists = await db.Sites.AnyAsync(s => s.SiteId == request.SiteId, ct);
        if (!siteExists)
            return UnprocessableEntity(new { message = $"Sitio con ID {request.SiteId} no existe." });

        // Validate size (optional)
        if (request.SizeId.HasValue)
        {
            var sizeExists = await db.Sizes.AnyAsync(s => s.SizeId == request.SizeId.Value, ct);
            if (!sizeExists)
                return UnprocessableEntity(new { message = $"Talla con ID {request.SizeId} no existe." });
        }

        // Validate embroideries (optional, M2M)
        if (request.EmbroideryIds.Count > 0)
        {
            var existingEmbIds = await db.Embroideries
                .Where(e => request.EmbroideryIds.Contains(e.EmbroideryId))
                .Select(e => e.EmbroideryId)
                .ToListAsync(ct);
            var missingEmb = request.EmbroideryIds.Except(existingEmbIds).ToList();
            if (missingEmb.Count > 0)
                return UnprocessableEntity(new { message = $"Bordados no encontrados: {string.Join(", ", missingEmb)}" });
        }

        var entity = new Stock
        {
            ProductId = request.ProductId,
            SiteId = request.SiteId,
            SizeId = request.SizeId,
            VariantId = request.VariantId,
            Quantity = request.Quantity,
            Barcode = request.Barcode ?? "",
            SerialNumber = request.SerialNumber,
            PriceSpecial = request.PriceSpecial,
            PriceId = request.PriceId,
            CreateDate = DateTime.UtcNow,
        };

        // Associate embroideries (M2M)
        if (request.EmbroideryIds.Count > 0)
        {
            var embroideries = await db.Embroideries
                .Where(e => request.EmbroideryIds.Contains(e.EmbroideryId))
                .ToListAsync(ct);
            entity.Embroideries = embroideries;
        }

        db.Stocks.Add(entity);
        await db.SaveChangesAsync(ct);

        // Reload navigations
        await db.Entry(entity).Reference(e => e.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(e => e.Site).LoadAsync(ct);
        if (entity.SizeId.HasValue) await db.Entry(entity).Reference(e => e.Size).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.Embroideries).LoadAsync(ct);

        return CreatedAtAction(nameof(GetStockById), new { id = entity.StockId }, MapStockToResponse(entity));
    }

    // ───────────────────────── PUT update stock ────────────────────

    /// <summary>Updates an existing stock line.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StockLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StockLineResponse>> UpdateStock(
        int id,
        [FromBody] UpdateStockRequest request,
        CancellationToken ct)
    {
        var entity = await StockBaseQuery()
            .FirstOrDefaultAsync(s => s.StockId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Stock con ID {id} no encontrado." });

        // Validate product
        if (request.ProductId != entity.ProductId)
        {
            var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId, ct);
            if (!productExists)
                return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });
        }

        // Validate site
        if (request.SiteId != entity.SiteId)
        {
            var siteExists = await db.Sites.AnyAsync(s => s.SiteId == request.SiteId, ct);
            if (!siteExists)
                return UnprocessableEntity(new { message = $"Sitio con ID {request.SiteId} no existe." });
        }

        // Validate size (optional)
        if (request.SizeId.HasValue && request.SizeId != entity.SizeId)
        {
            var sizeExists = await db.Sizes.AnyAsync(s => s.SizeId == request.SizeId.Value, ct);
            if (!sizeExists)
                return UnprocessableEntity(new { message = $"Talla con ID {request.SizeId} no existe." });
        }

        // Validate embroideries (optional, M2M)
        if (request.EmbroideryIds.Count > 0)
        {
            var existingEmbIds = await db.Embroideries
                .Where(e => request.EmbroideryIds.Contains(e.EmbroideryId))
                .Select(e => e.EmbroideryId)
                .ToListAsync(ct);
            var missingEmb = request.EmbroideryIds.Except(existingEmbIds).ToList();
            if (missingEmb.Count > 0)
                return UnprocessableEntity(new { message = $"Bordados no encontrados: {string.Join(", ", missingEmb)}" });
        }

        entity.ProductId = request.ProductId;
        entity.SiteId = request.SiteId;
        entity.SizeId = request.SizeId;
        entity.VariantId = request.VariantId;
        entity.Quantity = request.Quantity;
        entity.Barcode = request.Barcode ?? entity.Barcode;
        entity.SerialNumber = request.SerialNumber;
        entity.PriceSpecial = request.PriceSpecial;
        entity.PriceId = request.PriceId;

        // Update embroideries (replace all — M2M)
        entity.Embroideries.Clear();
        if (request.EmbroideryIds.Count > 0)
        {
            var embroideries = await db.Embroideries
                .Where(e => request.EmbroideryIds.Contains(e.EmbroideryId))
                .ToListAsync(ct);
            foreach (var emb in embroideries)
                entity.Embroideries.Add(emb);
        }

        await db.SaveChangesAsync(ct);

        // Reload navigations in case FKs changed
        await db.Entry(entity).Reference(e => e.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(e => e.Site).LoadAsync(ct);
        if (entity.SizeId.HasValue) await db.Entry(entity).Reference(e => e.Size).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.Embroideries).LoadAsync(ct);

        return Ok(MapStockToResponse(entity));
    }

    // ───────────────────────── DELETE stock ─────────────────────────

    /// <summary>Deletes a stock line.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStock(int id, CancellationToken ct)
    {
        var entity = await db.Stocks.FirstOrDefaultAsync(s => s.StockId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Stock con ID {id} no encontrado." });

        db.Stocks.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STOCK (lines) scoped under an entry
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET stocks for entry ─────────────────

    /// <summary>Returns all stock lines for a specific entry.</summary>
    [HttpGet("entries/{entryId:int}/stocks")]
    [ProducesResponseType(typeof(List<StockLineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<StockLineResponse>>> GetStocksForEntry(
        int entryId, CancellationToken ct)
    {
        var entryExists = await db.StockEntries.AnyAsync(e => e.StockEntryId == entryId, ct);
        if (!entryExists)
            return NotFound(new { message = $"Entrada de stock con ID {entryId} no encontrada." });

        var items = await StockBaseQuery()
            .AsNoTracking()
            .Where(s => s.StockEntryId == entryId)
            .OrderBy(s => s.StockId)
            .Select(s => MapStockToResponse(s))
            .ToListAsync(ct);

        return Ok(items);
    }

    // ───────────────────────── POST create stock under entry ────────

    /// <summary>Adds a new stock line to a specific entry.</summary>
    [HttpPost("entries/{entryId:int}/stocks")]
    [ProducesResponseType(typeof(StockLineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StockLineResponse>> CreateStockForEntry(
        int entryId,
        [FromBody] CreateStockRequest request,
        CancellationToken ct)
    {
        var entryExists = await db.StockEntries.AnyAsync(e => e.StockEntryId == entryId, ct);
        if (!entryExists)
            return NotFound(new { message = $"Entrada de stock con ID {entryId} no encontrada." });

        // Validate product
        var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId, ct);
        if (!productExists)
            return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });

        // Validate site
        var siteExists = await db.Sites.AnyAsync(s => s.SiteId == request.SiteId, ct);
        if (!siteExists)
            return UnprocessableEntity(new { message = $"Sitio con ID {request.SiteId} no existe." });

        // Validate embroideries (optional, M2M)
        if (request.EmbroideryIds.Count > 0)
        {
            var existingEmbIds = await db.Embroideries
                .Where(e => request.EmbroideryIds.Contains(e.EmbroideryId))
                .Select(e => e.EmbroideryId)
                .ToListAsync(ct);
            var missingEmb = request.EmbroideryIds.Except(existingEmbIds).ToList();
            if (missingEmb.Count > 0)
                return UnprocessableEntity(new { message = $"Bordados no encontrados: {string.Join(", ", missingEmb)}" });
        }

        var entity = new Stock
        {
            StockEntryId = entryId,
            ProductId = request.ProductId,
            SiteId = request.SiteId,
            SizeId = request.SizeId,
            VariantId = request.VariantId,
            Quantity = request.Quantity,
            Barcode = request.Barcode ?? "",
            SerialNumber = request.SerialNumber,
            PriceSpecial = request.PriceSpecial,
            PriceId = request.PriceId,
            CreateDate = DateTime.UtcNow,
        };

        // Associate embroideries (M2M)
        if (request.EmbroideryIds.Count > 0)
        {
            var embroideries = await db.Embroideries
                .Where(e => request.EmbroideryIds.Contains(e.EmbroideryId))
                .ToListAsync(ct);
            entity.Embroideries = embroideries;
        }

        db.Stocks.Add(entity);
        await db.SaveChangesAsync(ct);

        // Reload navigations
        await db.Entry(entity).Reference(e => e.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(e => e.Site).LoadAsync(ct);
        if (entity.SizeId.HasValue) await db.Entry(entity).Reference(e => e.Size).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.Embroideries).LoadAsync(ct);

        return CreatedAtAction(nameof(GetStockById), new { id = entity.StockId }, MapStockToResponse(entity));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STOCK EXPENSE endpoints (scoped under entry)
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET expenses for entry ───────────────

    /// <summary>Returns all expenses for a specific stock entry.</summary>
    [HttpGet("entries/{entryId:int}/expenses")]
    [ProducesResponseType(typeof(List<StockExpenseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<StockExpenseResponse>>> GetExpenses(
        int entryId, CancellationToken ct)
    {
        var entryExists = await db.StockEntries.AnyAsync(e => e.StockEntryId == entryId, ct);
        if (!entryExists)
            return NotFound(new { message = $"Entrada de stock con ID {entryId} no encontrada." });

        var items = await db.StockExpenses
            .AsNoTracking()
            .Where(e => e.StockEntryId == entryId)
            .OrderBy(e => e.StockExpenseId)
            .Select(e => MapExpenseToResponse(e))
            .ToListAsync(ct);

        return Ok(items);
    }

    // ───────────────────────── GET expense by id ───────────────────

    /// <summary>Returns a single expense.</summary>
    [HttpGet("entries/{entryId:int}/expenses/{expenseId:int}")]
    [ProducesResponseType(typeof(StockExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockExpenseResponse>> GetExpenseById(
        int entryId, int expenseId, CancellationToken ct)
    {
        var expense = await db.StockExpenses
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.StockExpenseId == expenseId && e.StockEntryId == entryId, ct);

        if (expense is null)
            return NotFound(new { message = $"Gasto con ID {expenseId} no encontrado en la entrada {entryId}." });

        return Ok(MapExpenseToResponse(expense));
    }

    // ───────────────────────── POST create expense ─────────────────

    /// <summary>Adds a new expense to a stock entry.</summary>
    [HttpPost("entries/{entryId:int}/expenses")]
    [ProducesResponseType(typeof(StockExpenseResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockExpenseResponse>> CreateExpense(
        int entryId,
        [FromBody] CreateStockExpenseRequest request,
        CancellationToken ct)
    {
        var entryExists = await db.StockEntries.AnyAsync(e => e.StockEntryId == entryId, ct);
        if (!entryExists)
            return NotFound(new { message = $"Entrada de stock con ID {entryId} no encontrada." });

        var entity = new StockExpense
        {
            StockEntryId = entryId,
            Amount = request.Amount,
            Comments = request.Comments,
        };

        db.StockExpenses.Add(entity);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetExpenseById),
            new { entryId, expenseId = entity.StockExpenseId },
            MapExpenseToResponse(entity));
    }

    // ───────────────────────── PUT update expense ──────────────────

    /// <summary>Updates an existing expense.</summary>
    [HttpPut("entries/{entryId:int}/expenses/{expenseId:int}")]
    [ProducesResponseType(typeof(StockExpenseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockExpenseResponse>> UpdateExpense(
        int entryId, int expenseId,
        [FromBody] UpdateStockExpenseRequest request,
        CancellationToken ct)
    {
        var entity = await db.StockExpenses
            .FirstOrDefaultAsync(e => e.StockExpenseId == expenseId && e.StockEntryId == entryId, ct);

        if (entity is null)
            return NotFound(new { message = $"Gasto con ID {expenseId} no encontrado en la entrada {entryId}." });

        entity.Amount = request.Amount;
        entity.Comments = request.Comments;

        await db.SaveChangesAsync(ct);

        return Ok(MapExpenseToResponse(entity));
    }

    // ───────────────────────── DELETE expense ───────────────────────

    /// <summary>Deletes an expense from a stock entry.</summary>
    [HttpDelete("entries/{entryId:int}/expenses/{expenseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense(
        int entryId, int expenseId, CancellationToken ct)
    {
        var entity = await db.StockExpenses
            .FirstOrDefaultAsync(e => e.StockExpenseId == expenseId && e.StockEntryId == entryId, ct);

        if (entity is null)
            return NotFound(new { message = $"Gasto con ID {expenseId} no encontrado en la entrada {entryId}." });

        db.StockExpenses.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
