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

        // ── Search (name, barcode, description) — multi-word match ──
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var terms = query.Search.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var term in terms)
            {
                var t = term; // closure capture
                q = q.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(t)) ||
                    (p.Barcode != null && p.Barcode.ToLower().Contains(t)) ||
                    (p.Description != null && p.Description.ToLower().Contains(t)));
            }
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

    // ───────────────────────────── GET schools ─────────────────────────

    /// <summary>
    /// Returns the schools (with school level) that use this product.
    /// </summary>
    [HttpGet("{id:int}/schools")]
    [ProducesResponseType(typeof(List<ProductSchoolInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ProductSchoolInfo>>> GetSchools(int id, CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.ProductId == id)
            .Select(p => new
            {
                p.ProductId,
                Schools = p.Schools
                    .OrderBy(s => s.SchoolLevel.Name)
                    .ThenBy(s => s.Name)
                    .Select(s => new ProductSchoolInfo
                    {
                        SchoolId = s.SchoolId,
                        Name = s.Name,
                        SchoolLevelId = s.SchoolLevelId,
                        SchoolLevelName = s.SchoolLevel.Name,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);

        if (product is null)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        return Ok(product.Schools);
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

    // ═══════════════════════════ Product Prices ═════════════════════════

    // ───────────────────────────── helpers ──────────────────────────────

    private static ProductPriceResponse MapPriceToResponse(ProductPrice pp) => new()
    {
        PriceId = pp.PriceId,
        ProductId = pp.ProductId,
        Price = pp.Price,
        CreateDate = pp.CreateDate,
        SizeId = pp.SizeId,
        SizeValue = pp.Size?.SizeValue,
        VariantId = pp.VariantId,
        VariantValue = pp.Variant?.VariantValue,
        ProductComboId = pp.ProductComboId,
    };

    // ───────────────────────────── GET prices ──────────────────────────

    /// <summary>
    /// Returns the latest prices for a product (most recent per size/variant/combo combination).
    /// </summary>
    [HttpGet("{productId:int}/prices")]
    [ProducesResponseType(typeof(List<ProductPriceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ProductPriceResponse>>> GetPrices(
        int productId, CancellationToken ct)
    {
        var productExists = await db.Products.AnyAsync(p => p.ProductId == productId, ct);
        if (!productExists)
            return NotFound(new { message = $"Product with ID {productId} was not found." });

        // Get the latest price per (SizeId, VariantId, ProductComboId) combination
        var prices = await db.ProductPrices
            .Include(pp => pp.Size)
            .Include(pp => pp.Variant)
            .AsNoTracking()
            .Where(pp => pp.ProductId == productId)
            .GroupBy(pp => new { pp.SizeId, pp.VariantId, pp.ProductComboId })
            .Select(g => g.OrderByDescending(pp => pp.CreateDate).First())
            .ToListAsync(ct);

        var result = prices.Select(MapPriceToResponse).ToList();
        return Ok(result);
    }

    // ───────────────────────────── GET price by id ─────────────────────

    /// <summary>
    /// Returns a single price entry by its ID.
    /// </summary>
    [HttpGet("{productId:int}/prices/{priceId:int}")]
    [ProducesResponseType(typeof(ProductPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductPriceResponse>> GetPrice(
        int productId, int priceId, CancellationToken ct)
    {
        var price = await db.ProductPrices
            .Include(pp => pp.Size)
            .Include(pp => pp.Variant)
            .AsNoTracking()
            .FirstOrDefaultAsync(pp => pp.PriceId == priceId && pp.ProductId == productId, ct);

        if (price is null)
            return NotFound(new { message = $"Price with ID {priceId} was not found for product {productId}." });

        return Ok(MapPriceToResponse(price));
    }

    // ───────────────────────────── POST create price ───────────────────

    /// <summary>
    /// Creates a new price entry for a product.
    /// </summary>
    [HttpPost("{productId:int}/prices")]
    [ProducesResponseType(typeof(ProductPriceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProductPriceResponse>> CreatePrice(
        int productId,
        [FromBody] CreateProductPriceRequest request,
        CancellationToken ct)
    {
        var productExists = await db.Products.AnyAsync(p => p.ProductId == productId, ct);
        if (!productExists)
            return NotFound(new { message = $"Product with ID {productId} was not found." });

        // Validate Size FK
        if (request.SizeId.HasValue)
        {
            var sizeExists = await db.Sizes.AnyAsync(s => s.SizeId == request.SizeId.Value, ct);
            if (!sizeExists)
                return UnprocessableEntity(new { message = $"Size with ID {request.SizeId} does not exist." });
        }

        // Validate Variant FK
        if (request.VariantId.HasValue)
        {
            var variantExists = await db.ProductVariants.AnyAsync(v => v.ProductVariantId == request.VariantId.Value, ct);
            if (!variantExists)
                return UnprocessableEntity(new { message = $"Variant with ID {request.VariantId} does not exist." });
        }

        // Validate ProductCombo FK
        if (request.ProductComboId.HasValue)
        {
            var comboExists = await db.ProductCombos.AnyAsync(c => c.ProductComboId == request.ProductComboId.Value, ct);
            if (!comboExists)
                return UnprocessableEntity(new { message = $"Product combo with ID {request.ProductComboId} does not exist." });
        }

        var price = new ProductPrice
        {
            ProductId = productId,
            Price = request.Price,
            SizeId = request.SizeId,
            VariantId = request.VariantId,
            ProductComboId = request.ProductComboId,
            CreateDate = DateTime.UtcNow,
        };

        db.ProductPrices.Add(price);
        await db.SaveChangesAsync(ct);

        // Reload navigations for response
        await db.Entry(price).Reference(p => p.Size).LoadAsync(ct);
        await db.Entry(price).Reference(p => p.Variant).LoadAsync(ct);

        return CreatedAtAction(nameof(GetPrice), new { productId, priceId = price.PriceId }, MapPriceToResponse(price));
    }

    // ───────────────────────────── PUT update price ────────────────────

    /// <summary>
    /// Updates the price value of an existing price entry.
    /// </summary>
    [HttpPut("{productId:int}/prices/{priceId:int}")]
    [ProducesResponseType(typeof(ProductPriceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductPriceResponse>> UpdatePrice(
        int productId, int priceId,
        [FromBody] UpdateProductPriceRequest request,
        CancellationToken ct)
    {
        var price = await db.ProductPrices
            .Include(pp => pp.Size)
            .Include(pp => pp.Variant)
            .FirstOrDefaultAsync(pp => pp.PriceId == priceId && pp.ProductId == productId, ct);

        if (price is null)
            return NotFound(new { message = $"Price with ID {priceId} was not found for product {productId}." });

        price.Price = request.Price;
        await db.SaveChangesAsync(ct);

        return Ok(MapPriceToResponse(price));
    }

    // ───────────────────────────── DELETE price ────────────────────────

    /// <summary>
    /// Deletes a price entry.
    /// </summary>
    [HttpDelete("{productId:int}/prices/{priceId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePrice(
        int productId, int priceId, CancellationToken ct)
    {
        var price = await db.ProductPrices
            .FirstOrDefaultAsync(pp => pp.PriceId == priceId && pp.ProductId == productId, ct);

        if (price is null)
            return NotFound(new { message = $"Price with ID {priceId} was not found for product {productId}." });

        db.ProductPrices.Remove(price);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Product Picture
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the main picture for a product (product_picture_id FK).
    /// Falls back to the first picture in ProductPictures if main FK is null.
    /// </summary>
    [HttpGet("{productId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetPicture(int productId, CancellationToken ct)
    {
        // Try the "main" picture first
        var product = await db.Products
            .AsNoTracking()
            .Where(p => p.ProductId == productId)
            .Select(p => new { p.ProductPictureId })
            .FirstOrDefaultAsync(ct);

        if (product is null)
            return NotFound(new { message = $"Producto con ID {productId} no encontrado." });

        byte[]? pictureBytes = null;

        if (product.ProductPictureId is not null)
        {
            pictureBytes = await db.ProductPictures
                .AsNoTracking()
                .Where(pp => pp.ProductPictureId == product.ProductPictureId)
                .Select(pp => pp.Picture)
                .FirstOrDefaultAsync(ct);
        }

        // Fallback: first available picture
        pictureBytes ??= await db.ProductPictures
            .AsNoTracking()
            .Where(pp => pp.ProductId == productId)
            .OrderBy(pp => pp.ProductPictureId)
            .Select(pp => pp.Picture)
            .FirstOrDefaultAsync(ct);

        if (pictureBytes is null or { Length: 0 })
            return NotFound(new { message = "Este producto no tiene imagen." });

        return File(pictureBytes, "image/png");
    }

    /// <summary>
    /// Returns the picture for a specific variant of a product.
    /// </summary>
    [HttpGet("{productId:int}/variants/{variantId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetVariantPicture(int productId, int variantId, CancellationToken ct)
    {
        var pictureBytes = await db.ProductPictures
            .AsNoTracking()
            .Where(pp => pp.ProductId == productId && pp.VariantId == variantId)
            .OrderBy(pp => pp.ProductPictureId)
            .Select(pp => pp.Picture)
            .FirstOrDefaultAsync(ct);

        if (pictureBytes is null or { Length: 0 })
            return NotFound(new { message = "No se encontró imagen para esta variante del producto." });

        return File(pictureBytes, "image/png");
    }

    /// <summary>
    /// Uploads or replaces the picture for a specific variant of a product.
    /// Creates the record if it doesn't exist (upsert).
    /// </summary>
    [HttpPut("{productId:int}/variants/{variantId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVariantPicture(
        int productId, int variantId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No se proporcionó un archivo válido." });

        var productExists = await db.Products.AnyAsync(p => p.ProductId == productId, ct);
        if (!productExists)
            return NotFound(new { message = $"Producto con ID {productId} no encontrado." });

        var variantExists = await db.ProductVariants.AnyAsync(v => v.ProductVariantId == variantId, ct);
        if (!variantExists)
            return NotFound(new { message = $"Variante con ID {variantId} no encontrada." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        var existing = await db.ProductPictures
            .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.VariantId == variantId, ct);

        if (existing is not null)
        {
            existing.Picture = bytes;
        }
        else
        {
            db.ProductPictures.Add(new ProductPicture
            {
                ProductId = productId,
                VariantId = variantId,
                Picture = bytes,
                CreateDate = DateOnly.FromDateTime(DateTime.UtcNow),
            });
        }

        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Imagen de variante actualizada correctamente." });
    }

    /// <summary>
    /// Deletes the picture for a specific variant of a product.
    /// </summary>
    [HttpDelete("{productId:int}/variants/{variantId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariantPicture(int productId, int variantId, CancellationToken ct)
    {
        var picture = await db.ProductPictures
            .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.VariantId == variantId, ct);

        if (picture is null)
            return NotFound(new { message = "No se encontró imagen para esta variante del producto." });

        db.ProductPictures.Remove(picture);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Uploads or replaces the main picture for a product (no variant).
    /// Creates the record if it doesn't exist (upsert), and sets it as the product's main picture.
    /// </summary>
    [HttpPut("{productId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProductPicture(
        int productId,
        IFormFile file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No se proporcionó un archivo válido." });

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product is null)
            return NotFound(new { message = $"Producto con ID {productId} no encontrado." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        var bytes = ms.ToArray();

        // Look for existing main picture (no variant)
        ProductPicture? existing = null;

        if (product.ProductPictureId is not null)
        {
            existing = await db.ProductPictures
                .FirstOrDefaultAsync(pp => pp.ProductPictureId == product.ProductPictureId, ct);
        }

        existing ??= await db.ProductPictures
            .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.VariantId == null, ct);

        if (existing is not null)
        {
            existing.Picture = bytes;
            product.ProductPictureId = existing.ProductPictureId;
        }
        else
        {
            var newPic = new ProductPicture
            {
                ProductId = productId,
                Picture = bytes,
                CreateDate = DateOnly.FromDateTime(DateTime.UtcNow),
            };
            db.ProductPictures.Add(newPic);
            await db.SaveChangesAsync(ct);

            product.ProductPictureId = newPic.ProductPictureId;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new { message = "Imagen de producto actualizada correctamente." });
    }

    /// <summary>
    /// Deletes the main picture for a product.
    /// </summary>
    [HttpDelete("{productId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProductPicture(int productId, CancellationToken ct)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.ProductId == productId, ct);

        if (product is null)
            return NotFound(new { message = $"Producto con ID {productId} no encontrado." });

        // Find the main picture
        ProductPicture? picture = null;

        if (product.ProductPictureId is not null)
        {
            picture = await db.ProductPictures
                .FirstOrDefaultAsync(pp => pp.ProductPictureId == product.ProductPictureId, ct);
        }

        picture ??= await db.ProductPictures
            .FirstOrDefaultAsync(pp => pp.ProductId == productId && pp.VariantId == null, ct);

        if (picture is null)
            return NotFound(new { message = "Este producto no tiene imagen principal." });

        product.ProductPictureId = null;
        db.ProductPictures.Remove(picture);
        await db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public class BrandLookup
{
    public int BrandId { get; set; }
    public string? Name { get; set; }
}
