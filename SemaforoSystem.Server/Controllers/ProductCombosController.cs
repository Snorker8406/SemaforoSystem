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

    private static ProductComboResponse MapToResponse(ProductCombo combo) => new()
    {
        ProductComboId = combo.ProductComboId,
        Name = combo.Name,
        Description = combo.Description,
        IsActive = combo.IsActive,
        CreatedAt = combo.CreatedAt,
        UpdatedAt = combo.UpdatedAt,
        VisualDefinitionCount = combo.ProductComboVisualDefinitions.Count,
        SchoolCount = combo.ProductComboVisualDefinitions
            .SelectMany(vd => vd.ProductComboVisualDefinitionSchools)
            .Select(s => s.SchoolId)
            .Distinct()
            .Count(),
        VisualDefinitions = combo.ProductComboVisualDefinitions
            .OrderBy(vd => vd.SortOrder)
            .Select(MapVisualDefinition)
            .ToList(),
    };

    private static VisualDefinitionResponse MapVisualDefinition(ProductComboVisualDefinition vd) => new()
    {
        ProductComboVisualDefinitionId = vd.ProductComboVisualDefinitionId,
        ProductComboId = vd.ProductComboId,
        Name = vd.Name,
        Description = vd.Description,
        FixedPriceAmount = vd.FixedPriceAmount,
        DiscountType = vd.DiscountType,
        DiscountValue = vd.DiscountValue,
        PriceListId = vd.PriceListId,
        PriceListName = vd.PriceList?.Name,
        IsActive = vd.IsActive,
        SortOrder = vd.SortOrder,
        CreatedAt = vd.CreatedAt,
        UpdatedAt = vd.UpdatedAt,
        HasImage = vd.ProductComboImageTarget is not null,
        ImageId = vd.ProductComboImageTarget?.ProductComboImageId,
        ComponentCount = vd.ProductComboComponents.Count,
        Components = vd.ProductComboComponents
            .OrderBy(c => c.SortOrder)
            .Select(MapComponent)
            .ToList(),
        Schools = vd.ProductComboVisualDefinitionSchools
            .Where(s => s.IsActive)
            .Select(s => new ComboSchoolInfo
            {
                SchoolId = s.SchoolId,
                Name = s.School?.Name ?? "",
            })
            .OrderBy(s => s.Name)
            .ToList(),
    };

    private static ComponentResponse MapComponent(ProductComboComponent c) => new()
    {
        ProductComboComponentId = c.ProductComboComponentId,
        ProductComboVisualDefinitionId = c.ProductComboVisualDefinitionId,
        ComponentType = c.ComponentType,
        ProductId = c.ProductId,
        ProductName = c.Product?.Name,
        ProductVisualDefinitionId = c.ProductVisualDefinitionId,
        VisualDefinitionProductName = c.ProductVisualDefinition?.Product?.Name,
        VisualDefinitionVariants = c.ProductVisualDefinition?.ProductVariants
            ?.OrderBy(v => v.ProductVariantSystem?.Name)
            .Select(v => new ComponentVariantInfo
            {
                SystemName = v.ProductVariantSystem?.Name ?? "",
                VariantValue = v.VariantValue,
            })
            .ToList() ?? [],
        EmbroideryId = c.EmbroideryId,
        EmbroideryName = c.Embroidery?.Name,
        Quantity = c.Quantity,
        Placement = c.Placement,
        IsRequired = c.IsRequired,
        ExtraPrice = c.ExtraPrice,
        SortOrder = c.SortOrder,
    };

    private IQueryable<ProductCombo> FullComboQuery() =>
        db.ProductCombos
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.PriceList)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboComponents)
                    .ThenInclude(comp => comp.Product)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboComponents)
                    .ThenInclude(comp => comp.Embroidery)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboComponents)
                    .ThenInclude(comp => comp.ProductVisualDefinition)
                        .ThenInclude(pvd => pvd!.Product)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboComponents)
                    .ThenInclude(comp => comp.ProductVisualDefinition)
                        .ThenInclude(pvd => pvd!.ProductVariants)
                            .ThenInclude(pv => pv.ProductVariantSystem)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboImageTarget)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboVisualDefinitionSchools)
                    .ThenInclude(s => s.School);

    // ═══════════════════════════════════════════════════════════════════
    //  GET list (paginated)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a paginated, filterable, sortable list of product combos.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductComboResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductComboResponse>>> GetAll(
        [FromQuery] ProductComboQueryParameters query,
        CancellationToken ct)
    {
        var q = FullComboQuery().AsNoTracking().AsQueryable();

        // ── Filters ──
        if (query.IsActive.HasValue)
            q = q.Where(c => c.IsActive == query.IsActive.Value);

        if (query.SchoolId.HasValue)
            q = q.Where(c => c.ProductComboVisualDefinitions.Any(vd =>
                vd.ProductComboVisualDefinitionSchools.Any(s => s.SchoolId == query.SchoolId.Value && s.IsActive)));

        // ── Search (name, description) ──
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var t = term;
                q = q.Where(c =>
                    c.Name.ToLower().Contains(t) ||
                    (c.Description != null && c.Description.ToLower().Contains(t)));
            }
        }

        // ── Sorting ──
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(c => c.Name) : q.OrderBy(c => c.Name),
            "createdat" => query.SortDescending ? q.OrderByDescending(c => c.CreatedAt) : q.OrderBy(c => c.CreatedAt),
            "isactive" => query.SortDescending ? q.OrderByDescending(c => c.IsActive) : q.OrderBy(c => c.IsActive),
            "visualdefinitioncount" => query.SortDescending
                ? q.OrderByDescending(c => c.ProductComboVisualDefinitions.Count)
                : q.OrderBy(c => c.ProductComboVisualDefinitions.Count),
            _ => q.OrderBy(c => c.Name),
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return Ok(new PagedResponse<ProductComboResponse>
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

    /// <summary>Returns a single product combo with all its visual definitions.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ProductComboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductComboResponse>> GetById(long id, CancellationToken ct)
    {
        var combo = await FullComboQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProductComboId == id, ct);

        if (combo is null)
            return NotFound(new { message = $"Combo con ID {id} no encontrado." });

        return Ok(MapToResponse(combo));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GET lookup (lightweight)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a lightweight list of combos for selects/dropdowns.</summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(List<ProductComboSummary>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductComboSummary>>> GetLookup(
        [FromQuery] bool? isActive,
        CancellationToken ct)
    {
        var q = db.ProductCombos.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            q = q.Where(c => c.IsActive == isActive.Value);

        var items = await q
            .OrderBy(c => c.Name)
            .Select(c => new ProductComboSummary
            {
                ProductComboId = c.ProductComboId,
                Name = c.Name,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  POST create combo
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Creates a new product combo.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductComboResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductComboResponse>> Create(
        [FromBody] CreateProductComboRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return UnprocessableEntity(new { message = "El nombre es obligatorio." });

        var entity = new ProductCombo
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = request.IsActive ?? true,
        };

        db.ProductCombos.Add(entity);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.ProductComboId }, MapToResponse(entity));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PUT update combo
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Updates a product combo header.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ProductComboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductComboResponse>> Update(
        long id,
        [FromBody] UpdateProductComboRequest request,
        CancellationToken ct)
    {
        var entity = await FullComboQuery()
            .FirstOrDefaultAsync(c => c.ProductComboId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Combo con ID {id} no encontrado." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return UnprocessableEntity(new { message = "El nombre es obligatorio." });

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.IsActive = request.IsActive ?? entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(MapToResponse(entity));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DELETE combo
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Deletes a product combo and all its children.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCombo(long id, CancellationToken ct)
    {
        var entity = await db.ProductCombos
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboComponents)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboImageTarget)
            .Include(c => c.ProductComboVisualDefinitions)
                .ThenInclude(vd => vd.ProductComboVisualDefinitionSchools)
            .FirstOrDefaultAsync(c => c.ProductComboId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Combo con ID {id} no encontrado." });

        // Remove children first to avoid FK issues
        foreach (var vd in entity.ProductComboVisualDefinitions)
        {
            db.ProductComboVisualDefinitionSchools.RemoveRange(vd.ProductComboVisualDefinitionSchools);
            db.ProductComboComponents.RemoveRange(vd.ProductComboComponents);

            if (vd.ProductComboImageTarget is not null)
            {
                var imageId = vd.ProductComboImageTarget.ProductComboImageId;
                db.ProductComboImageTargets.Remove(vd.ProductComboImageTarget);

                // Remove the image if not referenced by any other target
                var otherTargets = await db.ProductComboImageTargets
                    .CountAsync(t => t.ProductComboImageId == imageId
                        && t.ProductComboImageTargetId != vd.ProductComboImageTarget.ProductComboImageTargetId, ct);
                if (otherTargets == 0)
                {
                    var image = await db.ProductComboImages.FindAsync([imageId], ct);
                    if (image is not null)
                        db.ProductComboImages.Remove(image);
                }
            }
        }

        db.ProductComboVisualDefinitions.RemoveRange(entity.ProductComboVisualDefinitions);
        db.ProductCombos.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  VISUAL DEFINITIONS sub-resource
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns the visual definitions for a combo.</summary>
    [HttpGet("{comboId:long}/visual-definitions")]
    [ProducesResponseType(typeof(List<VisualDefinitionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VisualDefinitionResponse>>> GetVisualDefinitions(
        long comboId, CancellationToken ct)
    {
        var exists = await db.ProductCombos.AnyAsync(c => c.ProductComboId == comboId, ct);
        if (!exists)
            return NotFound(new { message = $"Combo con ID {comboId} no encontrado." });

        var vds = await db.ProductComboVisualDefinitions
            .AsNoTracking()
            .Where(vd => vd.ProductComboId == comboId)
            .Include(vd => vd.PriceList)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.Product)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.Embroidery)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.ProductVisualDefinition)
                    .ThenInclude(pvd => pvd!.Product)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.ProductVisualDefinition)
                    .ThenInclude(pvd => pvd!.ProductVariants)
                        .ThenInclude(pv => pv.ProductVariantSystem)
            .Include(vd => vd.ProductComboImageTarget)
            .Include(vd => vd.ProductComboVisualDefinitionSchools)
                .ThenInclude(s => s.School)
            .OrderBy(vd => vd.SortOrder)
            .ToListAsync(ct);

        return Ok(vds.Select(MapVisualDefinition).ToList());
    }

    /// <summary>Creates a visual definition under a combo.</summary>
    [HttpPost("{comboId:long}/visual-definitions")]
    [ProducesResponseType(typeof(VisualDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VisualDefinitionResponse>> CreateVisualDefinition(
        long comboId,
        [FromBody] CreateVisualDefinitionRequest request,
        CancellationToken ct)
    {
        var combo = await db.ProductCombos.AnyAsync(c => c.ProductComboId == comboId, ct);
        if (!combo)
            return NotFound(new { message = $"Combo con ID {comboId} no encontrado." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return UnprocessableEntity(new { message = "El nombre es obligatorio." });

        if (request.PriceListId.HasValue)
        {
            var plExists = await db.PriceLists.AnyAsync(pl => pl.PriceListId == request.PriceListId.Value, ct);
            if (!plExists)
                return UnprocessableEntity(new { message = $"Lista de precios con ID {request.PriceListId} no existe." });
        }

        // Validate school IDs
        if (request.SchoolIds is { Count: > 0 })
        {
            var existingSchoolIds = await db.Schools
                .Where(s => request.SchoolIds.Contains(s.SchoolId))
                .Select(s => s.SchoolId)
                .ToListAsync(ct);
            var missing = request.SchoolIds.Except(existingSchoolIds).ToList();
            if (missing.Count > 0)
                return UnprocessableEntity(new { message = $"Escuelas no encontradas: {string.Join(", ", missing)}" });
        }

        var maxSort = await db.ProductComboVisualDefinitions
            .Where(vd => vd.ProductComboId == comboId)
            .Select(vd => (int?)vd.SortOrder)
            .MaxAsync(ct) ?? 0;

        var entity = new ProductComboVisualDefinition
        {
            ProductComboId = comboId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            FixedPriceAmount = request.FixedPriceAmount,
            DiscountType = request.DiscountType ?? "NONE",
            DiscountValue = request.DiscountValue,
            PriceListId = request.PriceListId,
            IsActive = request.IsActive ?? true,
            SortOrder = request.SortOrder ?? (maxSort + 1),
        };

        db.ProductComboVisualDefinitions.Add(entity);
        await db.SaveChangesAsync(ct);

        // Add schools
        if (request.SchoolIds is { Count: > 0 })
        {
            foreach (var schoolId in request.SchoolIds)
            {
                db.ProductComboVisualDefinitionSchools.Add(new ProductComboVisualDefinitionSchool
                {
                    ProductComboVisualDefinitionId = entity.ProductComboVisualDefinitionId,
                    SchoolId = schoolId,
                    IsActive = true,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        // Reload with navigations
        var loaded = await db.ProductComboVisualDefinitions
            .AsNoTracking()
            .Include(vd => vd.PriceList)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.Product)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.Embroidery)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.ProductVisualDefinition)
                    .ThenInclude(pvd => pvd!.Product)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.ProductVisualDefinition)
                    .ThenInclude(pvd => pvd!.ProductVariants)
                        .ThenInclude(pv => pv.ProductVariantSystem)
            .Include(vd => vd.ProductComboImageTarget)
            .Include(vd => vd.ProductComboVisualDefinitionSchools)
                .ThenInclude(s => s.School)
            .FirstAsync(vd => vd.ProductComboVisualDefinitionId == entity.ProductComboVisualDefinitionId, ct);

        return CreatedAtAction(nameof(GetVisualDefinitions), new { comboId }, MapVisualDefinition(loaded));
    }

    /// <summary>Updates a visual definition.</summary>
    [HttpPut("{comboId:long}/visual-definitions/{vdId:long}")]
    [ProducesResponseType(typeof(VisualDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<VisualDefinitionResponse>> UpdateVisualDefinition(
        long comboId, long vdId,
        [FromBody] UpdateVisualDefinitionRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProductComboVisualDefinitions
            .Include(vd => vd.ProductComboVisualDefinitionSchools)
            .FirstOrDefaultAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);

        if (entity is null)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        if (string.IsNullOrWhiteSpace(request.Name))
            return UnprocessableEntity(new { message = "El nombre es obligatorio." });

        if (request.PriceListId.HasValue)
        {
            var plExists = await db.PriceLists.AnyAsync(pl => pl.PriceListId == request.PriceListId.Value, ct);
            if (!plExists)
                return UnprocessableEntity(new { message = $"Lista de precios con ID {request.PriceListId} no existe." });
        }

        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        entity.FixedPriceAmount = request.FixedPriceAmount;
        entity.DiscountType = request.DiscountType ?? entity.DiscountType;
        entity.DiscountValue = request.DiscountValue;
        entity.PriceListId = request.PriceListId;
        entity.IsActive = request.IsActive ?? entity.IsActive;
        entity.SortOrder = request.SortOrder ?? entity.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow;

        // Sync schools if provided
        if (request.SchoolIds is not null)
        {
            if (request.SchoolIds.Count > 0)
            {
                var existingSchoolIds = await db.Schools
                    .Where(s => request.SchoolIds.Contains(s.SchoolId))
                    .Select(s => s.SchoolId)
                    .ToListAsync(ct);
                var missing = request.SchoolIds.Except(existingSchoolIds).ToList();
                if (missing.Count > 0)
                    return UnprocessableEntity(new { message = $"Escuelas no encontradas: {string.Join(", ", missing)}" });
            }

            // Remove old
            db.ProductComboVisualDefinitionSchools.RemoveRange(entity.ProductComboVisualDefinitionSchools);

            // Add new
            foreach (var schoolId in request.SchoolIds)
            {
                db.ProductComboVisualDefinitionSchools.Add(new ProductComboVisualDefinitionSchool
                {
                    ProductComboVisualDefinitionId = vdId,
                    SchoolId = schoolId,
                    IsActive = true,
                });
            }
        }

        await db.SaveChangesAsync(ct);

        // Reload with navigations
        var loaded = await db.ProductComboVisualDefinitions
            .AsNoTracking()
            .Include(vd => vd.PriceList)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.Product)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.Embroidery)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.ProductVisualDefinition)
                    .ThenInclude(pvd => pvd!.Product)
            .Include(vd => vd.ProductComboComponents)
                .ThenInclude(c => c.ProductVisualDefinition)
                    .ThenInclude(pvd => pvd!.ProductVariants)
                        .ThenInclude(pv => pv.ProductVariantSystem)
            .Include(vd => vd.ProductComboImageTarget)
            .Include(vd => vd.ProductComboVisualDefinitionSchools)
                .ThenInclude(s => s.School)
            .FirstAsync(vd => vd.ProductComboVisualDefinitionId == vdId, ct);

        return Ok(MapVisualDefinition(loaded));
    }

    /// <summary>Deletes a visual definition and its children.</summary>
    [HttpDelete("{comboId:long}/visual-definitions/{vdId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVisualDefinition(
        long comboId, long vdId, CancellationToken ct)
    {
        var entity = await db.ProductComboVisualDefinitions
            .Include(vd => vd.ProductComboComponents)
            .Include(vd => vd.ProductComboImageTarget)
            .Include(vd => vd.ProductComboVisualDefinitionSchools)
            .FirstOrDefaultAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);

        if (entity is null)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        db.ProductComboVisualDefinitionSchools.RemoveRange(entity.ProductComboVisualDefinitionSchools);
        db.ProductComboComponents.RemoveRange(entity.ProductComboComponents);

        if (entity.ProductComboImageTarget is not null)
        {
            var imageId = entity.ProductComboImageTarget.ProductComboImageId;
            db.ProductComboImageTargets.Remove(entity.ProductComboImageTarget);

            var otherTargets = await db.ProductComboImageTargets
                .CountAsync(t => t.ProductComboImageId == imageId
                    && t.ProductComboImageTargetId != entity.ProductComboImageTarget.ProductComboImageTargetId, ct);
            if (otherTargets == 0)
            {
                var image = await db.ProductComboImages.FindAsync([imageId], ct);
                if (image is not null)
                    db.ProductComboImages.Remove(image);
            }
        }

        db.ProductComboVisualDefinitions.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  COMPONENTS sub-resource
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Lists components of a visual definition.</summary>
    [HttpGet("{comboId:long}/visual-definitions/{vdId:long}/components")]
    [ProducesResponseType(typeof(List<ComponentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ComponentResponse>>> GetComponents(
        long comboId, long vdId, CancellationToken ct)
    {
        var vdExists = await db.ProductComboVisualDefinitions
            .AnyAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);
        if (!vdExists)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        var components = await db.ProductComboComponents
            .AsNoTracking()
            .Where(c => c.ProductComboVisualDefinitionId == vdId)
            .Include(c => c.Product)
            .Include(c => c.Embroidery)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return Ok(components.Select(MapComponent).ToList());
    }

    /// <summary>Adds a component to a visual definition.</summary>
    [HttpPost("{comboId:long}/visual-definitions/{vdId:long}/components")]
    [ProducesResponseType(typeof(ComponentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ComponentResponse>> CreateComponent(
        long comboId, long vdId,
        [FromBody] CreateComponentRequest request,
        CancellationToken ct)
    {
        var vdExists = await db.ProductComboVisualDefinitions
            .AnyAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);
        if (!vdExists)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        var validTypes = new[] { "PRODUCT", "EMBROIDERY", "PRODUCT_VISUAL_DEFINITION" };
        if (!validTypes.Contains(request.ComponentType))
            return UnprocessableEntity(new { message = $"ComponentType inválido. Valores permitidos: {string.Join(", ", validTypes)}" });

        // Validate referenced entity exists
        switch (request.ComponentType)
        {
            case "PRODUCT":
                if (!request.ProductId.HasValue)
                    return UnprocessableEntity(new { message = "ProductId es requerido para tipo PRODUCT." });
                if (!await db.Products.AnyAsync(p => p.ProductId == request.ProductId.Value, ct))
                    return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });
                break;

            case "EMBROIDERY":
                if (!request.EmbroideryId.HasValue)
                    return UnprocessableEntity(new { message = "EmbroideryId es requerido para tipo EMBROIDERY." });
                if (!await db.Embroideries.AnyAsync(e => e.EmbroideryId == request.EmbroideryId.Value, ct))
                    return UnprocessableEntity(new { message = $"Bordado con ID {request.EmbroideryId} no existe." });
                break;

            case "PRODUCT_VISUAL_DEFINITION":
                if (!request.ProductVisualDefinitionId.HasValue)
                    return UnprocessableEntity(new { message = "ProductVisualDefinitionId es requerido para tipo PRODUCT_VISUAL_DEFINITION." });
                if (!await db.ProductVisualDefinitions.AnyAsync(pvd => pvd.ProductVisualDefinitionId == request.ProductVisualDefinitionId.Value, ct))
                    return UnprocessableEntity(new { message = $"Visual definition de producto con ID {request.ProductVisualDefinitionId} no existe." });
                break;
        }

        var maxSort = await db.ProductComboComponents
            .Where(c => c.ProductComboVisualDefinitionId == vdId)
            .Select(c => (int?)c.SortOrder)
            .MaxAsync(ct) ?? 0;

        var entity = new ProductComboComponent
        {
            ProductComboVisualDefinitionId = vdId,
            ComponentType = request.ComponentType,
            ProductId = request.ComponentType == "PRODUCT" ? request.ProductId : null,
            ProductVisualDefinitionId = request.ComponentType == "PRODUCT_VISUAL_DEFINITION" ? request.ProductVisualDefinitionId : null,
            EmbroideryId = request.ComponentType == "EMBROIDERY" ? request.EmbroideryId : null,
            Quantity = request.Quantity ?? 1,
            Placement = request.Placement?.Trim(),
            IsRequired = request.IsRequired ?? true,
            ExtraPrice = request.ExtraPrice,
            SortOrder = request.SortOrder ?? (maxSort + 1),
        };

        db.ProductComboComponents.Add(entity);
        await db.SaveChangesAsync(ct);

        // Reload with navigations
        await db.Entry(entity).Reference(c => c.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(c => c.Embroidery).LoadAsync(ct);
        if (entity.ProductVisualDefinitionId.HasValue)
        {
            await db.Entry(entity).Reference(c => c.ProductVisualDefinition).LoadAsync(ct);
            if (entity.ProductVisualDefinition is not null)
            {
                await db.Entry(entity.ProductVisualDefinition).Reference(pvd => pvd.Product).LoadAsync(ct);
                await db.Entry(entity.ProductVisualDefinition).Collection(pvd => pvd.ProductVariants).LoadAsync(ct);
                foreach (var pv in entity.ProductVisualDefinition.ProductVariants)
                    await db.Entry(pv).Reference(v => v.ProductVariantSystem).LoadAsync(ct);
            }
        }

        return CreatedAtAction(nameof(GetComponents), new { comboId, vdId }, MapComponent(entity));
    }

    /// <summary>Updates a component.</summary>
    [HttpPut("{comboId:long}/visual-definitions/{vdId:long}/components/{compId:long}")]
    [ProducesResponseType(typeof(ComponentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ComponentResponse>> UpdateComponent(
        long comboId, long vdId, long compId,
        [FromBody] UpdateComponentRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProductComboComponents
            .Include(c => c.Product)
            .Include(c => c.Embroidery)
            .FirstOrDefaultAsync(c => c.ProductComboComponentId == compId
                && c.ProductComboVisualDefinitionId == vdId, ct);

        if (entity is null)
            return NotFound(new { message = $"Componente con ID {compId} no encontrado." });

        // Verify the visual definition belongs to this combo
        var vdBelongsToCombo = await db.ProductComboVisualDefinitions
            .AnyAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);
        if (!vdBelongsToCombo)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        var validTypes = new[] { "PRODUCT", "EMBROIDERY", "PRODUCT_VISUAL_DEFINITION" };
        if (!validTypes.Contains(request.ComponentType))
            return UnprocessableEntity(new { message = $"ComponentType inválido. Valores permitidos: {string.Join(", ", validTypes)}" });

        // Validate referenced entity
        switch (request.ComponentType)
        {
            case "PRODUCT":
                if (!request.ProductId.HasValue)
                    return UnprocessableEntity(new { message = "ProductId es requerido para tipo PRODUCT." });
                if (!await db.Products.AnyAsync(p => p.ProductId == request.ProductId.Value, ct))
                    return UnprocessableEntity(new { message = $"Producto con ID {request.ProductId} no existe." });
                break;
            case "EMBROIDERY":
                if (!request.EmbroideryId.HasValue)
                    return UnprocessableEntity(new { message = "EmbroideryId es requerido para tipo EMBROIDERY." });
                if (!await db.Embroideries.AnyAsync(e => e.EmbroideryId == request.EmbroideryId.Value, ct))
                    return UnprocessableEntity(new { message = $"Bordado con ID {request.EmbroideryId} no existe." });
                break;
            case "PRODUCT_VISUAL_DEFINITION":
                if (!request.ProductVisualDefinitionId.HasValue)
                    return UnprocessableEntity(new { message = "ProductVisualDefinitionId es requerido para tipo PRODUCT_VISUAL_DEFINITION." });
                if (!await db.ProductVisualDefinitions.AnyAsync(pvd => pvd.ProductVisualDefinitionId == request.ProductVisualDefinitionId.Value, ct))
                    return UnprocessableEntity(new { message = $"Visual definition de producto con ID {request.ProductVisualDefinitionId} no existe." });
                break;
        }

        entity.ComponentType = request.ComponentType;
        entity.ProductId = request.ComponentType == "PRODUCT" ? request.ProductId : null;
        entity.ProductVisualDefinitionId = request.ComponentType == "PRODUCT_VISUAL_DEFINITION" ? request.ProductVisualDefinitionId : null;
        entity.EmbroideryId = request.ComponentType == "EMBROIDERY" ? request.EmbroideryId : null;
        entity.Quantity = request.Quantity ?? entity.Quantity;
        entity.Placement = request.Placement?.Trim();
        entity.IsRequired = request.IsRequired ?? entity.IsRequired;
        entity.ExtraPrice = request.ExtraPrice;
        entity.SortOrder = request.SortOrder ?? entity.SortOrder;

        await db.SaveChangesAsync(ct);

        // Reload navigations
        await db.Entry(entity).Reference(c => c.Product).LoadAsync(ct);
        await db.Entry(entity).Reference(c => c.Embroidery).LoadAsync(ct);
        if (entity.ProductVisualDefinitionId.HasValue)
        {
            await db.Entry(entity).Reference(c => c.ProductVisualDefinition).LoadAsync(ct);
            if (entity.ProductVisualDefinition is not null)
            {
                await db.Entry(entity.ProductVisualDefinition).Reference(pvd => pvd.Product).LoadAsync(ct);
                await db.Entry(entity.ProductVisualDefinition).Collection(pvd => pvd.ProductVariants).LoadAsync(ct);
                foreach (var pv in entity.ProductVisualDefinition.ProductVariants)
                    await db.Entry(pv).Reference(v => v.ProductVariantSystem).LoadAsync(ct);
            }
        }

        return Ok(MapComponent(entity));
    }

    /// <summary>Deletes a component.</summary>
    [HttpDelete("{comboId:long}/visual-definitions/{vdId:long}/components/{compId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComponent(
        long comboId, long vdId, long compId, CancellationToken ct)
    {
        var vdBelongsToCombo = await db.ProductComboVisualDefinitions
            .AnyAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);
        if (!vdBelongsToCombo)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        var entity = await db.ProductComboComponents
            .FirstOrDefaultAsync(c => c.ProductComboComponentId == compId
                && c.ProductComboVisualDefinitionId == vdId, ct);

        if (entity is null)
            return NotFound(new { message = $"Componente con ID {compId} no encontrado." });

        db.ProductComboComponents.Remove(entity);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  IMAGE management
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Uploads an image and links it to a visual definition.</summary>
    [HttpPost("{comboId:long}/visual-definitions/{vdId:long}/image")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadImage(
        long comboId, long vdId,
        IFormFile file,
        CancellationToken ct)
    {
        var vd = await db.ProductComboVisualDefinitions
            .Include(vd => vd.ProductComboImageTarget)
            .FirstOrDefaultAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);

        if (vd is null)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        if (file is null || file.Length == 0)
            return UnprocessableEntity(new { message = "El archivo es obligatorio." });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedTypes.Contains(file.ContentType))
            return UnprocessableEntity(new { message = $"Tipo de archivo no permitido. Permitidos: {string.Join(", ", allowedTypes)}" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        // Compute SHA256
        var sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));

        // Remove old image target if exists
        if (vd.ProductComboImageTarget is not null)
        {
            var oldImageId = vd.ProductComboImageTarget.ProductComboImageId;
            db.ProductComboImageTargets.Remove(vd.ProductComboImageTarget);

            var otherTargets = await db.ProductComboImageTargets
                .CountAsync(t => t.ProductComboImageId == oldImageId
                    && t.ProductComboImageTargetId != vd.ProductComboImageTarget.ProductComboImageTargetId, ct);
            if (otherTargets == 0)
            {
                var oldImage = await db.ProductComboImages.FindAsync([oldImageId], ct);
                if (oldImage is not null)
                    db.ProductComboImages.Remove(oldImage);
            }
        }

        var image = new ProductComboImage
        {
            ImageBytes = bytes,
            ContentType = file.ContentType,
            ImageRole = "ORIGINAL",
            FileName = file.FileName,
            FileSizeBytes = (int)file.Length,
            Sha256 = sha256,
        };

        db.ProductComboImages.Add(image);
        await db.SaveChangesAsync(ct);

        var target = new ProductComboImageTarget
        {
            ProductComboImageId = image.ProductComboImageId,
            ProductComboVisualDefinitionId = vdId,
            SortOrder = 0,
            IsPrimary = true,
        };

        db.ProductComboImageTargets.Add(target);
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            productComboImageId = image.ProductComboImageId,
            fileName = image.FileName,
            contentType = image.ContentType,
            fileSizeBytes = image.FileSizeBytes,
        });
    }

    /// <summary>Returns the image bytes for a combo image.</summary>
    [HttpGet("images/{imageId:long}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(long imageId, CancellationToken ct)
    {
        var image = await db.ProductComboImages
            .AsNoTracking()
            .Where(i => i.ProductComboImageId == imageId)
            .Select(i => new { i.ImageBytes, i.ContentType, i.FileName })
            .FirstOrDefaultAsync(ct);

        if (image is null)
            return NotFound(new { message = $"Imagen con ID {imageId} no encontrada." });

        return File(image.ImageBytes, image.ContentType, image.FileName);
    }

    /// <summary>Deletes a combo image.</summary>
    [HttpDelete("images/{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(long imageId, CancellationToken ct)
    {
        var image = await db.ProductComboImages
            .Include(i => i.ProductComboImageTargets)
            .FirstOrDefaultAsync(i => i.ProductComboImageId == imageId, ct);

        if (image is null)
            return NotFound(new { message = $"Imagen con ID {imageId} no encontrada." });

        db.ProductComboImageTargets.RemoveRange(image.ProductComboImageTargets);
        db.ProductComboImages.Remove(image);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  SCHOOLS sub-resource (on visual definition)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns schools linked to a visual definition.</summary>
    [HttpGet("{comboId:long}/visual-definitions/{vdId:long}/schools")]
    [ProducesResponseType(typeof(List<ComboSchoolInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ComboSchoolInfo>>> GetSchools(
        long comboId, long vdId, CancellationToken ct)
    {
        var vdExists = await db.ProductComboVisualDefinitions
            .AnyAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);
        if (!vdExists)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        var schools = await db.ProductComboVisualDefinitionSchools
            .AsNoTracking()
            .Where(s => s.ProductComboVisualDefinitionId == vdId && s.IsActive)
            .Include(s => s.School)
            .OrderBy(s => s.School.Name)
            .Select(s => new ComboSchoolInfo
            {
                SchoolId = s.SchoolId,
                Name = s.School.Name,
            })
            .ToListAsync(ct);

        return Ok(schools);
    }

    /// <summary>Replaces all school assignments on a visual definition.</summary>
    [HttpPut("{comboId:long}/visual-definitions/{vdId:long}/schools")]
    [ProducesResponseType(typeof(List<ComboSchoolInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<List<ComboSchoolInfo>>> SyncSchools(
        long comboId, long vdId,
        [FromBody] List<int> schoolIds,
        CancellationToken ct)
    {
        var vd = await db.ProductComboVisualDefinitions
            .Include(vd => vd.ProductComboVisualDefinitionSchools)
            .FirstOrDefaultAsync(vd => vd.ProductComboVisualDefinitionId == vdId && vd.ProductComboId == comboId, ct);

        if (vd is null)
            return NotFound(new { message = $"Visual definition con ID {vdId} no encontrada en combo {comboId}." });

        if (schoolIds.Count > 0)
        {
            var existingSchoolIds = await db.Schools
                .Where(s => schoolIds.Contains(s.SchoolId))
                .Select(s => s.SchoolId)
                .ToListAsync(ct);
            var missing = schoolIds.Except(existingSchoolIds).ToList();
            if (missing.Count > 0)
                return UnprocessableEntity(new { message = $"Escuelas no encontradas: {string.Join(", ", missing)}" });
        }

        // Remove old and add new
        db.ProductComboVisualDefinitionSchools.RemoveRange(vd.ProductComboVisualDefinitionSchools);

        foreach (var schoolId in schoolIds)
        {
            db.ProductComboVisualDefinitionSchools.Add(new ProductComboVisualDefinitionSchool
            {
                ProductComboVisualDefinitionId = vdId,
                SchoolId = schoolId,
                IsActive = true,
            });
        }

        await db.SaveChangesAsync(ct);

        var result = await db.ProductComboVisualDefinitionSchools
            .AsNoTracking()
            .Where(s => s.ProductComboVisualDefinitionId == vdId && s.IsActive)
            .Include(s => s.School)
            .OrderBy(s => s.School.Name)
            .Select(s => new ComboSchoolInfo
            {
                SchoolId = s.SchoolId,
                Name = s.School.Name,
            })
            .ToListAsync(ct);

        return Ok(result);
    }
}
