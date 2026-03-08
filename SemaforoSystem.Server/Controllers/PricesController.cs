using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Pricing;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

/// <summary>
/// Price resolution, setting base prices, managing promos, and price history.
/// Follows the pricing guide: scope specificity (ITEM_DEFINITION → VISUAL_DEFINITION → PRODUCT),
/// time-versioned entries, and PROMO > BASE resolution.
/// </summary>
[ApiController]
[Route("api/prices")]
//[Authorize]
[Produces("application/json")]
public class PricesController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Effective Price (read)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the effective price for a specific inventory item definition.
    /// Falls back through ITEM_DEFINITION → VISUAL_DEFINITION → PRODUCT scopes.
    /// </summary>
    [HttpGet("effective")]
    [ProducesResponseType(typeof(EffectivePriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EffectivePriceResponse>> GetEffectivePrice(
        [FromQuery] int priceListId,
        [FromQuery] int itemDefinitionId,
        [FromQuery] DateTime? asOf,
        CancellationToken ct)
    {
        var now = asOf ?? DateTime.UtcNow;

        // Load the item definition to know its product & visual definition
        var itemDef = await db.InventoryItemDefinitions
            .AsNoTracking()
            .Where(d => d.InventoryItemDefinitionId == itemDefinitionId)
            .Select(d => new
            {
                d.InventoryItemDefinitionId,
                d.ProductId,
                d.ProductVisualDefinitionId,
            })
            .FirstOrDefaultAsync(ct);

        if (itemDef is null)
            return NotFound(new { message = $"Item definition con ID {itemDefinitionId} no encontrado." });

        // ── Scope 1: ITEM_DEFINITION ──
        var winner = await ResolveAtScope(
            db.PriceItemDefinitionEntries
                .Where(e => e.PriceListId == priceListId
                         && e.InventoryItemDefinitionId == itemDefinitionId),
            now, ct);

        string scope = "ITEM_DEFINITION";

        // ── Scope 2: VISUAL_DEFINITION (fallback) ──
        if (winner is null && itemDef.ProductVisualDefinitionId.HasValue)
        {
            winner = await ResolveAtScope(
                db.PriceVisualDefinitionEntries
                    .Where(e => e.PriceListId == priceListId
                             && e.ProductVisualDefinitionId == itemDef.ProductVisualDefinitionId.Value),
                now, ct);
            scope = "VISUAL_DEFINITION";
        }

        // ── Scope 3: PRODUCT (fallback) ──
        if (winner is null)
        {
            winner = await ResolveAtScope(
                db.PriceProductEntries
                    .Where(e => e.PriceListId == priceListId
                             && e.ProductId == itemDef.ProductId),
                now, ct);
            scope = "PRODUCT";
        }

        if (winner is null)
            return NotFound(new { message = "No se encontró un precio vigente para esta configuración." });

        var response = new EffectivePriceResponse
        {
            PriceAmount = winner.PriceAmount,
            PriceKind = winner.PriceKind,
            Scope = scope,
            PriceEntryId = winner.PriceEntryId,
            PromoName = winner.PromoName,
            PromoCode = winner.PromoCode,
            ValidFrom = winner.ValidFrom,
            ValidTo = winner.ValidTo,
        };

        // If winner is PROMO, also return the BASE price for strikethrough
        if (winner.PriceKind == "PROMO")
        {
            response.BasePriceAmount = await FindBasePrice(
                scope, itemDef.InventoryItemDefinitionId, itemDef.ProductVisualDefinitionId, itemDef.ProductId, priceListId, now, ct);
        }

        return Ok(response);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Set Base Price (write)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Sets a new BASE price for a target. Closes the previous active BASE
    /// for the same target and list (sets valid_to = now).
    /// </summary>
    [HttpPost("base")]
    [ProducesResponseType(typeof(PriceEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PriceEntryResponse>> SetBasePrice(
        [FromBody] SetBasePriceRequest request,
        CancellationToken ct)
    {
        var (scope, error) = ValidateExactlyOneTarget(request.ProductId, request.ProductVisualDefinitionId, request.InventoryItemDefinitionId);
        if (error is not null) return BadRequest(new { message = error });

        // Validate price list
        if (!await db.PriceLists.AnyAsync(pl => pl.PriceListId == request.PriceListId, ct))
            return UnprocessableEntity(new { message = $"Lista de precios con ID {request.PriceListId} no encontrada." });

        // Validate target FK
        var fkError = await ValidateTargetExists(scope!, request.ProductId, request.ProductVisualDefinitionId, request.InventoryItemDefinitionId, ct);
        if (fkError is not null) return UnprocessableEntity(new { message = fkError });

        var now = DateTime.UtcNow;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Close previous BASE
        switch (scope)
        {
            case "PRODUCT":
                await db.PriceProductEntries
                    .Where(e => e.PriceListId == request.PriceListId
                             && e.ProductId == request.ProductId
                             && e.PriceKind == "BASE"
                             && e.ValidTo == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.ValidTo, now), ct);
                break;

            case "VISUAL_DEFINITION":
                await db.PriceVisualDefinitionEntries
                    .Where(e => e.PriceListId == request.PriceListId
                             && e.ProductVisualDefinitionId == request.ProductVisualDefinitionId
                             && e.PriceKind == "BASE"
                             && e.ValidTo == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.ValidTo, now), ct);
                break;

            case "ITEM_DEFINITION":
                await db.PriceItemDefinitionEntries
                    .Where(e => e.PriceListId == request.PriceListId
                             && e.InventoryItemDefinitionId == request.InventoryItemDefinitionId
                             && e.PriceKind == "BASE"
                             && e.ValidTo == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.ValidTo, now), ct);
                break;
        }

        // Insert new BASE
        long entryId = scope switch
        {
            "PRODUCT" => await InsertProductEntry(request.PriceListId, request.ProductId!.Value,
                request.PriceAmount, "BASE", 0, null, null, now, null, userId, request.Reason, ct),
            "VISUAL_DEFINITION" => await InsertVisualDefinitionEntry(request.PriceListId, request.ProductVisualDefinitionId!.Value,
                request.PriceAmount, "BASE", 0, null, null, now, null, userId, request.Reason, ct),
            "ITEM_DEFINITION" => await InsertItemDefinitionEntry(request.PriceListId, request.InventoryItemDefinitionId!.Value,
                request.PriceAmount, "BASE", 0, null, null, now, null, userId, request.Reason, ct),
            _ => throw new InvalidOperationException(),
        };

        await tx.CommitAsync(ct);

        var response = await LoadEntryResponse(scope!, entryId, ct);
        return CreatedAtAction(null, response);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Add Promo (write)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Adds a PROMO price entry with explicit validity window.
    /// Does not modify existing BASE entries.
    /// </summary>
    [HttpPost("promo")]
    [ProducesResponseType(typeof(PriceEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<PriceEntryResponse>> AddPromo(
        [FromBody] AddPromoPriceRequest request,
        CancellationToken ct)
    {
        var (scope, error) = ValidateExactlyOneTarget(request.ProductId, request.ProductVisualDefinitionId, request.InventoryItemDefinitionId);
        if (error is not null) return BadRequest(new { message = error });

        if (request.ValidTo <= request.ValidFrom)
            return BadRequest(new { message = "valid_to debe ser posterior a valid_from." });

        // Validate price list
        if (!await db.PriceLists.AnyAsync(pl => pl.PriceListId == request.PriceListId, ct))
            return UnprocessableEntity(new { message = $"Lista de precios con ID {request.PriceListId} no encontrada." });

        // Validate target FK
        var fkError = await ValidateTargetExists(scope!, request.ProductId, request.ProductVisualDefinitionId, request.InventoryItemDefinitionId, ct);
        if (fkError is not null) return UnprocessableEntity(new { message = fkError });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        long entryId = scope switch
        {
            "PRODUCT" => await InsertProductEntry(request.PriceListId, request.ProductId!.Value,
                request.PriceAmount, "PROMO", request.Priority, request.PromoName, request.PromoCode,
                request.ValidFrom, request.ValidTo, userId, request.Reason, ct),
            "VISUAL_DEFINITION" => await InsertVisualDefinitionEntry(request.PriceListId, request.ProductVisualDefinitionId!.Value,
                request.PriceAmount, "PROMO", request.Priority, request.PromoName, request.PromoCode,
                request.ValidFrom, request.ValidTo, userId, request.Reason, ct),
            "ITEM_DEFINITION" => await InsertItemDefinitionEntry(request.PriceListId, request.InventoryItemDefinitionId!.Value,
                request.PriceAmount, "PROMO", request.Priority, request.PromoName, request.PromoCode,
                request.ValidFrom, request.ValidTo, userId, request.Reason, ct),
            _ => throw new InvalidOperationException(),
        };

        var response = await LoadEntryResponse(scope!, entryId, ct);
        return CreatedAtAction(null, response);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Cancel Promo (sets valid_to = now)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cancels an active PROMO by setting its valid_to to now.
    /// </summary>
    [HttpDelete("promo/{scope}/{priceEntryId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPromo(string scope, long priceEntryId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        switch (scope.ToUpperInvariant())
        {
            case "PRODUCT":
            {
                var entry = await db.PriceProductEntries.FindAsync([priceEntryId], ct);
                if (entry is null) return NotFound(new { message = "Entrada de precio no encontrada." });
                if (entry.PriceKind != "PROMO") return BadRequest(new { message = "Solo se pueden cancelar entradas de tipo PROMO." });
                entry.ValidTo = now;
                break;
            }
            case "VISUAL_DEFINITION":
            {
                var entry = await db.PriceVisualDefinitionEntries.FindAsync([priceEntryId], ct);
                if (entry is null) return NotFound(new { message = "Entrada de precio no encontrada." });
                if (entry.PriceKind != "PROMO") return BadRequest(new { message = "Solo se pueden cancelar entradas de tipo PROMO." });
                entry.ValidTo = now;
                break;
            }
            case "ITEM_DEFINITION":
            {
                var entry = await db.PriceItemDefinitionEntries.FindAsync([priceEntryId], ct);
                if (entry is null) return NotFound(new { message = "Entrada de precio no encontrada." });
                if (entry.PriceKind != "PROMO") return BadRequest(new { message = "Solo se pueden cancelar entradas de tipo PROMO." });
                entry.ValidTo = now;
                break;
            }
            default:
                return BadRequest(new { message = "Scope inválido. Use PRODUCT, VISUAL_DEFINITION o ITEM_DEFINITION." });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Price History
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns paginated price history for a given scope + target.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResponse<PriceEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResponse<PriceEntryResponse>>> GetHistory(
        [FromQuery] PriceHistoryQuery query,
        CancellationToken ct)
    {
        switch (query.Scope.ToUpperInvariant())
        {
            case "PRODUCT":
            {
                var q = db.PriceProductEntries
                    .Include(e => e.PriceList)
                    .Include(e => e.Product)
                    .AsNoTracking()
                    .Where(e => e.ProductId == (int)query.TargetId);

                if (query.PriceListId.HasValue)
                    q = q.Where(e => e.PriceListId == query.PriceListId.Value);

                var total = await q.CountAsync(ct);

                var items = await q
                    .OrderByDescending(e => e.ValidFrom)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return Ok(new PagedResponse<PriceEntryResponse>
                {
                    Items = items.Select(e => MapProductEntry(e)),
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = total,
                });
            }

            case "VISUAL_DEFINITION":
            {
                var q = db.PriceVisualDefinitionEntries
                    .Include(e => e.PriceList)
                    .Include(e => e.ProductVisualDefinition)
                    .AsNoTracking()
                    .Where(e => e.ProductVisualDefinitionId == query.TargetId);

                if (query.PriceListId.HasValue)
                    q = q.Where(e => e.PriceListId == query.PriceListId.Value);

                var total = await q.CountAsync(ct);

                var items = await q
                    .OrderByDescending(e => e.ValidFrom)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return Ok(new PagedResponse<PriceEntryResponse>
                {
                    Items = items.Select(e => MapVisualDefinitionEntry(e)),
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = total,
                });
            }

            case "ITEM_DEFINITION":
            {
                var q = db.PriceItemDefinitionEntries
                    .Include(e => e.PriceList)
                    .Include(e => e.InventoryItemDefinition)
                    .AsNoTracking()
                    .Where(e => e.InventoryItemDefinitionId == (int)query.TargetId);

                if (query.PriceListId.HasValue)
                    q = q.Where(e => e.PriceListId == query.PriceListId.Value);

                var total = await q.CountAsync(ct);

                var items = await q
                    .OrderByDescending(e => e.ValidFrom)
                    .Skip((query.Page - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync(ct);

                return Ok(new PagedResponse<PriceEntryResponse>
                {
                    Items = items.Select(e => MapItemDefinitionEntry(e)),
                    Page = query.Page,
                    PageSize = query.PageSize,
                    TotalCount = total,
                });
            }

            default:
                return BadRequest(new { message = "Scope inválido. Use PRODUCT, VISUAL_DEFINITION o ITEM_DEFINITION." });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  List active prices for a price list + target scope
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns all currently valid price entries for a product across all scopes
    /// (product-level, visual-definition-level, and item-definition-level).
    /// Useful for the pricing admin UI.
    /// </summary>
    [HttpGet("by-product/{productId:int}")]
    [ProducesResponseType(typeof(List<PriceEntryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PriceEntryResponse>>> GetByProduct(
        int productId,
        [FromQuery] int? priceListId,
        [FromQuery] bool activeOnly = true,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var result = new List<PriceEntryResponse>();

        // ── Product-level entries ──
        var productQ = db.PriceProductEntries
            .Include(e => e.PriceList)
            .Include(e => e.Product)
            .AsNoTracking()
            .Where(e => e.ProductId == productId);

        if (priceListId.HasValue)
            productQ = productQ.Where(e => e.PriceListId == priceListId.Value);

        if (activeOnly)
            productQ = productQ.Where(e => e.ValidFrom <= now && (e.ValidTo == null || e.ValidTo > now));

        result.AddRange((await productQ.OrderByDescending(e => e.ValidFrom).ToListAsync(ct))
            .Select(MapProductEntry));

        // ── Visual-definition-level entries ──
        var vdIds = await db.ProductVisualDefinitions
            .AsNoTracking()
            .Where(vd => vd.ProductId == productId)
            .Select(vd => vd.ProductVisualDefinitionId)
            .ToListAsync(ct);

        if (vdIds.Count > 0)
        {
            var vdQ = db.PriceVisualDefinitionEntries
                .Include(e => e.PriceList)
                .Include(e => e.ProductVisualDefinition)
                .AsNoTracking()
                .Where(e => vdIds.Contains(e.ProductVisualDefinitionId));

            if (priceListId.HasValue)
                vdQ = vdQ.Where(e => e.PriceListId == priceListId.Value);

            if (activeOnly)
                vdQ = vdQ.Where(e => e.ValidFrom <= now && (e.ValidTo == null || e.ValidTo > now));

            result.AddRange((await vdQ.OrderByDescending(e => e.ValidFrom).ToListAsync(ct))
                .Select(MapVisualDefinitionEntry));
        }

        // ── Item-definition-level entries ──
        var idIds = await db.InventoryItemDefinitions
            .AsNoTracking()
            .Where(d => d.ProductId == productId)
            .Select(d => d.InventoryItemDefinitionId)
            .ToListAsync(ct);

        if (idIds.Count > 0)
        {
            var idQ = db.PriceItemDefinitionEntries
                .Include(e => e.PriceList)
                .Include(e => e.InventoryItemDefinition)
                .AsNoTracking()
                .Where(e => idIds.Contains(e.InventoryItemDefinitionId));

            if (priceListId.HasValue)
                idQ = idQ.Where(e => e.PriceListId == priceListId.Value);

            if (activeOnly)
                idQ = idQ.Where(e => e.ValidFrom <= now && (e.ValidTo == null || e.ValidTo > now));

            result.AddRange((await idQ.OrderByDescending(e => e.ValidFrom).ToListAsync(ct))
                .Select(MapItemDefinitionEntry));
        }

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves the winning price entry within a scope.
    /// PROMO with highest priority wins over BASE.
    /// </summary>
    private static async Task<ResolvedEntry?> ResolveAtScope<TEntry>(
        IQueryable<TEntry> entries, DateTime asOf, CancellationToken ct)
        where TEntry : class
    {
        // We need to project to a common shape. Use dynamic dispatch based on type.
        var projected = entries switch
        {
            IQueryable<PriceItemDefinitionEntry> q => q
                .Where(e => e.ValidFrom <= asOf && (e.ValidTo == null || asOf < e.ValidTo))
                .Select(e => new ResolvedEntry
                {
                    PriceEntryId = e.PriceEntryId,
                    PriceAmount = e.PriceAmount,
                    PriceKind = e.PriceKind,
                    Priority = e.Priority,
                    PromoName = e.PromoName,
                    PromoCode = e.PromoCode,
                    ValidFrom = e.ValidFrom,
                    ValidTo = e.ValidTo,
                }),

            IQueryable<PriceVisualDefinitionEntry> q => q
                .Where(e => e.ValidFrom <= asOf && (e.ValidTo == null || asOf < e.ValidTo))
                .Select(e => new ResolvedEntry
                {
                    PriceEntryId = e.PriceEntryId,
                    PriceAmount = e.PriceAmount,
                    PriceKind = e.PriceKind,
                    Priority = e.Priority,
                    PromoName = e.PromoName,
                    PromoCode = e.PromoCode,
                    ValidFrom = e.ValidFrom,
                    ValidTo = e.ValidTo,
                }),

            IQueryable<PriceProductEntry> q => q
                .Where(e => e.ValidFrom <= asOf && (e.ValidTo == null || asOf < e.ValidTo))
                .Select(e => new ResolvedEntry
                {
                    PriceEntryId = e.PriceEntryId,
                    PriceAmount = e.PriceAmount,
                    PriceKind = e.PriceKind,
                    Priority = e.Priority,
                    PromoName = e.PromoName,
                    PromoCode = e.PromoCode,
                    ValidFrom = e.ValidFrom,
                    ValidTo = e.ValidTo,
                }),

            _ => throw new InvalidOperationException("Unsupported entry type"),
        };

        // PROMO first (kind desc so PROMO > BASE), then highest priority, then newest
        return await projected
            .OrderByDescending(e => e.PriceKind) // PROMO > BASE alphabetically
            .ThenByDescending(e => e.Priority)
            .ThenByDescending(e => e.ValidFrom)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Finds the current BASE price at the same scope for strikethrough display when PROMO wins.
    /// </summary>
    private async Task<decimal?> FindBasePrice(
        string scope, int itemDefId, long? visualDefId, int productId,
        int priceListId, DateTime asOf, CancellationToken ct)
    {
        return scope switch
        {
            "ITEM_DEFINITION" => await db.PriceItemDefinitionEntries
                .Where(e => e.PriceListId == priceListId
                         && e.InventoryItemDefinitionId == itemDefId
                         && e.PriceKind == "BASE"
                         && e.ValidFrom <= asOf
                         && (e.ValidTo == null || asOf < e.ValidTo))
                .OrderByDescending(e => e.ValidFrom)
                .Select(e => (decimal?)e.PriceAmount)
                .FirstOrDefaultAsync(ct),

            "VISUAL_DEFINITION" when visualDefId.HasValue => await db.PriceVisualDefinitionEntries
                .Where(e => e.PriceListId == priceListId
                         && e.ProductVisualDefinitionId == visualDefId.Value
                         && e.PriceKind == "BASE"
                         && e.ValidFrom <= asOf
                         && (e.ValidTo == null || asOf < e.ValidTo))
                .OrderByDescending(e => e.ValidFrom)
                .Select(e => (decimal?)e.PriceAmount)
                .FirstOrDefaultAsync(ct),

            "PRODUCT" => await db.PriceProductEntries
                .Where(e => e.PriceListId == priceListId
                         && e.ProductId == productId
                         && e.PriceKind == "BASE"
                         && e.ValidFrom <= asOf
                         && (e.ValidTo == null || asOf < e.ValidTo))
                .OrderByDescending(e => e.ValidFrom)
                .Select(e => (decimal?)e.PriceAmount)
                .FirstOrDefaultAsync(ct),

            _ => null,
        };
    }

    /// <summary>
    /// Validates that exactly one target is set and returns the scope name.
    /// </summary>
    private static (string? scope, string? error) ValidateExactlyOneTarget(
        int? productId, long? visualDefId, int? itemDefId)
    {
        int count = (productId.HasValue ? 1 : 0)
                  + (visualDefId.HasValue ? 1 : 0)
                  + (itemDefId.HasValue ? 1 : 0);

        if (count == 0)
            return (null, "Debe especificar exactamente un target: productId, productVisualDefinitionId, o inventoryItemDefinitionId.");
        if (count > 1)
            return (null, "Solo debe especificar un target a la vez.");

        var scope = productId.HasValue ? "PRODUCT"
                  : visualDefId.HasValue ? "VISUAL_DEFINITION"
                  : "ITEM_DEFINITION";

        return (scope, null);
    }

    /// <summary>
    /// Validates that the target FK exists in the database.
    /// </summary>
    private async Task<string?> ValidateTargetExists(
        string scope, int? productId, long? visualDefId, int? itemDefId, CancellationToken ct)
    {
        return scope switch
        {
            "PRODUCT" when !await db.Products.AnyAsync(p => p.ProductId == productId, ct)
                => $"Producto con ID {productId} no encontrado.",
            "VISUAL_DEFINITION" when !await db.ProductVisualDefinitions.AnyAsync(v => v.ProductVisualDefinitionId == visualDefId, ct)
                => $"Visual definition con ID {visualDefId} no encontrada.",
            "ITEM_DEFINITION" when !await db.InventoryItemDefinitions.AnyAsync(d => d.InventoryItemDefinitionId == itemDefId, ct)
                => $"Item definition con ID {itemDefId} no encontrado.",
            _ => null,
        };
    }

    // ── Insert helpers ──

    private async Task<long> InsertProductEntry(
        int priceListId, int productId, decimal amount, string kind, int priority,
        string? promoName, string? promoCode, DateTime validFrom, DateTime? validTo,
        string? userId, string? reason, CancellationToken ct)
    {
        var entry = new PriceProductEntry
        {
            PriceListId = priceListId,
            ProductId = productId,
            PriceAmount = amount,
            PriceKind = kind,
            Priority = priority,
            PromoName = promoName,
            PromoCode = promoCode,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Reason = reason,
        };
        db.PriceProductEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.PriceEntryId;
    }

    private async Task<long> InsertVisualDefinitionEntry(
        int priceListId, long visualDefId, decimal amount, string kind, int priority,
        string? promoName, string? promoCode, DateTime validFrom, DateTime? validTo,
        string? userId, string? reason, CancellationToken ct)
    {
        var entry = new PriceVisualDefinitionEntry
        {
            PriceListId = priceListId,
            ProductVisualDefinitionId = visualDefId,
            PriceAmount = amount,
            PriceKind = kind,
            Priority = priority,
            PromoName = promoName,
            PromoCode = promoCode,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Reason = reason,
        };
        db.PriceVisualDefinitionEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.PriceEntryId;
    }

    private async Task<long> InsertItemDefinitionEntry(
        int priceListId, int itemDefId, decimal amount, string kind, int priority,
        string? promoName, string? promoCode, DateTime validFrom, DateTime? validTo,
        string? userId, string? reason, CancellationToken ct)
    {
        var entry = new PriceItemDefinitionEntry
        {
            PriceListId = priceListId,
            InventoryItemDefinitionId = itemDefId,
            PriceAmount = amount,
            PriceKind = kind,
            Priority = priority,
            PromoName = promoName,
            PromoCode = promoCode,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Reason = reason,
        };
        db.PriceItemDefinitionEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry.PriceEntryId;
    }

    // ── Mapping helpers ──

    private static PriceEntryResponse MapProductEntry(PriceProductEntry e) => new()
    {
        PriceEntryId = e.PriceEntryId,
        PriceListId = e.PriceListId,
        PriceListName = e.PriceList?.Name,
        Scope = "PRODUCT",
        ProductId = e.ProductId,
        TargetLabel = e.Product?.Name,
        PriceAmount = e.PriceAmount,
        PriceKind = e.PriceKind,
        Priority = e.Priority,
        PromoName = e.PromoName,
        PromoCode = e.PromoCode,
        ValidFrom = e.ValidFrom,
        ValidTo = e.ValidTo,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        Reason = e.Reason,
    };

    private static PriceEntryResponse MapVisualDefinitionEntry(PriceVisualDefinitionEntry e) => new()
    {
        PriceEntryId = e.PriceEntryId,
        PriceListId = e.PriceListId,
        PriceListName = e.PriceList?.Name,
        Scope = "VISUAL_DEFINITION",
        ProductVisualDefinitionId = e.ProductVisualDefinitionId,
        TargetLabel = e.ProductVisualDefinition is not null
            ? $"Visual #{e.ProductVisualDefinitionId}"
            : null,
        PriceAmount = e.PriceAmount,
        PriceKind = e.PriceKind,
        Priority = e.Priority,
        PromoName = e.PromoName,
        PromoCode = e.PromoCode,
        ValidFrom = e.ValidFrom,
        ValidTo = e.ValidTo,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        Reason = e.Reason,
    };

    private static PriceEntryResponse MapItemDefinitionEntry(PriceItemDefinitionEntry e) => new()
    {
        PriceEntryId = e.PriceEntryId,
        PriceListId = e.PriceListId,
        PriceListName = e.PriceList?.Name,
        Scope = "ITEM_DEFINITION",
        InventoryItemDefinitionId = e.InventoryItemDefinitionId,
        TargetLabel = e.InventoryItemDefinition is not null
            ? (e.InventoryItemDefinition.NameSnapshot ?? e.InventoryItemDefinition.SkuCode)
            : null,
        PriceAmount = e.PriceAmount,
        PriceKind = e.PriceKind,
        Priority = e.Priority,
        PromoName = e.PromoName,
        PromoCode = e.PromoCode,
        ValidFrom = e.ValidFrom,
        ValidTo = e.ValidTo,
        CreatedAt = e.CreatedAt,
        CreatedBy = e.CreatedBy,
        Reason = e.Reason,
    };

    /// <summary>
    /// Loads a single entry response by scope and id (used after insert).
    /// </summary>
    private async Task<PriceEntryResponse> LoadEntryResponse(string scope, long entryId, CancellationToken ct)
    {
        return scope switch
        {
            "PRODUCT" => MapProductEntry(
                (await db.PriceProductEntries
                    .Include(e => e.PriceList).Include(e => e.Product)
                    .AsNoTracking()
                    .FirstAsync(e => e.PriceEntryId == entryId, ct))),

            "VISUAL_DEFINITION" => MapVisualDefinitionEntry(
                (await db.PriceVisualDefinitionEntries
                    .Include(e => e.PriceList).Include(e => e.ProductVisualDefinition)
                    .AsNoTracking()
                    .FirstAsync(e => e.PriceEntryId == entryId, ct))),

            "ITEM_DEFINITION" => MapItemDefinitionEntry(
                (await db.PriceItemDefinitionEntries
                    .Include(e => e.PriceList).Include(e => e.InventoryItemDefinition)
                    .AsNoTracking()
                    .FirstAsync(e => e.PriceEntryId == entryId, ct))),

            _ => throw new InvalidOperationException(),
        };
    }

    /// <summary>Internal DTO for price resolution.</summary>
    private class ResolvedEntry
    {
        public long PriceEntryId { get; set; }
        public decimal PriceAmount { get; set; }
        public string PriceKind { get; set; } = null!;
        public int Priority { get; set; }
        public string? PromoName { get; set; }
        public string? PromoCode { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
    }
}
