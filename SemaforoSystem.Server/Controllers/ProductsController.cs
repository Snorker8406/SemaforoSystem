using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Products;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class ProductsController(ApplicationDbContext db) : ControllerBase
{
    // ───────────────────────────── helpers ──────────────────────────────

    private static ProductResponse MapToResponse(Product product) => new()
    {
        ProductId = product.ProductId,
        Name = product.Name,
        Barcode = product.Barcode,
        Description = product.Description,
        Model = product.Model,
        Comments = product.Comments,
        SerialCount = product.SerialCount,
        Serialize = product.Serialize,
        CreateDate = product.CreateDate,
        BrandId = product.BrandId,
        BrandName = product.Brand?.Name,
        SizeSystemId = product.SizeSystemId,
        SizeSystemName = product.SizeSystem?.Name,
        Categories = product.Categories.Select(c => new CategoryInfo
        {
            CategoryId = c.CategoryId,
            Name = c.Name,
        }).ToList(),
        SchoolCount = product.Schools.Count,
        HasPicture = product.ProductPictureId != null,
        PictureCount = product.ProductPictures.Count,
        StockTotal = product.Stocks.Sum(s => s.Quantity ?? 0),
        LatestCost = product.ProductCosts
            .OrderByDescending(c => c.CreateDate)
            .Select(c => (decimal?)c.Cost)
            .FirstOrDefault(),
        LatestPrice = product.ProductPrices
            .OrderByDescending(p => p.CreateDate)
            .Select(p => (decimal?)p.Price)
            .FirstOrDefault(),
        VariantSystems = product.ProductVariantSystems.Select(vs => new VariantSystemInfo
        {
            ProductVariantId = vs.ProductVariantId,
            Name = vs.Name,
        }).ToList(),
    };

    // ───────────────────────────── GET list ─────────────────────────────

    /// <summary>
    /// Returns a paginated, filterable, and sortable list of products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetAll(
        [FromQuery] ProductQueryParameters query,
        CancellationToken ct)
    {
        var q = db.Products
            .Include(p => p.Brand)
            .Include(p => p.SizeSystem)
            .Include(p => p.Categories)
            .Include(p => p.Schools)
            .Include(p => p.ProductPictures)
            .Include(p => p.Stocks)
            .Include(p => p.ProductCosts)
            .Include(p => p.ProductPrices)
            .Include(p => p.ProductVariantSystems)
            .AsNoTracking()
            .AsQueryable();

        // ── Filters ──
        if (query.BrandId.HasValue)
            q = q.Where(p => p.BrandId == query.BrandId.Value);

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.Categories.Any(c => c.CategoryId == query.CategoryId.Value));

        if (query.HasSchools == true)
            q = q.Where(p => p.Schools.Any());
        else if (query.HasSchools == false)
            q = q.Where(p => !p.Schools.Any());

        if (!string.IsNullOrWhiteSpace(query.Model))
        {
            var modelTerm = query.Model.ToLower();
            q = q.Where(p => p.Model != null && p.Model.ToLower().Contains(modelTerm));
        }

        if (query.Serialize.HasValue)
            q = q.Where(p => p.Serialize == query.Serialize.Value);

        if (query.HasStock == true)
            q = q.Where(p => p.Stocks.Any(s => s.Quantity > 0));
        else if (query.HasStock == false)
            q = q.Where(p => !p.Stocks.Any(s => s.Quantity > 0));

        // ── Search (name, barcode, description) ──
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(p =>
                (p.Name != null && p.Name.ToLower().Contains(term)) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(term)) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        // ── Sorting ──
        q = query.SortBy?.ToLower() switch
        {
            "name" => query.SortDescending ? q.OrderByDescending(p => p.Name) : q.OrderBy(p => p.Name),
            "createdate" => query.SortDescending ? q.OrderByDescending(p => p.CreateDate) : q.OrderBy(p => p.CreateDate),
            "barcode" => query.SortDescending ? q.OrderByDescending(p => p.Barcode) : q.OrderBy(p => p.Barcode),
            "brand" => query.SortDescending
                ? q.OrderByDescending(p => p.Brand != null ? p.Brand.Name : null)
                : q.OrderBy(p => p.Brand != null ? p.Brand.Name : null),
            "serialcount" => query.SortDescending
                ? q.OrderByDescending(p => p.SerialCount)
                : q.OrderBy(p => p.SerialCount),
            "model" => query.SortDescending
                ? q.OrderByDescending(p => p.Model)
                : q.OrderBy(p => p.Model),
            "schoolcount" => query.SortDescending
                ? q.OrderByDescending(p => p.Schools.Count)
                : q.OrderBy(p => p.Schools.Count),
            "stocktotal" => query.SortDescending
                ? q.OrderByDescending(p => p.Stocks.Sum(s => s.Quantity ?? 0))
                : q.OrderBy(p => p.Stocks.Sum(s => s.Quantity ?? 0)),
            "latestprice" => query.SortDescending
                ? q.OrderByDescending(p => p.ProductPrices.OrderByDescending(pp => pp.CreateDate).Select(pp => (decimal?)pp.Price).FirstOrDefault())
                : q.OrderBy(p => p.ProductPrices.OrderByDescending(pp => pp.CreateDate).Select(pp => (decimal?)pp.Price).FirstOrDefault()),
            "latestcost" => query.SortDescending
                ? q.OrderByDescending(p => p.ProductCosts.OrderByDescending(pc => pc.CreateDate).Select(pc => (decimal?)pc.Cost).FirstOrDefault())
                : q.OrderBy(p => p.ProductCosts.OrderByDescending(pc => pc.CreateDate).Select(pc => (decimal?)pc.Cost).FirstOrDefault()),
            _ => q.OrderBy(p => p.Name), // default sort
        };

        var totalCount = await q.CountAsync(ct);

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => MapToResponse(p))
            .ToListAsync(ct);

        return Ok(new PagedResponse<ProductResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ───────────────────────────── GET by id ────────────────────────────

    /// <summary>
    /// Returns a single product by its ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(int id, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.SizeSystem)
            .Include(p => p.Categories)
            .Include(p => p.Schools)
            .Include(p => p.ProductPictures)
            .Include(p => p.Stocks)
            .Include(p => p.ProductCosts)
            .Include(p => p.ProductPrices)
            .Include(p => p.ProductVariantSystems)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == id, ct);

        if (product is null)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        return Ok(MapToResponse(product));
    }

    // ───────────────────────────── POST create ─────────────────────────

    /// <summary>
    /// Creates a new product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken ct)
    {
        // Validate Brand FK
        if (request.BrandId.HasValue)
        {
            var brandExists = await db.Brands
                .AnyAsync(b => b.BrandId == request.BrandId.Value, ct);

            if (!brandExists)
                return UnprocessableEntity(new { message = $"Brand with ID {request.BrandId} does not exist." });
        }

        // Validate category IDs
        if (request.CategoryIds.Count > 0)
        {
            var existingCategoryIds = await db.Categories
                .Where(c => request.CategoryIds.Contains(c.CategoryId))
                .Select(c => c.CategoryId)
                .ToListAsync(ct);

            var missing = request.CategoryIds.Except(existingCategoryIds).ToList();
            if (missing.Count > 0)
                return UnprocessableEntity(new { message = $"Categories not found: {string.Join(", ", missing)}" });
        }

        // Validate SizeSystem FK
        if (request.SizeSystemId.HasValue)
        {
            var sizeSystemExists = await db.SizeSystems
                .AnyAsync(ss => ss.SizeSystemId == request.SizeSystemId.Value, ct);
            if (!sizeSystemExists)
                return UnprocessableEntity(new { message = $"Size system with ID {request.SizeSystemId} does not exist." });
        }

        // Validate VariantSystem IDs
        if (request.VariantSystemIds.Count > 0)
        {
            var existingVsIds = await db.ProductVariantSystems
                .Where(vs => request.VariantSystemIds.Contains(vs.ProductVariantId))
                .Select(vs => vs.ProductVariantId)
                .ToListAsync(ct);
            var missingVs = request.VariantSystemIds.Except(existingVsIds).ToList();
            if (missingVs.Count > 0)
                return UnprocessableEntity(new { message = $"Variant systems not found: {string.Join(", ", missingVs)}" });
        }

        var product = new Product
        {
            Name = request.Name,
            Barcode = request.Barcode,
            Description = request.Description,
            Model = request.Model,
            Comments = request.Comments,
            SerialCount = request.SerialCount,
            Serialize = request.Serialize,
            BrandId = request.BrandId,
            SizeSystemId = request.SizeSystemId,
            CreateDate = DateTime.UtcNow,
        };

        // Associate categories
        if (request.CategoryIds.Count > 0)
        {
            var categories = await db.Categories
                .Where(c => request.CategoryIds.Contains(c.CategoryId))
                .ToListAsync(ct);
            product.Categories = categories;
        }

        // Associate variant systems (M2M)
        if (request.VariantSystemIds.Count > 0)
        {
            var variantSystems = await db.ProductVariantSystems
                .Where(vs => request.VariantSystemIds.Contains(vs.ProductVariantId))
                .ToListAsync(ct);
            product.ProductVariantSystems = variantSystems;
        }

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        // Reload navigations for response
        await db.Entry(product).Reference(p => p.Brand).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.SizeSystem).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.Categories).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.Schools).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.ProductPictures).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.Stocks).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.ProductCosts).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.ProductPrices).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.ProductVariantSystems).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = product.ProductId }, MapToResponse(product));
    }

    // ───────────────────────────── PUT update ──────────────────────────

    /// <summary>
    /// Fully updates an existing product.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        [FromBody] UpdateProductRequest request,
        CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Brand)
            .Include(p => p.SizeSystem)
            .Include(p => p.Categories)
            .Include(p => p.Schools)
            .Include(p => p.ProductPictures)
            .Include(p => p.Stocks)
            .Include(p => p.ProductCosts)
            .Include(p => p.ProductPrices)
            .Include(p => p.ProductVariantSystems)
            .FirstOrDefaultAsync(p => p.ProductId == id, ct);

        if (product is null)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        // Validate Brand FK
        if (request.BrandId.HasValue && request.BrandId != product.BrandId)
        {
            var brandExists = await db.Brands
                .AnyAsync(b => b.BrandId == request.BrandId.Value, ct);

            if (!brandExists)
                return UnprocessableEntity(new { message = $"Brand with ID {request.BrandId} does not exist." });
        }

        // Validate category IDs
        if (request.CategoryIds.Count > 0)
        {
            var existingCategoryIds = await db.Categories
                .Where(c => request.CategoryIds.Contains(c.CategoryId))
                .Select(c => c.CategoryId)
                .ToListAsync(ct);

            var missing = request.CategoryIds.Except(existingCategoryIds).ToList();
            if (missing.Count > 0)
                return UnprocessableEntity(new { message = $"Categories not found: {string.Join(", ", missing)}" });
        }

        // Validate SizeSystem FK
        if (request.SizeSystemId.HasValue && request.SizeSystemId != product.SizeSystemId)
        {
            var sizeSystemExists = await db.SizeSystems
                .AnyAsync(ss => ss.SizeSystemId == request.SizeSystemId.Value, ct);
            if (!sizeSystemExists)
                return UnprocessableEntity(new { message = $"Size system with ID {request.SizeSystemId} does not exist." });
        }

        // Validate VariantSystem IDs
        if (request.VariantSystemIds.Count > 0)
        {
            var existingVsIds = await db.ProductVariantSystems
                .Where(vs => request.VariantSystemIds.Contains(vs.ProductVariantId))
                .Select(vs => vs.ProductVariantId)
                .ToListAsync(ct);
            var missingVs = request.VariantSystemIds.Except(existingVsIds).ToList();
            if (missingVs.Count > 0)
                return UnprocessableEntity(new { message = $"Variant systems not found: {string.Join(", ", missingVs)}" });
        }

        // Apply scalar updates
        product.Name = request.Name;
        product.Barcode = request.Barcode;
        product.Description = request.Description;
        product.Model = request.Model;
        product.Comments = request.Comments;
        product.SerialCount = request.SerialCount;
        product.Serialize = request.Serialize;
        product.BrandId = request.BrandId;
        product.SizeSystemId = request.SizeSystemId;

        // Update categories (replace all)
        product.Categories.Clear();
        if (request.CategoryIds.Count > 0)
        {
            var categories = await db.Categories
                .Where(c => request.CategoryIds.Contains(c.CategoryId))
                .ToListAsync(ct);
            foreach (var cat in categories)
                product.Categories.Add(cat);
        }

        // Update variant systems (replace all — M2M)
        product.ProductVariantSystems.Clear();
        if (request.VariantSystemIds.Count > 0)
        {
            var variantSystems = await db.ProductVariantSystems
                .Where(vs => request.VariantSystemIds.Contains(vs.ProductVariantId))
                .ToListAsync(ct);
            foreach (var vs in variantSystems)
                product.ProductVariantSystems.Add(vs);
        }

        await db.SaveChangesAsync(ct);

        // Reload SizeSystem for response
        await db.Entry(product).Reference(p => p.SizeSystem).LoadAsync(ct);

        return Ok(MapToResponse(product));
    }

    // ───────────────────────────── DELETE ───────────────────────────────

    /// <summary>
    /// Deletes a product by its ID.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Schools)
            .Include(p => p.Stocks)
            .Include(p => p.SalesDetails)
            .Include(p => p.ProductCosts)
            .Include(p => p.ProductPrices)
            .FirstOrDefaultAsync(p => p.ProductId == id, ct);

        if (product is null)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        // Guard against deleting products with related data
        var conflicts = new List<string>();
        if (product.Schools.Count > 0) conflicts.Add($"{product.Schools.Count} escuela(s)");
        if (product.Stocks.Count > 0) conflicts.Add($"{product.Stocks.Count} registro(s) de stock");
        if (product.SalesDetails.Count > 0) conflicts.Add($"{product.SalesDetails.Count} detalle(s) de venta");
        if (product.ProductCosts.Count > 0) conflicts.Add($"{product.ProductCosts.Count} costo(s)");
        if (product.ProductPrices.Count > 0) conflicts.Add($"{product.ProductPrices.Count} precio(s)");

        if (conflicts.Count > 0)
            return Conflict(new
            {
                message = $"No se puede eliminar el producto porque tiene relaciones: {string.Join(", ", conflicts)}."
            });

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ───────────────────────────── Lookups ──────────────────────────────

    /// <summary>
    /// Returns all brands for dropdown selects.
    /// </summary>
    [HttpGet("brands")]
    [ProducesResponseType(typeof(List<BrandLookup>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<BrandLookup>>> GetBrands(CancellationToken ct)
    {
        var brands = await db.Brands
            .AsNoTracking()
            .OrderBy(b => b.Name)
            .Select(b => new BrandLookup { BrandId = b.BrandId, Name = b.Name })
            .ToListAsync(ct);

        return Ok(brands);
    }

    /// <summary>
    /// Returns all categories for dropdown selects.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<CategoryInfo>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryInfo>>> GetCategories(CancellationToken ct)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .Where(c => c.Enabled == true)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryInfo { CategoryId = c.CategoryId, Name = c.Name })
            .ToListAsync(ct);

        return Ok(categories);
    }
}

public class BrandLookup
{
    public int BrandId { get; set; }
    public string? Name { get; set; }
}
