using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Embroideries;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class EmbroideriesController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static EmbroideryResponse MapToResponse(Embroidery e) => new()
    {
        EmbroideryId = e.EmbroideryId,
        SchoolId = e.SchoolId,
        SchoolName = e.School?.Name,
        Name = e.Name,
        Description = e.Description,
        Stiches = e.Stiches,
        ColorSecuence = e.ColorSecuence,
        Price = e.Price,
        ImageDesign = e.ImageDesign,
        CreateDate = e.CreateDate,
        HasEmbFile = e.EmbFile is { Length: > 0 },
        HasDstFile = e.DstFile is { Length: > 0 },
        HasImage = e.Image is { Length: > 0 },
    };

    private IQueryable<Embroidery> BaseQuery() =>
        db.Embroideries.Include(e => e.School);

    // ═══════════════════════════════════════════════════════════════════
    //  GET list (paginated)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a paginated list of embroideries.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<EmbroideryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<EmbroideryResponse>>> GetAll(
        [FromQuery] EmbroideryQueryParameters query,
        CancellationToken ct)
    {
        var q = BaseQuery().AsNoTracking().AsQueryable();

        // Filters
        if (query.SchoolId.HasValue)
            q = q.Where(e => e.SchoolId == query.SchoolId.Value);

        // Search (name, description, stiches)
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var t = term;
                q = q.Where(e =>
                    e.Name.ToLower().Contains(t) ||
                    (e.Description != null && e.Description.ToLower().Contains(t)) ||
                    (e.School != null && e.School.Name.ToLower().Contains(t)));
            }
        }

        // Sorting
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending
                ? q.OrderByDescending(e => e.Name)
                : q.OrderBy(e => e.Name),
            "school" => query.SortDescending
                ? q.OrderByDescending(e => e.School != null ? e.School.Name : "")
                : q.OrderBy(e => e.School != null ? e.School.Name : ""),
            "price" => query.SortDescending
                ? q.OrderByDescending(e => e.Price)
                : q.OrderBy(e => e.Price),
            "createdate" => query.SortDescending
                ? q.OrderByDescending(e => e.CreateDate)
                : q.OrderBy(e => e.CreateDate),
            _ => q.OrderBy(e => e.Name),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(new PagedResponse<EmbroideryResponse>
        {
            Items = items.Select(MapToResponse),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GET by id
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a single embroidery.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmbroideryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmbroideryResponse>> GetById(int id, CancellationToken ct)
    {
        var entity = await BaseQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmbroideryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bordado con ID {id} no encontrado." });

        return Ok(MapToResponse(entity));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GET lookup (lightweight)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a lightweight list for selects/dropdowns.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(List<EmbroideryLookup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EmbroideryLookup>>> GetLookup(
        [FromQuery] int? schoolId,
        CancellationToken ct)
    {
        var q = db.Embroideries.AsNoTracking().AsQueryable();

        if (schoolId.HasValue)
            q = q.Where(e => e.SchoolId == schoolId.Value);

        var items = await q
            .OrderBy(e => e.Name)
            .Select(e => new EmbroideryLookup
            {
                EmbroideryId = e.EmbroideryId,
                Name = e.Name,
                SchoolId = e.SchoolId,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  POST create
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Creates a new embroidery.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(EmbroideryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmbroideryResponse>> Create(
        [FromBody] CreateEmbroideryRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return UnprocessableEntity(new { message = "El nombre es obligatorio." });

        // Validate school if provided
        if (request.SchoolId.HasValue)
        {
            var schoolExists = await db.Schools.AnyAsync(s => s.SchoolId == request.SchoolId.Value, ct);
            if (!schoolExists)
                return UnprocessableEntity(new { message = $"Escuela con ID {request.SchoolId} no existe." });
        }

        var entity = new Embroidery
        {
            SchoolId = request.SchoolId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Stiches = request.Stiches?.Trim(),
            ColorSecuence = request.ColorSecuence?.Trim(),
            Price = request.Price,
            ImageDesign = request.ImageDesign?.Trim(),
            CreateDate = DateTime.UtcNow,
        };

        db.Embroideries.Add(entity);
        await db.SaveChangesAsync(ct);

        // Reload with school navigation
        await db.Entry(entity).Reference(e => e.School).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.EmbroideryId }, MapToResponse(entity));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PUT update
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Updates an existing embroidery.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(EmbroideryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<EmbroideryResponse>> Update(
        int id,
        [FromBody] UpdateEmbroideryRequest request,
        CancellationToken ct)
    {
        var entity = await BaseQuery()
            .FirstOrDefaultAsync(e => e.EmbroideryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bordado con ID {id} no encontrado." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return UnprocessableEntity(new { message = "El nombre es obligatorio." });

        // Validate school if provided
        if (request.SchoolId.HasValue && request.SchoolId != entity.SchoolId)
        {
            var schoolExists = await db.Schools.AnyAsync(s => s.SchoolId == request.SchoolId.Value, ct);
            if (!schoolExists)
                return UnprocessableEntity(new { message = $"Escuela con ID {request.SchoolId} no existe." });
        }

        entity.SchoolId = request.SchoolId;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.Stiches = request.Stiches?.Trim();
        entity.ColorSecuence = request.ColorSecuence?.Trim();
        entity.Price = request.Price;
        entity.ImageDesign = request.ImageDesign?.Trim();

        await db.SaveChangesAsync(ct);

        // Reload school navigation in case FK changed
        await db.Entry(entity).Reference(e => e.School).LoadAsync(ct);

        return Ok(MapToResponse(entity));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DELETE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Deletes an embroidery.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await db.Embroideries.FirstOrDefaultAsync(e => e.EmbroideryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bordado con ID {id} no encontrado." });

        db.Embroideries.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Image upload / download
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns the embroidery image.</summary>
    [HttpGet("{id:int}/image")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(int id, CancellationToken ct)
    {
        var entity = await db.Embroideries
            .AsNoTracking()
            .Where(e => e.EmbroideryId == id)
            .Select(e => new { e.Image })
            .FirstOrDefaultAsync(ct);

        if (entity?.Image is not { Length: > 0 })
            return NotFound(new { message = "El bordado no tiene imagen." });

        return File(entity.Image, "image/png");
    }

    /// <summary>Uploads an image for the embroidery.</summary>
    [HttpPut("{id:int}/image")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        var entity = await db.Embroideries.FirstOrDefaultAsync(e => e.EmbroideryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bordado con ID {id} no encontrado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        entity.Image = ms.ToArray();

        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  EMB file upload / download
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Downloads the EMB file.</summary>
    [HttpGet("{id:int}/emb-file")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmbFile(int id, CancellationToken ct)
    {
        var entity = await db.Embroideries
            .AsNoTracking()
            .Where(e => e.EmbroideryId == id)
            .Select(e => new { e.EmbFile, e.Name })
            .FirstOrDefaultAsync(ct);

        if (entity?.EmbFile is not { Length: > 0 })
            return NotFound(new { message = "El bordado no tiene archivo EMB." });

        return File(entity.EmbFile, "application/octet-stream", $"{entity.Name}.emb");
    }

    /// <summary>Uploads an EMB file for the embroidery.</summary>
    [HttpPut("{id:int}/emb-file")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadEmbFile(int id, IFormFile file, CancellationToken ct)
    {
        var entity = await db.Embroideries.FirstOrDefaultAsync(e => e.EmbroideryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bordado con ID {id} no encontrado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        entity.EmbFile = ms.ToArray();

        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DST file upload / download
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Downloads the DST file.</summary>
    [HttpGet("{id:int}/dst-file")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDstFile(int id, CancellationToken ct)
    {
        var entity = await db.Embroideries
            .AsNoTracking()
            .Where(e => e.EmbroideryId == id)
            .Select(e => new { e.DstFile, e.Name })
            .FirstOrDefaultAsync(ct);

        if (entity?.DstFile is not { Length: > 0 })
            return NotFound(new { message = "El bordado no tiene archivo DST." });

        return File(entity.DstFile, "application/octet-stream", $"{entity.Name}.dst");
    }

    /// <summary>Uploads a DST file for the embroidery.</summary>
    [HttpPut("{id:int}/dst-file")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadDstFile(int id, IFormFile file, CancellationToken ct)
    {
        var entity = await db.Embroideries.FirstOrDefaultAsync(e => e.EmbroideryId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bordado con ID {id} no encontrado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        entity.DstFile = ms.ToArray();

        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}
