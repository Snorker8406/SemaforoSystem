using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Sizes;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class SizesController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static SizeSystemResponse MapSystemToResponse(SizeSystem ss) => new()
    {
        SizeSystemId = ss.SizeSystemId,
        Name = ss.Name,
        Description = ss.Description,
        SizeCount = ss.Sizes.Count,
        Sizes = ss.Sizes
            .OrderBy(s => s.SizeOrder ?? int.MaxValue)
            .ThenBy(s => s.SizeId)
            .Select(s => new SizeResponse
            {
                SizeId = s.SizeId,
                SizeValue = s.SizeValue,
                Description = s.Description,
                SizeOrder = s.SizeOrder,
                SizeSystemId = ss.SizeSystemId,
                SizeSystemName = ss.Name,
            }).ToList(),
    };

    private static SizeResponse MapSizeToResponse(Size s) => new()
    {
        SizeId = s.SizeId,
        SizeValue = s.SizeValue,
        Description = s.Description,
        SizeOrder = s.SizeOrder,
        SizeSystemId = s.SizeSystemId,
        SizeSystemName = s.SizeSystem?.Name,
    };

    // ═══════════════════════════════════════════════════════════════════
    //  SIZE SYSTEM (header) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET list ─────────────────────────────

    /// <summary>Returns a paginated, filterable list of size systems with their sizes.</summary>
    [HttpGet("systems")]
    [ProducesResponseType(typeof(PagedResponse<SizeSystemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SizeSystemResponse>>> GetAllSystems(
        [FromQuery] SizeSystemQueryParameters query,
        CancellationToken ct)
    {
        var q = db.SizeSystems
            .Include(ss => ss.Sizes)
            .AsNoTracking()
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(ss =>
                ss.Name.ToLower().Contains(term) ||
                (ss.Description != null && ss.Description.ToLower().Contains(term)));
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(ss => ss.Name) : q.OrderBy(ss => ss.Name),
            "sizecount" => query.SortDescending
                ? q.OrderByDescending(ss => ss.Sizes.Count)
                : q.OrderBy(ss => ss.Sizes.Count),
            _ => q.OrderBy(ss => ss.Name),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(ss => MapSystemToResponse(ss))
            .ToListAsync(ct);

        return Ok(new PagedResponse<SizeSystemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET all (lookup) ─────────────────────

    /// <summary>Returns all size systems as a lightweight lookup list.</summary>
    [HttpGet("systems/lookup")]
    [ProducesResponseType(typeof(List<SizeSystemSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SizeSystemSummary>>> GetSystemsLookup(CancellationToken ct)
    {
        var items = await db.SizeSystems
            .AsNoTracking()
            .OrderBy(ss => ss.Name)
            .Select(ss => new SizeSystemSummary
            {
                SizeSystemId = ss.SizeSystemId,
                Name = ss.Name,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ───────────────────────── GET by id ────────────────────────────

    /// <summary>Returns a single size system with its sizes.</summary>
    [HttpGet("systems/{id:int}")]
    [ProducesResponseType(typeof(SizeSystemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SizeSystemResponse>> GetSystemById(int id, CancellationToken ct)
    {
        var ss = await db.SizeSystems
            .Include(s => s.Sizes)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SizeSystemId == id, ct);

        if (ss is null)
            return NotFound(new { message = $"Sistema de tallas con ID {id} no encontrado." });

        return Ok(MapSystemToResponse(ss));
    }

    // ───────────────────────── POST create ──────────────────────────

    /// <summary>Creates a new size system.</summary>
    [HttpPost("systems")]
    [ProducesResponseType(typeof(SizeSystemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SizeSystemResponse>> CreateSystem(
        [FromBody] CreateSizeSystemRequest request,
        CancellationToken ct)
    {
        // Unique name check
        var nameExists = await db.SizeSystems
            .AnyAsync(ss => ss.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"Ya existe un sistema de tallas con el nombre \"{request.Name}\"." });

        var entity = new SizeSystem
        {
            Name = request.Name,
            Description = request.Description,
        };

        db.SizeSystems.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Collection(e => e.Sizes).LoadAsync(ct);

        return CreatedAtAction(nameof(GetSystemById), new { id = entity.SizeSystemId }, MapSystemToResponse(entity));
    }

    // ───────────────────────── PUT update ───────────────────────────

    /// <summary>Updates an existing size system.</summary>
    [HttpPut("systems/{id:int}")]
    [ProducesResponseType(typeof(SizeSystemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SizeSystemResponse>> UpdateSystem(
        int id,
        [FromBody] UpdateSizeSystemRequest request,
        CancellationToken ct)
    {
        var entity = await db.SizeSystems
            .Include(ss => ss.Sizes)
            .FirstOrDefaultAsync(ss => ss.SizeSystemId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Sistema de tallas con ID {id} no encontrado." });

        // Unique name check (excluding self)
        var nameExists = await db.SizeSystems
            .AnyAsync(ss => ss.SizeSystemId != id && ss.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"Ya existe un sistema de tallas con el nombre \"{request.Name}\"." });

        entity.Name = request.Name;
        entity.Description = request.Description;

        await db.SaveChangesAsync(ct);

        return Ok(MapSystemToResponse(entity));
    }

    // ───────────────────────── DELETE ────────────────────────────────

    /// <summary>Deletes a size system if it has no sizes associated.</summary>
    [HttpDelete("systems/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSystem(int id, CancellationToken ct)
    {
        var entity = await db.SizeSystems
            .Include(ss => ss.Sizes)
            .FirstOrDefaultAsync(ss => ss.SizeSystemId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Sistema de tallas con ID {id} no encontrado." });

        if (entity.Sizes.Count > 0)
            return Conflict(new
            {
                message = $"No se puede eliminar el sistema de tallas porque tiene {entity.Sizes.Count} talla(s) asociada(s). Elimine las tallas primero."
            });

        db.SizeSystems.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SIZE (detail) endpoints — nested under a SizeSystem
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET sizes (paginated, filterable) ─────

    /// <summary>Returns a paginated list of sizes, optionally filtered by system.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SizeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SizeResponse>>> GetAllSizes(
        [FromQuery] SizeQueryParameters query,
        CancellationToken ct)
    {
        var q = db.Sizes
            .Include(s => s.SizeSystem)
            .AsNoTracking()
            .AsQueryable();

        // Filter by system
        if (query.SizeSystemId.HasValue)
            q = q.Where(s => s.SizeSystemId == query.SizeSystemId.Value);

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(s =>
                s.SizeValue.ToLower().Contains(term) ||
                (s.Description != null && s.Description.ToLower().Contains(term)));
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "sizevalue" => query.SortDescending ? q.OrderByDescending(s => s.SizeValue) : q.OrderBy(s => s.SizeValue),
            "sizeorder" => query.SortDescending ? q.OrderByDescending(s => s.SizeOrder) : q.OrderBy(s => s.SizeOrder),
            "sizesystem" => query.SortDescending
                ? q.OrderByDescending(s => s.SizeSystem != null ? s.SizeSystem.Name : null)
                : q.OrderBy(s => s.SizeSystem != null ? s.SizeSystem.Name : null),
            _ => q.OrderBy(s => s.SizeOrder ?? int.MaxValue).ThenBy(s => s.SizeId),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => MapSizeToResponse(s))
            .ToListAsync(ct);

        return Ok(new PagedResponse<SizeResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET sizes for a specific system ───────

    /// <summary>Returns all sizes belonging to a specific size system (no pagination).</summary>
    [HttpGet("systems/{systemId:int}/sizes")]
    [ProducesResponseType(typeof(List<SizeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<SizeResponse>>> GetSizesBySystem(int systemId, CancellationToken ct)
    {
        var systemExists = await db.SizeSystems.AnyAsync(ss => ss.SizeSystemId == systemId, ct);
        if (!systemExists)
            return NotFound(new { message = $"Sistema de tallas con ID {systemId} no encontrado." });

        var sizes = await db.Sizes
            .Include(s => s.SizeSystem)
            .AsNoTracking()
            .Where(s => s.SizeSystemId == systemId)
            .OrderBy(s => s.SizeOrder ?? int.MaxValue)
            .ThenBy(s => s.SizeId)
            .Select(s => MapSizeToResponse(s))
            .ToListAsync(ct);

        return Ok(sizes);
    }

    // ───────────────────────── GET size by id ───────────────────────

    /// <summary>Returns a single size by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SizeResponse>> GetSizeById(int id, CancellationToken ct)
    {
        var size = await db.Sizes
            .Include(s => s.SizeSystem)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SizeId == id, ct);

        if (size is null)
            return NotFound(new { message = $"Talla con ID {id} no encontrada." });

        return Ok(MapSizeToResponse(size));
    }

    // ───────────────────────── POST create size ─────────────────────

    /// <summary>Creates a new size within a size system.</summary>
    [HttpPost("systems/{systemId:int}/sizes")]
    [ProducesResponseType(typeof(SizeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SizeResponse>> CreateSize(
        int systemId,
        [FromBody] CreateSizeRequest request,
        CancellationToken ct)
    {
        var systemExists = await db.SizeSystems.AnyAsync(ss => ss.SizeSystemId == systemId, ct);
        if (!systemExists)
            return NotFound(new { message = $"Sistema de tallas con ID {systemId} no encontrado." });

        var entity = new Size
        {
            SizeValue = request.SizeValue,
            Description = request.Description,
            SizeOrder = request.SizeOrder,
            SizeSystemId = systemId,
        };

        db.Sizes.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(e => e.SizeSystem).LoadAsync(ct);

        return CreatedAtAction(nameof(GetSizeById), new { id = entity.SizeId }, MapSizeToResponse(entity));
    }

    // ───────────────────────── PUT update size ──────────────────────

    /// <summary>Updates an existing size.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SizeResponse>> UpdateSize(
        int id,
        [FromBody] UpdateSizeRequest request,
        CancellationToken ct)
    {
        var entity = await db.Sizes
            .Include(s => s.SizeSystem)
            .FirstOrDefaultAsync(s => s.SizeId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Talla con ID {id} no encontrada." });

        entity.SizeValue = request.SizeValue;
        entity.Description = request.Description;
        entity.SizeOrder = request.SizeOrder;

        await db.SaveChangesAsync(ct);

        return Ok(MapSizeToResponse(entity));
    }

    // ───────────────────────── PUT move size to another system ──────

    /// <summary>Moves a size to a different size system.</summary>
    [HttpPut("{id:int}/move/{targetSystemId:int}")]
    [ProducesResponseType(typeof(SizeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SizeResponse>> MoveSize(
        int id,
        int targetSystemId,
        CancellationToken ct)
    {
        var entity = await db.Sizes
            .Include(s => s.SizeSystem)
            .FirstOrDefaultAsync(s => s.SizeId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Talla con ID {id} no encontrada." });

        var targetExists = await db.SizeSystems.AnyAsync(ss => ss.SizeSystemId == targetSystemId, ct);
        if (!targetExists)
            return NotFound(new { message = $"Sistema de tallas destino con ID {targetSystemId} no encontrado." });

        entity.SizeSystemId = targetSystemId;
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(e => e.SizeSystem).LoadAsync(ct);

        return Ok(MapSizeToResponse(entity));
    }

    // ───────────────────────── DELETE size ───────────────────────────

    /// <summary>Deletes a size if it has no related records.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSize(int id, CancellationToken ct)
    {
        var entity = await db.Sizes
            .Include(s => s.SalesLines)
            .FirstOrDefaultAsync(s => s.SizeId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Talla con ID {id} no encontrada." });

        var conflicts = new List<string>();
        if (entity.SalesLines.Count > 0) conflicts.Add($"{entity.SalesLines.Count} linea(s) de venta");

        if (conflicts.Count > 0)
            return Conflict(new
            {
                message = $"No se puede eliminar la talla porque tiene relaciones: {string.Join(", ", conflicts)}."
            });

        db.Sizes.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
