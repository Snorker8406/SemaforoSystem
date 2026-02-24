using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Schools;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SchoolsController(ApplicationDbContext db) : ControllerBase
{
    // ───────────────────────────── helpers ──────────────────────────────

    private static SchoolResponse MapToResponse(School school) => new()
    {
        SchoolId = school.SchoolId,
        SchoolLevelId = school.SchoolLevelId,
        SchoolLevelName = school.SchoolLevel?.Name ?? string.Empty,
        CreateDate = school.CreateDate,
        Name = school.Name,
        Address = school.Address,
        Ciudad = school.Ciudad,
        State = school.State,
        PhoneNumber = school.PhoneNumber,
        PrincipalInfo = school.PrincipalInfo,
        Email = school.Email,
        Description = school.Description,
        HasLogo = school.Logo is { Length: > 0 },
        HasPhoto = school.Photo is { Length: > 0 },
    };

    // ───────────────────────────── GET list ─────────────────────────────

    /// <summary>
    /// Returns a paginated, filterable, and sortable list of schools.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SchoolResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SchoolResponse>>> GetAll(
        [FromQuery] SchoolQueryParameters query,
        CancellationToken ct)
    {
        var q = db.Schools
            .Include(s => s.SchoolLevel)
            .AsNoTracking()
            .AsQueryable();

        // ── Filters ──
        if (query.SchoolLevelId.HasValue)
            q = q.Where(s => s.SchoolLevelId == query.SchoolLevelId.Value);

        if (!string.IsNullOrWhiteSpace(query.Ciudad))
            q = q.Where(s => s.Ciudad != null && s.Ciudad.ToLower().Contains(query.Ciudad.ToLower()));

        if (!string.IsNullOrWhiteSpace(query.State))
            q = q.Where(s => s.State != null && s.State.ToLower().Contains(query.State.ToLower()));

        // ── Search (name, email, address) ──
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(s =>
                s.Name.ToLower().Contains(term) ||
                (s.Email != null && s.Email.ToLower().Contains(term)) ||
                s.Address.ToLower().Contains(term));
        }

        // ── Sorting ──
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(s => s.Name) : q.OrderBy(s => s.Name),
            "createdate" => query.SortDescending ? q.OrderByDescending(s => s.CreateDate) : q.OrderBy(s => s.CreateDate),
            "ciudad" => query.SortDescending ? q.OrderByDescending(s => s.Ciudad) : q.OrderBy(s => s.Ciudad),
            "state" => query.SortDescending ? q.OrderByDescending(s => s.State) : q.OrderBy(s => s.State),
            "schoollevel" => query.SortDescending
                ? q.OrderByDescending(s => s.SchoolLevel.Name)
                : q.OrderBy(s => s.SchoolLevel.Name),
            _ => q.OrderBy(s => s.Name), // default sort
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => MapToResponse(s))
            .ToListAsync(ct);

        return Ok(new PagedResponse<SchoolResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────────── GET by id ────────────────────────────

    /// <summary>
    /// Returns a single school by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SchoolResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolResponse>> GetById(int id, CancellationToken ct)
    {
        var school = await db.Schools
            .Include(s => s.SchoolLevel)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SchoolId == id, ct);

        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        return Ok(MapToResponse(school));
    }

    // ───────────────────────────── POST create ─────────────────────────

    /// <summary>
    /// Creates a new school.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SchoolResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SchoolResponse>> Create(
        [FromBody] CreateSchoolRequest request,
        CancellationToken ct)
    {
        // Validate FK
        var levelExists = await db.SchoolLevels
            .AnyAsync(l => l.SchoolLevelId == request.SchoolLevelId, ct);

        if (!levelExists)
            return UnprocessableEntity(new { message = $"SchoolLevel with ID {request.SchoolLevelId} does not exist." });

        // Check for duplicate name
        var nameExists = await db.Schools
            .AnyAsync(s => s.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"A school with the name '{request.Name}' already exists." });

        var school = new School
        {
            SchoolLevelId = request.SchoolLevelId,
            CreateDate = DateTime.UtcNow,
            Name = request.Name,
            Address = request.Address,
            Ciudad = request.Ciudad,
            State = request.State,
            PhoneNumber = request.PhoneNumber,
            PrincipalInfo = request.PrincipalInfo,
            Email = request.Email,
            Description = request.Description,
        };

        db.Schools.Add(school);
        await db.SaveChangesAsync(ct);

        // Reload with navigation for response
        await db.Entry(school).Reference(s => s.SchoolLevel).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = school.SchoolId }, MapToResponse(school));
    }

    // ───────────────────────────── PUT update ──────────────────────────

    /// <summary>
    /// Fully updates an existing school.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SchoolResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SchoolResponse>> Update(
        int id,
        [FromBody] UpdateSchoolRequest request,
        CancellationToken ct)
    {
        var school = await db.Schools
            .Include(s => s.SchoolLevel)
            .FirstOrDefaultAsync(s => s.SchoolId == id, ct);

        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        // Validate FK
        if (school.SchoolLevelId != request.SchoolLevelId)
        {
            var levelExists = await db.SchoolLevels
                .AnyAsync(l => l.SchoolLevelId == request.SchoolLevelId, ct);

            if (!levelExists)
                return UnprocessableEntity(new { message = $"SchoolLevel with ID {request.SchoolLevelId} does not exist." });
        }

        // Check for duplicate name (excluding current record)
        var nameExists = await db.Schools
            .AnyAsync(s => s.SchoolId != id && s.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"A school with the name '{request.Name}' already exists." });

        // Apply updates
        school.SchoolLevelId = request.SchoolLevelId;
        school.Name = request.Name;
        school.Address = request.Address;
        school.Ciudad = request.Ciudad;
        school.State = request.State;
        school.PhoneNumber = request.PhoneNumber;
        school.PrincipalInfo = request.PrincipalInfo;
        school.Email = request.Email;
        school.Description = request.Description;

        await db.SaveChangesAsync(ct);

        // Reload navigation if SchoolLevel changed
        await db.Entry(school).Reference(s => s.SchoolLevel).LoadAsync(ct);

        return Ok(MapToResponse(school));
    }

    // ───────────────────────────── DELETE ───────────────────────────────

    /// <summary>
    /// Deletes a school by its ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var school = await db.Schools
            .Include(s => s.Embroideries)
            .Include(s => s.Files)
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.SchoolId == id, ct);

        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        // Guard against deleting schools with related data
        if (school.Embroideries.Count > 0 || school.Files.Count > 0 || school.Products.Count > 0)
        {
            return Conflict(new
            {
                message = "Cannot delete this school because it has related records.",
                relatedCounts = new
                {
                    embroideries = school.Embroideries.Count,
                    files = school.Files.Count,
                    products = school.Products.Count,
                },
            });
        }

        db.Schools.Remove(school);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ──────────────────────── Logo upload / download ────────────────────

    /// <summary>
    /// Uploads or replaces the school logo.
    /// </summary>
    [HttpPut("{id:int}/logo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(2 * 1024 * 1024)] // 2 MB
    public async Task<IActionResult> UploadLogo(int id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "File is empty." });

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(new { message = "Only image files are allowed." });

        var school = await db.Schools.FindAsync([id], ct);
        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        school.Logo = ms.ToArray();

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Returns the school logo image.
    /// </summary>
    [HttpGet("{id:int}/logo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetLogo(int id, CancellationToken ct)
    {
        var school = await db.Schools
            .AsNoTracking()
            .Where(s => s.SchoolId == id)
            .Select(s => new { s.Logo })
            .FirstOrDefaultAsync(ct);

        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        if (school.Logo is null or { Length: 0 })
            return NotFound(new { message = "This school does not have a logo." });

        return File(school.Logo, "image/png");
    }

    /// <summary>
    /// Deletes the school logo.
    /// </summary>
    [HttpDelete("{id:int}/logo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLogo(int id, CancellationToken ct)
    {
        var school = await db.Schools.FindAsync([id], ct);
        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        school.Logo = null;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ──────────────────────── Photo upload / download ───────────────────

    /// <summary>
    /// Uploads or replaces the school photo.
    /// </summary>
    [HttpPut("{id:int}/photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadPhoto(int id, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "File is empty." });

        if (!file.ContentType.StartsWith("image/"))
            return BadRequest(new { message = "Only image files are allowed." });

        var school = await db.Schools.FindAsync([id], ct);
        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        school.Photo = ms.ToArray();

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Returns the school photo image.
    /// </summary>
    [HttpGet("{id:int}/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetPhoto(int id, CancellationToken ct)
    {
        var school = await db.Schools
            .AsNoTracking()
            .Where(s => s.SchoolId == id)
            .Select(s => new { s.Photo })
            .FirstOrDefaultAsync(ct);

        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        if (school.Photo is null or { Length: 0 })
            return NotFound(new { message = "This school does not have a photo." });

        return File(school.Photo, "image/png");
    }

    /// <summary>
    /// Deletes the school photo.
    /// </summary>
    [HttpDelete("{id:int}/photo")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePhoto(int id, CancellationToken ct)
    {
        var school = await db.Schools.FindAsync([id], ct);
        if (school is null)
            return NotFound(new { message = $"School with ID {id} was not found." });

        school.Photo = null;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ──────────────────── Lookup: schools (lightweight) ────────────────

    /// <summary>
    /// Returns all schools as a lightweight lookup list (id + name).
    /// </summary>
    [HttpGet("lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLookup(CancellationToken ct)
    {
        var items = await db.Schools
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new { s.SchoolId, s.Name })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ──────────────────── Lookup: school levels ────────────────────────

    /// <summary>
    /// Returns all school levels for dropdown / select UI components.
    /// </summary>
    [HttpGet("levels")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSchoolLevels(CancellationToken ct)
    {
        var levels = await db.SchoolLevels
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .Select(l => new { l.SchoolLevelId, l.Name, l.Description })
            .ToListAsync(ct);

        return Ok(levels);
    }

    // ───────────────────── Statistics ───────────────────────────────────

    /// <summary>
    /// Returns summary statistics for the schools module.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var totalSchools = await db.Schools.CountAsync(ct);
        var byLevel = await db.Schools
            .AsNoTracking()
            .GroupBy(s => s.SchoolLevel.Name)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byState = await db.Schools
            .AsNoTracking()
            .Where(s => s.State != null)
            .GroupBy(s => s.State!)
            .Select(g => new { State = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(10)
            .ToListAsync(ct);

        return Ok(new { totalSchools, byLevel, byState });
    }

    // ────────────────── Existence check ────────────────────────────────

    /// <summary>
    /// Returns whether a school name is already taken (for form validation).
    /// </summary>
    [HttpGet("exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckExists(
        [FromQuery] string name,
        [FromQuery] int? excludeId,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Ok(new { exists = false });

        var exists = await db.Schools
            .AsNoTracking()
            .AnyAsync(s =>
                s.Name.ToLower() == name.ToLower() &&
                (!excludeId.HasValue || s.SchoolId != excludeId.Value), ct);

        return Ok(new { exists });
    }
}
