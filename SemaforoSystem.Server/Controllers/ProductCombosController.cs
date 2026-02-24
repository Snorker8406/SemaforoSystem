using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.ProductCombos;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class ProductCombosController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static ProductComboResponse MapComboToResponse(ProductCombo combo) => new()
    {
        ProductComboId = combo.ProductComboId,
        Name = combo.Name,
        Description = combo.Description,
        Active = combo.Active,
        CreateDate = combo.CreateDate,
        DetailCount = combo.ProductComboDetails.Count,
        PriceCount = combo.ProductPrices.Count,
        SchoolCount = combo.Schools.Count,
        Details = combo.ProductComboDetails
            .OrderBy(d => d.ProductComboDetailId)
            .Select(d => MapDetailToResponse(d))
            .ToList(),
        Schools = combo.Schools
            .OrderBy(s => s.Name)
            .Select(s => new ComboSchoolInfo { SchoolId = s.SchoolId, Name = s.Name })
            .ToList(),
    };

    private static ProductComboDetailResponse MapDetailToResponse(ProductComboDetail detail) => new()
    {
        ProductComboDetailId = detail.ProductComboDetailId,
        ProductComboId = detail.ProductComboId,
        ProductId = detail.ProductId,
        ProductName = detail.Product?.Name,
        EmbroideryId = detail.EmbroideryId,
        EmbroideryName = detail.Embroidery?.Name,
    };

    // ═══════════════════════════════════════════════════════════════════
    //  COMBO (header) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET list (paginated) ─────────────────

    /// <summary>Returns a paginated, filterable, and sortable list of combos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductComboResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductComboResponse>>> GetAll(
        [FromQuery] ProductComboQueryParameters query,
        CancellationToken ct)
    {
        var q = db.ProductCombos
            .Include(c => c.ProductComboDetails).ThenInclude(d => d.Product)
            .Include(c => c.ProductComboDetails).ThenInclude(d => d.Embroidery)
            .Include(c => c.ProductPrices)
            .Include(c => c.Schools)
            .AsNoTracking()
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(c =>
                c.Name.ToLower().Contains(term) ||
                (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
            "createdate" => query.SortDescending
                ? q.OrderByDescending(c => c.CreateDate)
                : q.OrderBy(c => c.CreateDate),
            "detailcount" => query.SortDescending
                ? q.OrderByDescending(c => c.ProductComboDetails.Count)
                : q.OrderBy(c => c.ProductComboDetails.Count),
            _ => q.OrderBy(c => c.Name),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => MapComboToResponse(c))
            .ToListAsync(ct);

        return Ok(new PagedResponse<ProductComboResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET lookup ───────────────────────────

    /// <summary>Returns all combos as a lightweight lookup list.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(List<ProductComboSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductComboSummary>>> GetLookup(CancellationToken ct)
    {
        var items = await db.ProductCombos
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new ProductComboSummary
            {
                ProductComboId = c.ProductComboId,
                Name = c.Name,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ───────────────────────── GET by id ────────────────────────────

    /// <summary>Returns a single combo with its detail lines.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductComboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductComboResponse>> GetById(int id, CancellationToken ct)
    {
        var combo = await db.ProductCombos
            .Include(c => c.ProductComboDetails).ThenInclude(d => d.Product)
            .Include(c => c.ProductComboDetails).ThenInclude(d => d.Embroidery)
            .Include(c => c.ProductPrices)
            .Include(c => c.Schools)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProductComboId == id, ct);

        if (combo is null)
            return NotFound(new { message = $"Combo con ID {id} no encontrado." });

        return Ok(MapComboToResponse(combo));
    }

    // ───────────────────────── POST create ──────────────────────────

    /// <summary>Creates a new product combo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductComboResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductComboResponse>> Create(
        [FromBody] CreateProductComboRequest request,
        CancellationToken ct)
    {
        // Unique name check
        var nameExists = await db.ProductCombos
            .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"Ya existe un combo con el nombre \"{request.Name}\"." });

        var entity = new ProductCombo
        {
            Name = request.Name,
            Description = request.Description,
            Active = request.Active,
            CreateDate = DateTime.UtcNow,
        };

        db.ProductCombos.Add(entity);
        await db.SaveChangesAsync(ct);

        // Sync schools M2M
        if (request.SchoolIds is { Count: > 0 })
        {
            var schools = await db.Schools
                .Where(s => request.SchoolIds.Contains(s.SchoolId))
                .ToListAsync(ct);
            foreach (var school in schools)
                entity.Schools.Add(school);
            await db.SaveChangesAsync(ct);
        }

        await db.Entry(entity).Collection(e => e.ProductComboDetails).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.ProductPrices).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.Schools).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.ProductComboId }, MapComboToResponse(entity));
    }

    // ───────────────────────── PUT update ───────────────────────────

    /// <summary>Updates an existing product combo header.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductComboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductComboResponse>> Update(
        int id,
        [FromBody] UpdateProductComboRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProductCombos
            .Include(c => c.ProductComboDetails).ThenInclude(d => d.Product)
            .Include(c => c.ProductComboDetails).ThenInclude(d => d.Embroidery)
            .Include(c => c.ProductPrices)
            .Include(c => c.Schools)
            .FirstOrDefaultAsync(c => c.ProductComboId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Combo con ID {id} no encontrado." });

        // Unique name check (excluding self)
        var nameExists = await db.ProductCombos
            .AnyAsync(c => c.ProductComboId != id && c.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"Ya existe un combo con el nombre \"{request.Name}\"." });

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Active = request.Active;

        // Sync schools M2M
        if (request.SchoolIds is not null)
        {
            entity.Schools.Clear();
            if (request.SchoolIds.Count > 0)
            {
                var schools = await db.Schools
                    .Where(s => request.SchoolIds.Contains(s.SchoolId))
                    .ToListAsync(ct);
                foreach (var school in schools)
                    entity.Schools.Add(school);
            }
        }

        await db.SaveChangesAsync(ct);

        return Ok(MapComboToResponse(entity));
    }

    // ───────────────────────── DELETE ────────────────────────────────

    /// <summary>Deletes a combo if it has no related prices.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await db.ProductCombos
            .Include(c => c.ProductComboDetails)
            .Include(c => c.ProductPrices)
            .Include(c => c.Schools)
            .FirstOrDefaultAsync(c => c.ProductComboId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Combo con ID {id} no encontrado." });

        if (entity.ProductPrices.Count > 0)
            return Conflict(new
            {
                message = $"No se puede eliminar el combo porque tiene {entity.ProductPrices.Count} precio(s) asociado(s). Elimine los precios primero."
            });

        // Remove schools M2M, details, then the header
        entity.Schools.Clear();
        db.ProductComboDetails.RemoveRange(entity.ProductComboDetails);
        db.ProductCombos.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  COMBO DETAIL (lines) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET details for a combo ──────────────

    /// <summary>Returns all detail lines for a specific combo.</summary>
    [HttpGet("{comboId:int}/details")]
    [ProducesResponseType(typeof(List<ProductComboDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ProductComboDetailResponse>>> GetDetails(int comboId, CancellationToken ct)
    {
        var comboExists = await db.ProductCombos.AnyAsync(c => c.ProductComboId == comboId, ct);
        if (!comboExists)
            return NotFound(new { message = $"Combo con ID {comboId} no encontrado." });

        var details = await db.ProductComboDetails
            .Include(d => d.Product)
            .Include(d => d.Embroidery)
            .AsNoTracking()
            .Where(d => d.ProductComboId == comboId)
            .OrderBy(d => d.ProductComboDetailId)
            .Select(d => MapDetailToResponse(d))
            .ToListAsync(ct);

        return Ok(details);
    }

    // ───────────────────────── GET detail by id ─────────────────────

    /// <summary>Returns a single combo detail line.</summary>
    [HttpGet("{comboId:int}/details/{detailId:int}")]
    [ProducesResponseType(typeof(ProductComboDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductComboDetailResponse>> GetDetailById(
        int comboId, int detailId, CancellationToken ct)
    {
        var detail = await db.ProductComboDetails
            .Include(d => d.Product)
            .Include(d => d.Embroidery)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.ProductComboDetailId == detailId && d.ProductComboId == comboId, ct);

        if (detail is null)
            return NotFound(new { message = $"Detalle con ID {detailId} no encontrado en el combo {comboId}." });

        return Ok(MapDetailToResponse(detail));
    }

    // ───────────────────────── POST create detail ───────────────────

    /// <summary>Adds a new detail line to a combo.</summary>
    [HttpPost("{comboId:int}/details")]
    [ProducesResponseType(typeof(ProductComboDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductComboDetailResponse>> CreateDetail(
        int comboId,
        [FromBody] CreateProductComboDetailRequest request,
        CancellationToken ct)
    {
        var comboExists = await db.ProductCombos.AnyAsync(c => c.ProductComboId == comboId, ct);
        if (!comboExists)
            return NotFound(new { message = $"Combo con ID {comboId} no encontrado." });

        // At least one FK must be provided
        if (!request.ProductId.HasValue && !request.EmbroideryId.HasValue)
            return UnprocessableEntity(new { message = "Debe indicar al menos un producto o un bordado." });

        // Validate Product FK
        if (request.ProductId.HasValue)
        {
            var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId.Value, ct);
            if (!productExists)
                return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });
        }

        // Validate Embroidery FK
        if (request.EmbroideryId.HasValue)
        {
            var embroideryExists = await db.Embroideries.AnyAsync(e => e.EmbroideryId == request.EmbroideryId.Value, ct);
            if (!embroideryExists)
                return UnprocessableEntity(new { message = $"Bordado con ID {request.EmbroideryId} no existe." });
        }

        var entity = new ProductComboDetail
        {
            ProductComboId = comboId,
            ProductId = request.ProductId,
            EmbroideryId = request.EmbroideryId,
        };

        db.ProductComboDetails.Add(entity);
        await db.SaveChangesAsync(ct);

        // Reload navigations
        await db.Entry(entity).Reference(e => e.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(e => e.Embroidery).LoadAsync(ct);

        return CreatedAtAction(
            nameof(GetDetailById),
            new { comboId, detailId = entity.ProductComboDetailId },
            MapDetailToResponse(entity));
    }

    // ───────────────────────── PUT update detail ────────────────────

    /// <summary>Updates an existing combo detail line.</summary>
    [HttpPut("{comboId:int}/details/{detailId:int}")]
    [ProducesResponseType(typeof(ProductComboDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductComboDetailResponse>> UpdateDetail(
        int comboId, int detailId,
        [FromBody] UpdateProductComboDetailRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProductComboDetails
            .Include(d => d.Product)
            .Include(d => d.Embroidery)
            .FirstOrDefaultAsync(d => d.ProductComboDetailId == detailId && d.ProductComboId == comboId, ct);

        if (entity is null)
            return NotFound(new { message = $"Detalle con ID {detailId} no encontrado en el combo {comboId}." });

        // At least one FK must be provided
        if (!request.ProductId.HasValue && !request.EmbroideryId.HasValue)
            return UnprocessableEntity(new { message = "Debe indicar al menos un producto o un bordado." });

        // Validate Product FK
        if (request.ProductId.HasValue)
        {
            var productExists = await db.Products.AnyAsync(p => p.ProductId == request.ProductId.Value, ct);
            if (!productExists)
                return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });
        }

        // Validate Embroidery FK
        if (request.EmbroideryId.HasValue)
        {
            var embroideryExists = await db.Embroideries.AnyAsync(e => e.EmbroideryId == request.EmbroideryId.Value, ct);
            if (!embroideryExists)
                return UnprocessableEntity(new { message = $"Bordado con ID {request.EmbroideryId} no existe." });
        }

        entity.ProductId = request.ProductId;
        entity.EmbroideryId = request.EmbroideryId;

        await db.SaveChangesAsync(ct);

        // Reload navigations in case FKs changed
        await db.Entry(entity).Reference(e => e.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(e => e.Embroidery).LoadAsync(ct);

        return Ok(MapDetailToResponse(entity));
    }

    // ───────────────────────── DELETE detail ─────────────────────────

    /// <summary>Removes a detail line from a combo.</summary>
    [HttpDelete("{comboId:int}/details/{detailId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDetail(int comboId, int detailId, CancellationToken ct)
    {
        var entity = await db.ProductComboDetails
            .FirstOrDefaultAsync(d => d.ProductComboDetailId == detailId && d.ProductComboId == comboId, ct);

        if (entity is null)
            return NotFound(new { message = $"Detalle con ID {detailId} no encontrado en el combo {comboId}." });

        db.ProductComboDetails.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
