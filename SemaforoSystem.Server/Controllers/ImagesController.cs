using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Images;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
public class ImagesController(ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════

    private static ProductImageDto MapImageToDto(ProductImage img) => new()
    {
        ProductImageId = img.ProductImageId,
        ImageRole = img.ImageRole,
        ContentType = img.ContentType,
        FileName = img.FileName,
        FileSizeBytes = img.FileSizeBytes,
        WidthPx = img.WidthPx,
        HeightPx = img.HeightPx,
        AltText = img.AltText,
        CreatedAt = img.CreatedAt,
    };

    private static ProductImageTargetDto MapTargetToDto(ProductImageTarget t) => new()
    {
        ProductImageTargetId = t.ProductImageTargetId,
        ProductImageId = t.ProductImageId,
        TargetType = t.TargetType,
        ProductId = t.ProductId,
        InventoryItemDefinitionId = t.InventoryItemDefinitionId,
        SortOrder = t.SortOrder,
        IsPrimary = t.IsPrimary,
        CreatedAt = t.CreatedAt,
        Image = t.ProductImage is not null ? MapImageToDto(t.ProductImage) : null,
    };

    // ═══════════════════════════════════════════════════════════════════
    //  Upload
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Uploads an image and attaches it to a target (PRODUCT or ITEM_DEFINITION).
    /// Expects multipart/form-data with fields for metadata + a single file.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ProductImageTargetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductImageTargetDto>> Upload(
        IFormFile file,
        [FromForm] UploadImageMetadata metadata,
        CancellationToken ct)
    {
        // ── Validate file ──
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No se proporcionó un archivo válido." });

        // ── Validate target type ──
        var targetType = metadata.TargetType.ToUpperInvariant();
        if (targetType is not ("PRODUCT" and not "ITEM_DEFINITION"))
        {
            if (targetType != "PRODUCT" && targetType != "ITEM_DEFINITION")
                return BadRequest(new { message = "target_type debe ser PRODUCT o ITEM_DEFINITION." });
        }

        // ── Validate FK ──
        if (targetType == "PRODUCT")
        {
            if (metadata.ProductId is null)
                return BadRequest(new { message = "product_id es requerido cuando target_type = PRODUCT." });

            if (!await db.Products.AnyAsync(p => p.ProductId == metadata.ProductId, ct))
                return NotFound(new { message = $"Producto con ID {metadata.ProductId} no encontrado." });
        }
        else // ITEM_DEFINITION
        {
            if (metadata.InventoryItemDefinitionId is null)
                return BadRequest(new { message = "inventory_item_definition_id es requerido cuando target_type = ITEM_DEFINITION." });

            if (!await db.InventoryItemDefinitions.AnyAsync(d => d.InventoryItemDefinitionId == metadata.InventoryItemDefinitionId, ct))
                return NotFound(new { message = $"Definición de artículo con ID {metadata.InventoryItemDefinitionId} no encontrada." });
        }

        // ── Read bytes + compute SHA-256 ──
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();
        var sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));

        // ── Validate image role ──
        var role = (metadata.ImageRole ?? "ORIGINAL").ToUpperInvariant();
        if (role is not ("ORIGINAL" or "THUMB" or "DETAIL"))
            role = "ORIGINAL";

        // ── Insert ProductImage ──
        var image = new ProductImage
        {
            ImageBytes = bytes,
            ContentType = file.ContentType,
            FileName = file.FileName,
            FileSizeBytes = (int)file.Length,
            Sha256 = sha256,
            ImageRole = role,
            AltText = metadata.AltText,
        };

        db.ProductImages.Add(image);
        await db.SaveChangesAsync(ct); // generate PK

        // ── If is_primary, clear any existing primary for this target scope ──
        if (metadata.IsPrimary)
        {
            await ClearPrimaryFlag(targetType, metadata.ProductId, metadata.InventoryItemDefinitionId, ct);
        }

        // ── Insert ProductImageTarget ──
        var target = new ProductImageTarget
        {
            ProductImageId = image.ProductImageId,
            TargetType = targetType,
            ProductId = targetType == "PRODUCT" ? metadata.ProductId : null,
            InventoryItemDefinitionId = targetType == "ITEM_DEFINITION" ? metadata.InventoryItemDefinitionId : null,
            SortOrder = metadata.SortOrder,
            IsPrimary = metadata.IsPrimary,
        };

        db.ProductImageTargets.Add(target);
        await db.SaveChangesAsync(ct);

        target.ProductImage = image;

        return CreatedAtAction(nameof(GetImageContent), new { imageId = image.ProductImageId }, MapTargetToDto(target));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Serve binary content
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the raw image bytes for a given ProductImage ID.
    /// </summary>
    [HttpGet("{imageId:long}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetImageContent(long imageId, CancellationToken ct)
    {
        var image = await db.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductImageId == imageId)
            .Select(i => new { i.ImageBytes, i.ContentType })
            .FirstOrDefaultAsync(ct);

        if (image is null || image.ImageBytes.Length == 0)
            return NotFound(new { message = "Imagen no encontrada." });

        return File(image.ImageBytes, image.ContentType);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Query by Product
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns image metadata (no bytes) for a product, optionally filtered by role.
    /// Ordered by is_primary DESC, sort_order ASC.
    /// </summary>
    [HttpGet("by-product/{productId:int}")]
    [ProducesResponseType(typeof(List<ProductImageTargetDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductImageTargetDto>>> GetByProduct(
        int productId,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        var q = db.ProductImageTargets
            .Include(t => t.ProductImage)
            .AsNoTracking()
            .Where(t => t.TargetType == "PRODUCT" && t.ProductId == productId);

        if (!string.IsNullOrWhiteSpace(role))
            q = q.Where(t => t.ProductImage!.ImageRole == role.ToUpperInvariant());

        var items = await q
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => t.SortOrder)
            .ToListAsync(ct);

        return Ok(items.Select(MapTargetToDto).ToList());
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Query by Item Definition (with product-level fallback)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns images for a specific inventory_item_definition.
    /// If none found, falls back to product-level images.
    /// </summary>
    [HttpGet("by-item-definition/{itemDefinitionId:int}")]
    [ProducesResponseType(typeof(List<ProductImageTargetDto>), StatusCodes.Status200OK)]
    [AllowAnonymous]
    public async Task<ActionResult<List<ProductImageTargetDto>>> GetByItemDefinition(
        int itemDefinitionId,
        [FromQuery] string? role,
        CancellationToken ct)
    {
        // Try item_definition-level images first
        var q = db.ProductImageTargets
            .Include(t => t.ProductImage)
            .AsNoTracking()
            .Where(t => t.TargetType == "ITEM_DEFINITION" && t.InventoryItemDefinitionId == itemDefinitionId);

        if (!string.IsNullOrWhiteSpace(role))
            q = q.Where(t => t.ProductImage!.ImageRole == role.ToUpperInvariant());

        var items = await q
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => t.SortOrder)
            .ToListAsync(ct);

        // Fallback to product-level if none found
        if (items.Count == 0)
        {
            var productId = await db.InventoryItemDefinitions
                .AsNoTracking()
                .Where(d => d.InventoryItemDefinitionId == itemDefinitionId)
                .Select(d => (int?)d.ProductId)
                .FirstOrDefaultAsync(ct);

            if (productId is not null)
            {
                var fallbackQ = db.ProductImageTargets
                    .Include(t => t.ProductImage)
                    .AsNoTracking()
                    .Where(t => t.TargetType == "PRODUCT" && t.ProductId == productId);

                if (!string.IsNullOrWhiteSpace(role))
                    fallbackQ = fallbackQ.Where(t => t.ProductImage!.ImageRole == role.ToUpperInvariant());

                items = await fallbackQ
                    .OrderByDescending(t => t.IsPrimary)
                    .ThenBy(t => t.SortOrder)
                    .ToListAsync(ct);
            }
        }

        return Ok(items.Select(MapTargetToDto).ToList());
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Primary image content shortcuts
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the primary image bytes for a product.
    /// Falls back to first image by sort_order if no primary is set.
    /// </summary>
    [HttpGet("by-product/{productId:int}/primary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductPrimaryImage(int productId, CancellationToken ct)
    {
        var target = await db.ProductImageTargets
            .Include(t => t.ProductImage)
            .AsNoTracking()
            .Where(t => t.TargetType == "PRODUCT" && t.ProductId == productId)
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => t.SortOrder)
            .FirstOrDefaultAsync(ct);

        if (target?.ProductImage is null || target.ProductImage.ImageBytes.Length == 0)
            return NotFound(new { message = "Este producto no tiene imagen." });

        return File(target.ProductImage.ImageBytes, target.ProductImage.ContentType);
    }

    /// <summary>
    /// Returns the primary image bytes for an item_definition (with product fallback).
    /// </summary>
    [HttpGet("by-item-definition/{itemDefinitionId:int}/primary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetItemDefinitionPrimaryImage(int itemDefinitionId, CancellationToken ct)
    {
        // Try item_definition level
        var target = await db.ProductImageTargets
            .Include(t => t.ProductImage)
            .AsNoTracking()
            .Where(t => t.TargetType == "ITEM_DEFINITION" && t.InventoryItemDefinitionId == itemDefinitionId)
            .OrderByDescending(t => t.IsPrimary)
            .ThenBy(t => t.SortOrder)
            .FirstOrDefaultAsync(ct);

        // Fallback to product level
        if (target?.ProductImage is null || target.ProductImage.ImageBytes.Length == 0)
        {
            var productId = await db.InventoryItemDefinitions
                .AsNoTracking()
                .Where(d => d.InventoryItemDefinitionId == itemDefinitionId)
                .Select(d => (int?)d.ProductId)
                .FirstOrDefaultAsync(ct);

            if (productId is not null)
            {
                target = await db.ProductImageTargets
                    .Include(t => t.ProductImage)
                    .AsNoTracking()
                    .Where(t => t.TargetType == "PRODUCT" && t.ProductId == productId)
                    .OrderByDescending(t => t.IsPrimary)
                    .ThenBy(t => t.SortOrder)
                    .FirstOrDefaultAsync(ct);
            }
        }

        if (target?.ProductImage is null || target.ProductImage.ImageBytes.Length == 0)
            return NotFound(new { message = "No se encontró imagen para esta configuración." });

        return File(target.ProductImage.ImageBytes, target.ProductImage.ContentType);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Update target (sort_order / is_primary)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the sort_order and/or is_primary of a target link.
    /// </summary>
    [HttpPatch("targets/{targetId:long}")]
    [ProducesResponseType(typeof(ProductImageTargetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductImageTargetDto>> UpdateTarget(
        long targetId,
        [FromBody] UpdateImageTargetRequest request,
        CancellationToken ct)
    {
        var target = await db.ProductImageTargets
            .Include(t => t.ProductImage)
            .FirstOrDefaultAsync(t => t.ProductImageTargetId == targetId, ct);

        if (target is null)
            return NotFound(new { message = $"Target con ID {targetId} no encontrado." });

        if (request.SortOrder.HasValue)
            target.SortOrder = request.SortOrder.Value;

        if (request.IsPrimary == true)
        {
            await ClearPrimaryFlag(target.TargetType, target.ProductId, target.InventoryItemDefinitionId, ct);
            target.IsPrimary = true;
        }
        else if (request.IsPrimary == false)
        {
            target.IsPrimary = false;
        }

        await db.SaveChangesAsync(ct);

        return Ok(MapTargetToDto(target));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Delete target (detach image from scope)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Detaches an image from a target scope.
    /// Does NOT delete the underlying ProductImage row.
    /// </summary>
    [HttpDelete("targets/{targetId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTarget(long targetId, CancellationToken ct)
    {
        var target = await db.ProductImageTargets
            .FirstOrDefaultAsync(t => t.ProductImageTargetId == targetId, ct);

        if (target is null)
            return NotFound(new { message = $"Target con ID {targetId} no encontrado." });

        db.ProductImageTargets.Remove(target);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Delete image (full removal — cascades targets)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Deletes an image and all its target links (cascade).
    /// </summary>
    [HttpDelete("{imageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(long imageId, CancellationToken ct)
    {
        var image = await db.ProductImages
            .FirstOrDefaultAsync(i => i.ProductImageId == imageId, ct);

        if (image is null)
            return NotFound(new { message = "Imagen no encontrada." });

        db.ProductImages.Remove(image);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Internal helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Clears the is_primary flag for all targets in the same scope
    /// (same target_type + same product_id or inventory_item_definition_id).
    /// </summary>
    private async Task ClearPrimaryFlag(
        string targetType, int? productId, int? itemDefinitionId, CancellationToken ct)
    {
        List<ProductImageTarget> existing;

        if (targetType == "PRODUCT" && productId is not null)
        {
            existing = await db.ProductImageTargets
                .Where(t => t.TargetType == "PRODUCT" && t.ProductId == productId && t.IsPrimary)
                .ToListAsync(ct);
        }
        else if (targetType == "ITEM_DEFINITION" && itemDefinitionId is not null)
        {
            existing = await db.ProductImageTargets
                .Where(t => t.TargetType == "ITEM_DEFINITION" && t.InventoryItemDefinitionId == itemDefinitionId && t.IsPrimary)
                .ToListAsync(ct);
        }
        else
        {
            return;
        }

        foreach (var t in existing)
            t.IsPrimary = false;
    }
}
