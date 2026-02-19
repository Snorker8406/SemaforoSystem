using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.SupplyProcess;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class SupplyProcessController(ApplicationDbContext db) : ControllerBase
{
    private const int EscolarCategoryId = 1;

    /// <summary>
    /// Returns all products in the "Escolar" category (category_id = 1)
    /// along with the schools each product is assigned to.
    /// </summary>
    /// <param name="minSchoolCount">
    /// Threshold: <c>SchoolCommonProduct</c> will be <c>true</c> when a product's
    /// school count is ≥ this value.
    /// </param>
    [HttpGet("school-common-products")]
    [ProducesResponseType(typeof(List<SchoolProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SchoolProductResponse>>> GetSchoolCommonProducts(
        [FromQuery] int minSchoolCount,
        CancellationToken ct)
    {
        var products = await db.Products
            .AsNoTracking()
            .Where(p => p.Categories.Any(c => c.CategoryId == EscolarCategoryId))
            .Select(p => new SchoolProductResponse
            {
                ProductId = p.ProductId,
                Name = p.Name,
                SerialCount = p.SerialCount,
                CategoryName = p.Categories
                    .Where(c => c.CategoryId == EscolarCategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefault() ?? "Escolar",
                SchoolNames = p.Schools
                    .OrderBy(s => s.Name)
                    .Select(s => s.SchoolLevel.Name + " " + s.Name)
                    .ToList(),
                SchoolCount = p.Schools.Count,
                SchoolCommonProduct = p.Schools.Count >= minSchoolCount,
            })
            .OrderByDescending(p => p.SchoolCount)
            .ThenBy(p => p.Name)
            .ToListAsync(ct);

        return Ok(products);
    }

    /// <summary>
    /// Returns all schools that have at least one product in the "Escolar" category,
    /// with each product containing its related schools list and the common-product flag.
    /// </summary>
    /// <param name="minSchoolCount">
    /// Threshold: <c>SchoolCommonProduct</c> will be <c>true</c> when a product's
    /// school count is ≥ this value.
    /// </param>
    [HttpGet("schools-with-products")]
    [ProducesResponseType(typeof(List<SchoolWithProductsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SchoolWithProductsResponse>>> GetSchoolsWithProducts(
        [FromQuery] int minSchoolCount,
        CancellationToken ct)
    {
        var schools = await db.Schools
            .AsNoTracking()
            .Where(s => s.Products.Any(p => p.Categories.Any(c => c.CategoryId == EscolarCategoryId)))
            .Select(s => new SchoolWithProductsResponse
            {
                SchoolId = s.SchoolId,
                Name = s.Name,
                SchoolLevelName = s.SchoolLevel.Name,
                Address = s.Address,
                Ciudad = s.Ciudad,
                State = s.State,
                ProductCount = s.Products
                    .Count(p => p.Categories.Any(c => c.CategoryId == EscolarCategoryId)),
                Products = s.Products
                    .Where(p => p.Categories.Any(c => c.CategoryId == EscolarCategoryId))
                    .Select(p => new SchoolProductResponse
                    {
                        ProductId = p.ProductId,
                        Name = p.Name,
                        SerialCount = p.SerialCount,
                        CategoryName = p.Categories
                            .Where(c => c.CategoryId == EscolarCategoryId)
                            .Select(c => c.Name)
                            .FirstOrDefault() ?? "Escolar",
                        SchoolNames = p.Schools
                            .OrderBy(ps => ps.Name)
                            .Select(ps => ps.SchoolLevel.Name + " " + ps.Name)
                            .ToList(),
                        SchoolCount = p.Schools.Count,
                        SchoolCommonProduct = p.Schools.Count >= minSchoolCount,
                    })
                    .OrderByDescending(p => p.SchoolCount)
                    .ThenBy(p => p.Name)
                    .ToList(),
            })
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return Ok(schools);
    }
}
