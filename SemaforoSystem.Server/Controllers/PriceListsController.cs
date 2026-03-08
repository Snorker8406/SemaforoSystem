using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Pricing;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

/// <summary>
/// CRUD for price lists (Retail, Wholesale, VIP, etc.).
/// </summary>
[ApiController]
[Route("api/price-lists")]
//[Authorize]
[Produces("application/json")]
public class PriceListsController(ApplicationDbContext db) : ControllerBase
{
    // ───────────────────────────── helpers ──────────────────────────────

    private static PriceListResponse MapToResponse(PriceList pl) => new()
    {
        PriceListId = pl.PriceListId,
        Name = pl.Name,
        Currency = pl.Currency,
        IsDefault = pl.IsDefault,
        IsActive = pl.IsActive,
        CreatedAt = pl.CreatedAt,
        UpdatedAt = pl.UpdatedAt,
        ProductEntryCount = pl.PriceProductEntries.Count,
        VisualDefinitionEntryCount = pl.PriceVisualDefinitionEntries.Count,
        ItemDefinitionEntryCount = pl.PriceItemDefinitionEntries.Count,
    };

    // ───────────────────────────── GET list ─────────────────────────────

    /// <summary>
    /// Returns all price lists (optionally filtered by active status).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PriceListResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PriceListResponse>>> GetAll(
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var q = db.PriceLists
            .Include(pl => pl.PriceProductEntries)
            .Include(pl => pl.PriceVisualDefinitionEntries)
            .Include(pl => pl.PriceItemDefinitionEntries)
            .AsNoTracking()
            .AsQueryable();

        if (isActive.HasValue)
            q = q.Where(pl => pl.IsActive == isActive.Value);

        var items = await q
            .OrderByDescending(pl => pl.IsDefault)
            .ThenBy(pl => pl.Name)
            .ToListAsync(ct);

        return Ok(items.Select(MapToResponse).ToList());
    }

    // ───────────────────────────── GET by id ────────────────────────────

    /// <summary>
    /// Returns a single price list by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PriceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriceListResponse>> GetById(int id, CancellationToken ct)
    {
        var pl = await db.PriceLists
            .Include(p => p.PriceProductEntries)
            .Include(p => p.PriceVisualDefinitionEntries)
            .Include(p => p.PriceItemDefinitionEntries)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PriceListId == id, ct);

        if (pl is null)
            return NotFound(new { message = $"Lista de precios con ID {id} no encontrada." });

        return Ok(MapToResponse(pl));
    }

    // ───────────────────────────── POST create ─────────────────────────

    /// <summary>
    /// Creates a new price list.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PriceListResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PriceListResponse>> Create(
        [FromBody] CreatePriceListRequest request,
        CancellationToken ct)
    {
        // Unique name check
        if (await db.PriceLists.AnyAsync(pl => pl.Name == request.Name, ct))
            return Conflict(new { message = $"Ya existe una lista de precios con el nombre '{request.Name}'." });

        // If setting as default, clear any existing default
        if (request.IsDefault)
        {
            var currentDefault = await db.PriceLists
                .FirstOrDefaultAsync(pl => pl.IsDefault, ct);
            if (currentDefault is not null)
                currentDefault.IsDefault = false;
        }

        var priceList = new PriceList
        {
            Name = request.Name,
            Currency = request.Currency,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.PriceLists.Add(priceList);
        await db.SaveChangesAsync(ct);

        // Reload collections (empty for new list)
        return CreatedAtAction(nameof(GetById), new { id = priceList.PriceListId }, MapToResponse(priceList));
    }

    // ───────────────────────────── PUT update ──────────────────────────

    /// <summary>
    /// Updates an existing price list.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PriceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PriceListResponse>> Update(
        int id,
        [FromBody] UpdatePriceListRequest request,
        CancellationToken ct)
    {
        var priceList = await db.PriceLists
            .Include(pl => pl.PriceProductEntries)
            .Include(pl => pl.PriceVisualDefinitionEntries)
            .Include(pl => pl.PriceItemDefinitionEntries)
            .FirstOrDefaultAsync(pl => pl.PriceListId == id, ct);

        if (priceList is null)
            return NotFound(new { message = $"Lista de precios con ID {id} no encontrada." });

        // Unique name check
        if (await db.PriceLists.AnyAsync(pl => pl.Name == request.Name && pl.PriceListId != id, ct))
            return Conflict(new { message = $"Ya existe una lista de precios con el nombre '{request.Name}'." });

        // Handle default flag
        if (request.IsDefault && !priceList.IsDefault)
        {
            var currentDefault = await db.PriceLists
                .FirstOrDefaultAsync(pl => pl.IsDefault && pl.PriceListId != id, ct);
            if (currentDefault is not null)
                currentDefault.IsDefault = false;
        }

        priceList.Name = request.Name;
        priceList.Currency = request.Currency;
        priceList.IsDefault = request.IsDefault;
        priceList.IsActive = request.IsActive;
        priceList.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(MapToResponse(priceList));
    }

    // ───────────────────────────── DELETE ───────────────────────────────

    /// <summary>
    /// Deletes a price list. Fails if it has entries.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var priceList = await db.PriceLists
            .Include(pl => pl.PriceProductEntries)
            .Include(pl => pl.PriceVisualDefinitionEntries)
            .Include(pl => pl.PriceItemDefinitionEntries)
            .FirstOrDefaultAsync(pl => pl.PriceListId == id, ct);

        if (priceList is null)
            return NotFound(new { message = $"Lista de precios con ID {id} no encontrada." });

        var total = priceList.PriceProductEntries.Count
                  + priceList.PriceVisualDefinitionEntries.Count
                  + priceList.PriceItemDefinitionEntries.Count;

        if (total > 0)
            return Conflict(new
            {
                message = $"No se puede eliminar la lista porque tiene {total} entrada(s) de precio.",
                counts = new
                {
                    product = priceList.PriceProductEntries.Count,
                    visualDefinition = priceList.PriceVisualDefinitionEntries.Count,
                    itemDefinition = priceList.PriceItemDefinitionEntries.Count,
                },
            });

        db.PriceLists.Remove(priceList);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
