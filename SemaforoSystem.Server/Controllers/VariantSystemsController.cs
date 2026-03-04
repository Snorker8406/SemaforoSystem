using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Variants;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class VariantSystemsController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static VariantSystemResponse MapSystemToResponse(ProductVariantSystem vs) => new()
    {
        ProductVariantId = vs.ProductVariantId,
        Name = vs.Name,
        Description = vs.Description,
        VariantCount = vs.ProductVariants.Count,
        ProductCount = vs.Products.Count,
        Variants = vs.ProductVariants
            .OrderBy(v => v.ProductVariantId)
            .Select(v => new VariantResponse
            {
                ProductVariantId = v.ProductVariantId,
                VariantValue = v.VariantValue,
                Description = v.Description,
                ProductVariantSystemId = vs.ProductVariantId,
                ProductVariantSystemName = vs.Name,
                HasPicture = v.ProductPictures.Count > 0,
            }).ToList(),
    };

    private static VariantResponse MapVariantToResponse(ProductVariant v) => new()
    {
        ProductVariantId = v.ProductVariantId,
        VariantValue = v.VariantValue,
        Description = v.Description,
        ProductVariantSystemId = v.ProductVariantSystemId,
        ProductVariantSystemName = v.ProductVariantSystem?.Name,
        HasPicture = v.ProductPictures.Count > 0,
    };

    // ═══════════════════════════════════════════════════════════════════
    //  VARIANT SYSTEM (header) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET list ─────────────────────────────

    /// <summary>Returns a paginated, filterable list of variant systems with their variants.</summary>
    [HttpGet("systems")]
    [ProducesResponseType(typeof(PagedResponse<VariantSystemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<VariantSystemResponse>>> GetAllSystems(
        [FromQuery] VariantSystemQueryParameters query,
        CancellationToken ct)
    {
        var q = db.ProductVariantSystems
            .Include(vs => vs.ProductVariants)
                .ThenInclude(v => v.ProductPictures)
            .Include(vs => vs.Products)
            .AsNoTracking()
            .AsQueryable();

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(vs =>
                vs.Name.ToLower().Contains(term) ||
                (vs.Description != null && vs.Description.ToLower().Contains(term)));
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(vs => vs.Name) : q.OrderBy(vs => vs.Name),
            "variantcount" => query.SortDescending
                ? q.OrderByDescending(vs => vs.ProductVariants.Count)
                : q.OrderBy(vs => vs.ProductVariants.Count),
            "productcount" => query.SortDescending
                ? q.OrderByDescending(vs => vs.Products.Count)
                : q.OrderBy(vs => vs.Products.Count),
            _ => q.OrderBy(vs => vs.Name),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(vs => MapSystemToResponse(vs))
            .ToListAsync(ct);

        return Ok(new PagedResponse<VariantSystemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET all (lookup) ─────────────────────

    /// <summary>Returns all variant systems as a lightweight lookup list.</summary>
    [HttpGet("systems/lookup")]
    [ProducesResponseType(typeof(List<VariantSystemSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VariantSystemSummary>>> GetSystemsLookup(CancellationToken ct)
    {
        var items = await db.ProductVariantSystems
            .AsNoTracking()
            .OrderBy(vs => vs.Name)
            .Select(vs => new VariantSystemSummary
            {
                ProductVariantId = vs.ProductVariantId,
                Name = vs.Name,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ───────────────────────── GET by id ────────────────────────────

    /// <summary>Returns a single variant system with its variants.</summary>
    [HttpGet("systems/{id:int}")]
    [ProducesResponseType(typeof(VariantSystemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantSystemResponse>> GetSystemById(int id, CancellationToken ct)
    {
        var vs = await db.ProductVariantSystems
            .Include(s => s.ProductVariants)
                .ThenInclude(v => v.ProductPictures)
            .Include(s => s.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductVariantId == id, ct);

        if (vs is null)
            return NotFound(new { message = $"Sistema de variantes con ID {id} no encontrado." });

        return Ok(MapSystemToResponse(vs));
    }

    // ───────────────────────── POST create ──────────────────────────

    /// <summary>Creates a new variant system.</summary>
    [HttpPost("systems")]
    [ProducesResponseType(typeof(VariantSystemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VariantSystemResponse>> CreateSystem(
        [FromBody] CreateVariantSystemRequest request,
        CancellationToken ct)
    {
        // Unique name check
        var nameExists = await db.ProductVariantSystems
            .AnyAsync(vs => vs.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"Ya existe un sistema de variantes con el nombre \"{request.Name}\"." });

        var entity = new ProductVariantSystem
        {
            Name = request.Name,
            Description = request.Description,
        };

        db.ProductVariantSystems.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Collection(e => e.ProductVariants).LoadAsync(ct);
        await db.Entry(entity).Collection(e => e.Products).LoadAsync(ct);

        return CreatedAtAction(nameof(GetSystemById), new { id = entity.ProductVariantId }, MapSystemToResponse(entity));
    }

    // ───────────────────────── PUT update ───────────────────────────

    /// <summary>Updates an existing variant system.</summary>
    [HttpPut("systems/{id:int}")]
    [ProducesResponseType(typeof(VariantSystemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<VariantSystemResponse>> UpdateSystem(
        int id,
        [FromBody] UpdateVariantSystemRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProductVariantSystems
            .Include(vs => vs.ProductVariants)
            .Include(vs => vs.Products)
            .FirstOrDefaultAsync(vs => vs.ProductVariantId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Sistema de variantes con ID {id} no encontrado." });

        // Unique name check (excluding self)
        var nameExists = await db.ProductVariantSystems
            .AnyAsync(vs => vs.ProductVariantId != id && vs.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"Ya existe un sistema de variantes con el nombre \"{request.Name}\"." });

        entity.Name = request.Name;
        entity.Description = request.Description;

        await db.SaveChangesAsync(ct);

        return Ok(MapSystemToResponse(entity));
    }

    // ───────────────────────── DELETE ────────────────────────────────

    /// <summary>Deletes a variant system if it has no variants or products associated.</summary>
    [HttpDelete("systems/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSystem(int id, CancellationToken ct)
    {
        var entity = await db.ProductVariantSystems
            .Include(vs => vs.ProductVariants)
            .Include(vs => vs.Products)
            .FirstOrDefaultAsync(vs => vs.ProductVariantId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Sistema de variantes con ID {id} no encontrado." });

        var conflicts = new List<string>();
        if (entity.ProductVariants.Count > 0) conflicts.Add($"{entity.ProductVariants.Count} variante(s)");
        if (entity.Products.Count > 0) conflicts.Add($"{entity.Products.Count} producto(s)");

        if (conflicts.Count > 0)
            return Conflict(new
            {
                message = $"No se puede eliminar el sistema de variantes porque tiene relaciones: {string.Join(", ", conflicts)}. Elimine las relaciones primero."
            });

        db.ProductVariantSystems.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  VARIANT (detail) endpoints
    // ═══════════════════════════════════════════════════════════════════

    // ───────────────────────── GET variants (paginated, filterable) ──

    /// <summary>Returns a paginated list of variants, optionally filtered by system.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<VariantResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<VariantResponse>>> GetAllVariants(
        [FromQuery] VariantQueryParameters query,
        CancellationToken ct)
    {
        var q = db.ProductVariants
            .Include(v => v.ProductVariantSystem)
            .Include(v => v.ProductPictures)
            .AsNoTracking()
            .AsQueryable();

        // Filter by system
        if (query.ProductVariantSystemId.HasValue)
            q = q.Where(v => v.ProductVariantSystemId == query.ProductVariantSystemId.Value);

        // Search
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(v =>
                v.VariantValue.ToLower().Contains(term) ||
                (v.Description != null && v.Description.ToLower().Contains(term)));
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "variantvalue" => query.SortDescending ? q.OrderByDescending(v => v.VariantValue) : q.OrderBy(v => v.VariantValue),
            "variantsystem" => query.SortDescending
                ? q.OrderByDescending(v => v.ProductVariantSystem.Name)
                : q.OrderBy(v => v.ProductVariantSystem.Name),
            _ => q.OrderBy(v => v.ProductVariantId),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(v => MapVariantToResponse(v))
            .ToListAsync(ct);

        return Ok(new PagedResponse<VariantResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────── GET variants for a specific system ────

    /// <summary>Returns all variants belonging to a specific variant system (no pagination).</summary>
    [HttpGet("systems/{systemId:int}/variants")]
    [ProducesResponseType(typeof(List<VariantResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VariantResponse>>> GetVariantsBySystem(int systemId, CancellationToken ct)
    {
        var systemExists = await db.ProductVariantSystems.AnyAsync(vs => vs.ProductVariantId == systemId, ct);
        if (!systemExists)
            return NotFound(new { message = $"Sistema de variantes con ID {systemId} no encontrado." });

        var variants = await db.ProductVariants
            .Include(v => v.ProductVariantSystem)
            .Include(v => v.ProductPictures)
            .AsNoTracking()
            .Where(v => v.ProductVariantSystemId == systemId)
            .OrderBy(v => v.ProductVariantId)
            .Select(v => MapVariantToResponse(v))
            .ToListAsync(ct);

        return Ok(variants);
    }

    // ───────────────────────── GET variant by id ────────────────────

    /// <summary>Returns a single variant by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantResponse>> GetVariantById(int id, CancellationToken ct)
    {
        var variant = await db.ProductVariants
            .Include(v => v.ProductVariantSystem)
            .Include(v => v.ProductPictures)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ProductVariantId == id, ct);

        if (variant is null)
            return NotFound(new { message = $"Variante con ID {id} no encontrada." });

        return Ok(MapVariantToResponse(variant));
    }

    // ───────────────────────── POST create variant ──────────────────

    /// <summary>Creates a new variant within a variant system.</summary>
    [HttpPost("systems/{systemId:int}/variants")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantResponse>> CreateVariant(
        int systemId,
        [FromBody] CreateVariantRequest request,
        CancellationToken ct)
    {
        var systemExists = await db.ProductVariantSystems.AnyAsync(vs => vs.ProductVariantId == systemId, ct);
        if (!systemExists)
            return NotFound(new { message = $"Sistema de variantes con ID {systemId} no encontrado." });

        var entity = new ProductVariant
        {
            VariantValue = request.VariantValue,
            Description = request.Description,
            ProductVariantSystemId = systemId,
        };

        db.ProductVariants.Add(entity);
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(e => e.ProductVariantSystem).LoadAsync(ct);

        return CreatedAtAction(nameof(GetVariantById), new { id = entity.ProductVariantId }, MapVariantToResponse(entity));
    }

    // ───────────────────────── PUT update variant ───────────────────

    /// <summary>Updates an existing variant.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantResponse>> UpdateVariant(
        int id,
        [FromBody] UpdateVariantRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProductVariants
            .Include(v => v.ProductVariantSystem)
            .FirstOrDefaultAsync(v => v.ProductVariantId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Variante con ID {id} no encontrada." });

        entity.VariantValue = request.VariantValue;
        entity.Description = request.Description;

        await db.SaveChangesAsync(ct);

        return Ok(MapVariantToResponse(entity));
    }

    // ───────────────────────── PUT move variant to another system ───

    /// <summary>Moves a variant to a different variant system.</summary>
    [HttpPut("{id:int}/move/{targetSystemId:int}")]
    [ProducesResponseType(typeof(VariantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VariantResponse>> MoveVariant(
        int id,
        int targetSystemId,
        CancellationToken ct)
    {
        var entity = await db.ProductVariants
            .Include(v => v.ProductVariantSystem)
            .FirstOrDefaultAsync(v => v.ProductVariantId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Variante con ID {id} no encontrada." });

        var targetExists = await db.ProductVariantSystems.AnyAsync(vs => vs.ProductVariantId == targetSystemId, ct);
        if (!targetExists)
            return NotFound(new { message = $"Sistema de variantes destino con ID {targetSystemId} no encontrado." });

        entity.ProductVariantSystemId = targetSystemId;
        await db.SaveChangesAsync(ct);

        await db.Entry(entity).Reference(e => e.ProductVariantSystem).LoadAsync(ct);

        return Ok(MapVariantToResponse(entity));
    }

    // ───────────────────────── DELETE variant ────────────────────────

    /// <summary>Deletes a variant if it has no related records.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariant(int id, CancellationToken ct)
    {
        var entity = await db.ProductVariants
            .FirstOrDefaultAsync(v => v.ProductVariantId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Variante con ID {id} no encontrada." });

        db.ProductVariants.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Variant Picture
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the picture associated with a specific variant.
    /// </summary>
    [HttpGet("{variantId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetVariantPicture(int variantId, CancellationToken ct)
    {
        var exists = await db.ProductVariants.AnyAsync(v => v.ProductVariantId == variantId, ct);
        if (!exists)
            return NotFound(new { message = $"Variante con ID {variantId} no encontrada." });

        var pictureBytes = await db.ProductPictures
            .AsNoTracking()
            .Where(pp => pp.VariantId == variantId)
            .OrderBy(pp => pp.ProductPictureId)
            .Select(pp => pp.Picture)
            .FirstOrDefaultAsync(ct);

        if (pictureBytes is null or { Length: 0 })
            return NotFound(new { message = "Esta variante no tiene imagen." });

        return File(pictureBytes, "image/png");
    }

    /// <summary>
    /// Uploads or replaces the picture for a specific variant.
    /// Expects a single image file via multipart/form-data.
    /// Only works if the variant already has a picture (update). For initial upload, use
    /// PUT /api/products/{productId}/variants/{variantId}/picture.
    /// </summary>
    [HttpPut("{variantId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVariantPicture(
        int variantId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No se proporcionó un archivo válido." });

        var existing = await db.ProductPictures
            .FirstOrDefaultAsync(pp => pp.VariantId == variantId, ct);

        if (existing is null)
            return NotFound(new { message = "Esta variante no tiene imagen. Use PUT /api/products/{productId}/variants/{variantId}/picture para crear una." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        existing.Picture = ms.ToArray();

        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Imagen de variante actualizada correctamente." });
    }

    /// <summary>
    /// Deletes the picture associated with a variant.
    /// </summary>
    [HttpDelete("{variantId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariantPicture(int variantId, CancellationToken ct)
    {
        var picture = await db.ProductPictures
            .FirstOrDefaultAsync(pp => pp.VariantId == variantId, ct);

        if (picture is null)
            return NotFound(new { message = "Esta variante no tiene imagen." });

        db.ProductPictures.Remove(picture);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
