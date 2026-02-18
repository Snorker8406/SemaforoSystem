using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.SchoolLevels;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SchoolLevelsController(ApplicationDbContext db) : ControllerBase
{
    // ───────────────────────────── helpers ──────────────────────────────

    private static SchoolLevelResponse MapToResponse(SchoolLevel level, int schoolCount) => new()
    {
        SchoolLevelId = level.SchoolLevelId,
        Name = level.Name,
        Description = level.Description,
        SchoolCount = schoolCount,
    };

    // ───────────────────────────── GET list ─────────────────────────────

    /// <summary>
    /// Returns a paginated, searchable list of school levels.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SchoolLevelResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<SchoolLevelResponse>>> GetAll(
        [FromQuery] QueryParameters query,
        CancellationToken ct)
    {
        var q = db.SchoolLevels
            .AsNoTracking()
            .AsQueryable();

        // ── Search ──
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(l =>
                l.Name.ToLower().Contains(term) ||
                (l.Description != null && l.Description.ToLower().Contains(term)));
        }

        // ── Sorting ──
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(l => l.Name) : q.OrderBy(l => l.Name),
            _ => q.OrderBy(l => l.Name),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new SchoolLevelResponse
            {
                SchoolLevelId = l.SchoolLevelId,
                Name = l.Name,
                Description = l.Description,
                SchoolCount = l.Schools.Count,
            })
            .ToListAsync(ct);

        return Ok(new PagedResponse<SchoolLevelResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────────── GET by id ────────────────────────────

    /// <summary>
    /// Returns a single school level by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SchoolLevelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SchoolLevelResponse>> GetById(int id, CancellationToken ct)
    {
        var level = await db.SchoolLevels
            .AsNoTracking()
            .Where(l => l.SchoolLevelId == id)
            .Select(l => new { Level = l, SchoolCount = l.Schools.Count })
            .FirstOrDefaultAsync(ct);

        if (level is null)
            return NotFound(new { message = $"SchoolLevel with ID {id} was not found." });

        return Ok(MapToResponse(level.Level, level.SchoolCount));
    }

    // ───────────────────────────── POST create ─────────────────────────

    /// <summary>
    /// Creates a new school level.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SchoolLevelResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SchoolLevelResponse>> Create(
        [FromBody] CreateSchoolLevelRequest request,
        CancellationToken ct)
    {
        var nameExists = await db.SchoolLevels
            .AnyAsync(l => l.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"A school level with the name '{request.Name}' already exists." });

        var level = new SchoolLevel
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
        };

        db.SchoolLevels.Add(level);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = level.SchoolLevelId }, MapToResponse(level, 0));
    }

    // ───────────────────────────── PUT update ──────────────────────────

    /// <summary>
    /// Fully updates an existing school level.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(SchoolLevelResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SchoolLevelResponse>> Update(
        int id,
        [FromBody] UpdateSchoolLevelRequest request,
        CancellationToken ct)
    {
        var level = await db.SchoolLevels
            .FirstOrDefaultAsync(l => l.SchoolLevelId == id, ct);

        if (level is null)
            return NotFound(new { message = $"SchoolLevel with ID {id} was not found." });

        var nameExists = await db.SchoolLevels
            .AnyAsync(l => l.SchoolLevelId != id && l.Name.ToLower() == request.Name.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"A school level with the name '{request.Name}' already exists." });

        level.Name = request.Name.Trim();
        level.Description = request.Description?.Trim();

        await db.SaveChangesAsync(ct);

        var schoolCount = await db.Schools.CountAsync(s => s.SchoolLevelId == id, ct);

        return Ok(MapToResponse(level, schoolCount));
    }

    // ───────────────────────────── DELETE ───────────────────────────────

    /// <summary>
    /// Deletes a school level by its ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var level = await db.SchoolLevels
            .FirstOrDefaultAsync(l => l.SchoolLevelId == id, ct);

        if (level is null)
            return NotFound(new { message = $"SchoolLevel with ID {id} was not found." });

        var schoolCount = await db.Schools.CountAsync(s => s.SchoolLevelId == id, ct);

        if (schoolCount > 0)
        {
            return Conflict(new
            {
                message = $"Cannot delete this school level because it is assigned to {schoolCount} school(s).",
                schoolCount,
            });
        }

        db.SchoolLevels.Remove(level);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ────────────────── Existence check ────────────────────────────────

    /// <summary>
    /// Returns whether a school level name is already taken (for form validation).
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

        var exists = await db.SchoolLevels
            .AsNoTracking()
            .AnyAsync(l =>
                l.Name.ToLower() == name.ToLower() &&
                (!excludeId.HasValue || l.SchoolLevelId != excludeId.Value), ct);

        return Ok(new { exists });
    }
}
