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
        // SchoolCount = product.ProductVisualDefinitions
        //     .SelectMany(vd => vd.ProductVisualDefinitionSchools)
        //     .Select(pvds => pvds.SchoolId)
        //     .Distinct()
        //     .Count(),
        SchoolCount = 0,
        HasPicture = product.ProductImageTarget is not null,
        // StockTotal = product.InventoryItemDefinitions
        //     .SelectMany(d => d.InventoryBalances)
        //     .Sum(b => b.OnHand ?? 0),
        StockTotal = 0,
        LatestCost = product.ProductCosts
            .OrderByDescending(c => c.CreateDate)
            .Select(c => (decimal?)c.Cost)
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
            // .Include(p => p.InventoryItemDefinitions)
            //     .ThenInclude(d => d.InventoryBalances)
            // .Include(p => p.ProductVisualDefinitions)
            //     .ThenInclude(vd => vd.ProductVisualDefinitionSchools)
            .Include(p => p.ProductImageTarget)
            .Include(p => p.ProductCosts)
            .Include(p => p.ProductVariantSystems)
            .AsNoTracking()
            .AsQueryable();

        // ── Filters ──
        if (query.BrandId.HasValue)
            q = q.Where(p => p.BrandId == query.BrandId.Value);

        if (query.CategoryId.HasValue)
            q = q.Where(p => p.Categories.Any(c => c.CategoryId == query.CategoryId.Value));

        if (query.HasSchools == true)
            q = q.Where(p => p.ProductVisualDefinitions.Any(vd => vd.ProductVisualDefinitionSchools.Any()));
        else if (query.HasSchools == false)
            q = q.Where(p => !p.ProductVisualDefinitions.Any(vd => vd.ProductVisualDefinitionSchools.Any()));

        if (!string.IsNullOrWhiteSpace(query.Model))
        {
            var modelTerm = query.Model.ToLower();
            q = q.Where(p => p.Model != null && p.Model.ToLower().Contains(modelTerm));
        }

        if (query.Serialize.HasValue)
            q = q.Where(p => p.Serialize == query.Serialize.Value);

        if (query.HasStock == true)
            q = q.Where(p => p.InventoryItemDefinitions
                .SelectMany(d => d.InventoryBalances)
                .Any(b => (b.OnHand ?? 0) > 0));
        else if (query.HasStock == false)
            q = q.Where(p => !p.InventoryItemDefinitions
                .SelectMany(d => d.InventoryBalances)
                .Any(b => (b.OnHand ?? 0) > 0));

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
                ? q.OrderByDescending(p => p.ProductVisualDefinitions.SelectMany(vd => vd.ProductVisualDefinitionSchools).Select(pvds => pvds.SchoolId).Distinct().Count())
                : q.OrderBy(p => p.ProductVisualDefinitions.SelectMany(vd => vd.ProductVisualDefinitionSchools).Select(pvds => pvds.SchoolId).Distinct().Count()),
            "stocktotal" => query.SortDescending
                ? q.OrderByDescending(p => p.InventoryItemDefinitions.SelectMany(d => d.InventoryBalances).Sum(b => b.OnHand ?? 0))
                : q.OrderBy(p => p.InventoryItemDefinitions.SelectMany(d => d.InventoryBalances).Sum(b => b.OnHand ?? 0)),
            "latestcost" => query.SortDescending
                ? q.OrderByDescending(p => p.ProductCosts.OrderByDescending(pc => pc.CreateDate).Select(pc => (decimal?)pc.Cost).FirstOrDefault())
                : q.OrderBy(p => p.ProductCosts.OrderByDescending(pc => pc.CreateDate).Select(pc => (decimal?)pc.Cost).FirstOrDefault()),
            _ => q.OrderBy(p => p.Name), // default sort
        };

        var totalCount = await q.CountAsync(ct);

        var products = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        var items = products.Select(MapToResponse).ToList();

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
            // .Include(p => p.InventoryItemDefinitions)
            //     .ThenInclude(d => d.InventoryBalances)
            // .Include(p => p.ProductVisualDefinitions)
            //     .ThenInclude(vd => vd.ProductVisualDefinitionSchools)
            .Include(p => p.ProductImageTarget)
            .Include(p => p.ProductCosts)
            .Include(p => p.ProductVariantSystems)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProductId == id, ct);

        if (product is null)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        return Ok(MapToResponse(product));
    }

    // ───────────────────────────── GET schools ─────────────────────────

    /// <summary>
    /// Returns the schools (with school level) linked via visual definitions for this product.
    /// </summary>
    [HttpGet("{id:int}/schools")]
    [ProducesResponseType(typeof(List<ProductSchoolInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ProductSchoolInfo>>> GetSchools(int id, CancellationToken ct)
    {
        var exists = await db.Products.AnyAsync(p => p.ProductId == id, ct);
        if (!exists)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        var schools = await db.ProductVisualDefinitionSchools
            .AsNoTracking()
            .Where(pvds => pvds.ProductVisualDefinition.ProductId == id && pvds.IsActive)
            .Select(pvds => new
            {
                pvds.SchoolId,
                pvds.School.Name,
                pvds.School.SchoolLevelId,
                SchoolLevelName = pvds.School.SchoolLevel.Name,
                pvds.ProductVisualDefinitionId,
            })
            .Distinct()
            .OrderBy(s => s.SchoolLevelName)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

        var result = schools.Select(s => new ProductSchoolInfo
        {
            SchoolId = s.SchoolId,
            Name = s.Name,
            SchoolLevelId = s.SchoolLevelId,
            SchoolLevelName = s.SchoolLevelName,
            ProductVisualDefinitionId = s.ProductVisualDefinitionId,
        }).ToList();

        return Ok(result);
    }

    // ───────────────────── GET item-definitions ────────────────────────

    /// <summary>
    /// Returns the inventory item definitions (variant-derived items) for a product,
    /// including size, variant info, and whether an image exists.
    /// </summary>
    [HttpGet("{id:int}/item-definitions")]
    [ProducesResponseType(typeof(List<ItemDefinitionSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ItemDefinitionSummary>>> GetItemDefinitions(
        int id, CancellationToken ct)
    {
        var exists = await db.Products.AnyAsync(p => p.ProductId == id, ct);
        if (!exists)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        var items = await db.InventoryItemDefinitions
            .AsNoTracking()
            .Where(d => d.ProductId == id)
            .Include(d => d.Size)
            .Include(d => d.ProductVariants)
                .ThenInclude(v => v.ProductVariantSystem)
            .Include(d => d.ProductImageTarget)
            .OrderBy(d => d.SkuCode)
            .Select(d => new ItemDefinitionSummary
            {
                InventoryItemDefinitionId = d.InventoryItemDefinitionId,
                SkuCode = d.SkuCode,
                NameSnapshot = d.NameSnapshot,
                IsSerialized = d.IsSerialized,
                IsActive = d.IsActive,
                SizeId = d.SizeId,
                SizeValue = d.Size != null ? d.Size.SizeValue : null,
                HasImage = d.ProductImageTarget != null,
                Variants = d.ProductVariants
                    .OrderBy(v => v.ProductVariantSystem.Name)
                    .ThenBy(v => v.VariantValue)
                    .Select(v => new ItemDefinitionVariantInfo
                    {
                        ProductVariantId = v.ProductVariantId,
                        VariantValue = v.VariantValue,
                        SystemName = v.ProductVariantSystem.Name,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ───────────────── GET visual-definitions (grouped) ───────────────

    /// <summary>
    /// Returns the product's visual definitions, each with its variant info
    /// and nested item definitions (sizes) including effective prices.
    /// Price resolution follows: ITEM_DEFINITION → VISUAL_DEFINITION → PRODUCT.
    /// </summary>
    [HttpGet("{id:int}/visual-definitions")]
    [ProducesResponseType(typeof(List<VisualDefinitionGroup>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VisualDefinitionGroup>>> GetVisualDefinitions(
        int id,
        [FromQuery] int? priceListId,
        CancellationToken ct)
    {
        var exists = await db.Products.AnyAsync(p => p.ProductId == id, ct);
        if (!exists)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        // Resolve price list: use provided or default
        int resolvedPriceListId;
        if (priceListId.HasValue)
        {
            resolvedPriceListId = priceListId.Value;
        }
        else
        {
            var defaultList = await db.PriceLists
                .AsNoTracking()
                .Where(pl => pl.IsDefault && pl.IsActive)
                .Select(pl => (int?)pl.PriceListId)
                .FirstOrDefaultAsync(ct);
            resolvedPriceListId = defaultList ?? 0;
        }

        var now = DateTime.UtcNow;

        var groups = await db.ProductVisualDefinitions
            .AsNoTracking()
            .Where(vd => vd.ProductId == id)
            .Include(vd => vd.ProductVariants)
                .ThenInclude(v => v.ProductVariantSystem)
            .Include(vd => vd.ProductImageTarget)
            .Include(vd => vd.InventoryItemDefinitions)
                .ThenInclude(d => d.Size)
            .Include(vd => vd.ProductVisualDefinitionSchools)
                .ThenInclude(pvds => pvds.School)
                    .ThenInclude(s => s.SchoolLevel)
            .Include(vd => vd.ProductVisualDefinitionEmbroideries)
                .ThenInclude(pvde => pvde.Embroidery)
            .OrderBy(vd => vd.ProductVisualDefinitionId)
            .Select(vd => new VisualDefinitionGroup
            {
                ProductVisualDefinitionId = vd.ProductVisualDefinitionId,
                VariantsHash = vd.VariantsHash,
                HasImage = vd.ProductImageTarget != null,
                Variants = vd.ProductVariants
                    .OrderBy(v => v.ProductVariantSystem.Name)
                    .ThenBy(v => v.VariantValue)
                    .Select(v => new ItemDefinitionVariantInfo
                    {
                        ProductVariantId = v.ProductVariantId,
                        VariantValue = v.VariantValue,
                        SystemName = v.ProductVariantSystem.Name,
                    })
                    .ToList(),
                Schools = vd.ProductVisualDefinitionSchools
                    .Where(pvds => pvds.IsActive)
                    .OrderBy(pvds => pvds.School.SchoolLevel.Name)
                    .ThenBy(pvds => pvds.School.Name)
                    .Select(pvds => new VisualDefinitionSchoolInfo
                    {
                        SchoolId = pvds.SchoolId,
                        Name = pvds.School.Name,
                        SchoolLevelName = pvds.School.SchoolLevel.Name,
                    })
                    .ToList(),
                Embroideries = vd.ProductVisualDefinitionEmbroideries
                    .OrderBy(pvde => pvde.Placement)
                    .ThenBy(pvde => pvde.Embroidery.Name)
                    .Select(pvde => new VisualDefinitionEmbroideryInfo
                    {
                        EmbroideryId = pvde.EmbroideryId,
                        Name = pvde.Embroidery.Name,
                        Placement = pvde.Placement,
                        IsRequired = pvde.IsRequired,
                    })
                    .ToList(),
                Items = vd.InventoryItemDefinitions
                    .OrderBy(d => d.Size != null ? d.Size.SizeOrder : int.MaxValue)
                    .ThenBy(d => d.Size != null ? d.Size.SizeValue : "")
                    .ThenBy(d => d.SkuCode)
                    .Select(d => new VisualDefinitionItem
                    {
                        InventoryItemDefinitionId = d.InventoryItemDefinitionId,
                        SkuCode = d.SkuCode,
                        NameSnapshot = d.NameSnapshot,
                        IsSerialized = d.IsSerialized,
                        IsActive = d.IsActive,
                        SizeId = d.SizeId,
                        SizeValue = d.Size != null ? d.Size.SizeValue : null,
                        SizeOrder = d.Size != null ? d.Size.SizeOrder : null,
                    })
                    .ToList(),
            })
            .ToListAsync(ct);

        // Also include item definitions without a visual definition (ungrouped)
        var ungroupedItems = await db.InventoryItemDefinitions
            .AsNoTracking()
            .Where(d => d.ProductId == id && d.ProductVisualDefinitionId == null)
            .Include(d => d.Size)
            .OrderBy(d => d.Size != null ? d.Size.SizeOrder : int.MaxValue)
            .ThenBy(d => d.Size != null ? d.Size.SizeValue : "")
            .ThenBy(d => d.SkuCode)
            .Select(d => new VisualDefinitionItem
            {
                InventoryItemDefinitionId = d.InventoryItemDefinitionId,
                SkuCode = d.SkuCode,
                NameSnapshot = d.NameSnapshot,
                IsSerialized = d.IsSerialized,
                IsActive = d.IsActive,
                SizeId = d.SizeId,
                SizeValue = d.Size != null ? d.Size.SizeValue : null,
                SizeOrder = d.Size != null ? d.Size.SizeOrder : null,
            })
            .ToListAsync(ct);

        if (ungroupedItems.Count > 0)
        {
            groups.Add(new VisualDefinitionGroup
            {
                ProductVisualDefinitionId = 0, // sentinel for "no visual definition"
                VariantsHash = "",
                HasImage = false,
                Variants = [],
                Items = ungroupedItems,
            });
        }

        // ── Resolve effective prices for all items ──
        if (resolvedPriceListId > 0)
        {
            // Collect all item definition ids and their visual definition ids
            var allItems = groups.SelectMany(g => g.Items.Select(i => new
            {
                Item = i,
                VisualDefId = g.ProductVisualDefinitionId > 0 ? (long?)g.ProductVisualDefinitionId : null,
            })).ToList();

            if (allItems.Count > 0)
            {
                var itemDefIds = allItems.Select(x => x.Item.InventoryItemDefinitionId).ToList();
                var visualDefIds = allItems
                    .Where(x => x.VisualDefId.HasValue)
                    .Select(x => x.VisualDefId!.Value)
                    .Distinct()
                    .ToList();

                // Batch-load all valid price entries at each scope
                var itemPrices = await db.PriceItemDefinitionEntries
                    .AsNoTracking()
                    .Where(e => e.PriceListId == resolvedPriceListId
                             && itemDefIds.Contains(e.InventoryItemDefinitionId)
                             && e.ValidFrom <= now
                             && (e.ValidTo == null || now < e.ValidTo))
                    .ToListAsync(ct);

                var visualPrices = visualDefIds.Count > 0
                    ? await db.PriceVisualDefinitionEntries
                        .AsNoTracking()
                        .Where(e => e.PriceListId == resolvedPriceListId
                                 && visualDefIds.Contains(e.ProductVisualDefinitionId)
                                 && e.ValidFrom <= now
                                 && (e.ValidTo == null || now < e.ValidTo))
                        .ToListAsync(ct)
                    : [];

                var productPrices = await db.PriceProductEntries
                    .AsNoTracking()
                    .Where(e => e.PriceListId == resolvedPriceListId
                             && e.ProductId == id
                             && e.ValidFrom <= now
                             && (e.ValidTo == null || now < e.ValidTo))
                    .ToListAsync(ct);

                // Resolve per item
                foreach (var x in allItems)
                {
                    // Scope 1: ITEM_DEFINITION
                    var scope1 = itemPrices
                        .Where(e => e.InventoryItemDefinitionId == x.Item.InventoryItemDefinitionId)
                        .OrderByDescending(e => e.PriceKind) // PROMO > BASE
                        .ThenByDescending(e => e.Priority)
                        .ThenByDescending(e => e.ValidFrom)
                        .FirstOrDefault();

                    if (scope1 is not null)
                    {
                        ApplyPrice(x.Item, scope1.PriceAmount, scope1.PriceKind, "ITEM_DEFINITION",
                            scope1.PriceKind == "PROMO" ? scope1.PromoName : null);
                        if (scope1.PriceKind == "PROMO")
                        {
                            var baseAtScope = itemPrices
                                .Where(e => e.InventoryItemDefinitionId == x.Item.InventoryItemDefinitionId && e.PriceKind == "BASE")
                                .OrderByDescending(e => e.ValidFrom)
                                .FirstOrDefault();
                            x.Item.BasePriceAmount = baseAtScope?.PriceAmount;
                        }
                        continue;
                    }

                    // Scope 2: VISUAL_DEFINITION
                    if (x.VisualDefId.HasValue)
                    {
                        var scope2 = visualPrices
                            .Where(e => e.ProductVisualDefinitionId == x.VisualDefId.Value)
                            .OrderByDescending(e => e.PriceKind)
                            .ThenByDescending(e => e.Priority)
                            .ThenByDescending(e => e.ValidFrom)
                            .FirstOrDefault();

                        if (scope2 is not null)
                        {
                            ApplyPrice(x.Item, scope2.PriceAmount, scope2.PriceKind, "VISUAL_DEFINITION",
                                scope2.PriceKind == "PROMO" ? scope2.PromoName : null);
                            if (scope2.PriceKind == "PROMO")
                            {
                                var baseAtScope = visualPrices
                                    .Where(e => e.ProductVisualDefinitionId == x.VisualDefId.Value && e.PriceKind == "BASE")
                                    .OrderByDescending(e => e.ValidFrom)
                                    .FirstOrDefault();
                                x.Item.BasePriceAmount = baseAtScope?.PriceAmount;
                            }
                            continue;
                        }
                    }

                    // Scope 3: PRODUCT
                    var scope3 = productPrices
                        .OrderByDescending(e => e.PriceKind)
                        .ThenByDescending(e => e.Priority)
                        .ThenByDescending(e => e.ValidFrom)
                        .FirstOrDefault();

                    if (scope3 is not null)
                    {
                        ApplyPrice(x.Item, scope3.PriceAmount, scope3.PriceKind, "PRODUCT",
                            scope3.PriceKind == "PROMO" ? scope3.PromoName : null);
                        if (scope3.PriceKind == "PROMO")
                        {
                            var baseAtScope = productPrices
                                .Where(e => e.PriceKind == "BASE")
                                .OrderByDescending(e => e.ValidFrom)
                                .FirstOrDefault();
                            x.Item.BasePriceAmount = baseAtScope?.PriceAmount;
                        }
                    }
                }
            }
        }

        // ── Resolve inventory balances (aggregated + by site) ──
        {
            var allItemDefIds = groups.SelectMany(g => g.Items.Select(i => i.InventoryItemDefinitionId)).ToList();
            if (allItemDefIds.Count > 0)
            {
                var balances = await db.InventoryBalances
                    .AsNoTracking()
                    .Where(b => allItemDefIds.Contains(b.InventoryItemDefinitionId))
                    .Include(b => b.Site)
                    .Select(b => new
                    {
                        DefId = b.InventoryItemDefinitionId,
                        b.SiteId,
                        SiteName = b.Site.Name,
                        OnHand = b.OnHand ?? 0,
                        Reserved = b.Reserved ?? 0,
                    })
                    .ToListAsync(ct);

                var stockByDefMap = balances
                    .GroupBy(b => b.DefId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.OrderBy(x => x.SiteName)
                            .Select(x => new VisualDefinitionItemSiteStock
                            {
                                SiteId = x.SiteId,
                                SiteName = x.SiteName,
                                OnHand = x.OnHand,
                                Reserved = x.Reserved,
                            })
                            .ToList());

                foreach (var item in groups.SelectMany(g => g.Items))
                {
                    item.StockBySite = stockByDefMap.GetValueOrDefault(item.InventoryItemDefinitionId, []);
                    item.StockTotal = item.StockBySite.Sum(s => s.OnHand);
                }
            }
        }

        return Ok(groups);

        static void ApplyPrice(VisualDefinitionItem item, decimal amount, string kind, string scope, string? promoName)
        {
            item.PriceAmount = amount;
            item.PriceKind = kind;
            item.PriceScope = scope;
            item.PromoName = promoName;
        }
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
        await db.Entry(product).Collection(p => p.InventoryItemDefinitions).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.ProductVisualDefinitions).LoadAsync(ct);
        await db.Entry(product).Reference(p => p.ProductImageTarget).LoadAsync(ct);
        await db.Entry(product).Collection(p => p.ProductCosts).LoadAsync(ct);
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
            .Include(p => p.InventoryItemDefinitions)
                .ThenInclude(d => d.InventoryBalances)
            .Include(p => p.ProductVisualDefinitions)
                .ThenInclude(vd => vd.ProductVisualDefinitionSchools)
            .Include(p => p.ProductImageTarget)
            .Include(p => p.ProductCosts)
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
            .Include(p => p.InventoryItemDefinitions)
                .ThenInclude(d => d.InventoryBalances)
            .Include(p => p.ProductVisualDefinitions)
                .ThenInclude(vd => vd.ProductVisualDefinitionSchools)
            .Include(p => p.SalesLines)
            .Include(p => p.ProductCosts)
            .FirstOrDefaultAsync(p => p.ProductId == id, ct);

        if (product is null)
            return NotFound(new { message = $"Product with ID {id} was not found." });

        // Guard against deleting products with related data
        var conflicts = new List<string>();
        var schoolCount = product.ProductVisualDefinitions
            .SelectMany(vd => vd.ProductVisualDefinitionSchools)
            .Select(pvds => pvds.SchoolId)
            .Distinct()
            .Count();
        var inventoryDefinitionCount = product.InventoryItemDefinitions.Count;
        var inventoryBalanceCount = product.InventoryItemDefinitions.Sum(d => d.InventoryBalances.Count);
        if (schoolCount > 0) conflicts.Add($"{schoolCount} escuela(s)");
        if (inventoryDefinitionCount > 0) conflicts.Add($"{inventoryDefinitionCount} definicion(es) de inventario");
        if (inventoryBalanceCount > 0) conflicts.Add($"{inventoryBalanceCount} registro(s) de balance de inventario");
        if (product.SalesLines.Count > 0) conflicts.Add($"{product.SalesLines.Count} linea(s) de venta");
        if (product.ProductCosts.Count > 0) conflicts.Add($"{product.ProductCosts.Count} costo(s)");

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

    // ═══════════════════════════════════════════════════════════════════
    //  Product Picture (legacy redirect to ImagesController)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the primary image for a product (redirects to new images API).
    /// Uses product_image_targets with is_primary=true fallback to sort_order.
    /// </summary>
    [HttpGet("{productId:int}/picture")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [AllowAnonymous]
    public async Task<IActionResult> GetPicture(int productId, CancellationToken ct)
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
}

public class BrandLookup
{
    public int BrandId { get; set; }
    public string? Name { get; set; }
}
