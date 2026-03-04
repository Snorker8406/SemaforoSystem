using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Inventory;
using SemaforoSystem.Server.Models;
using SemaforoSystem.Server.Services;

namespace SemaforoSystem.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
//[Authorize]
[Produces("application/json")]
public class InventoryController(InventoryService inventoryService, ApplicationDbContext db) : ControllerBase
{
    // ═══════════════════════════════════════════════════════════════════
    //  ENTRY (Goods Receipt)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an ENTRY transaction — receives goods into inventory.
    /// Supports both serialized and non-serialized items.
    /// </summary>
    /// <remarks>
    /// For serialized items, each line must include a <c>Barcodes</c> array
    /// with exactly <c>Quantity</c> unique barcode strings.
    /// The system will create individual serial items and link them to the ledger line.
    /// </remarks>
    [HttpPost("entries")]
    [ProducesResponseType(typeof(InventoryTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEntry(
        [FromBody] CreateEntryRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Basic validation: each line must have either a definition id or a product id
        for (int i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!line.InventoryItemDefinitionId.HasValue && !line.ProductId.HasValue)
            {
                ModelState.AddModelError($"Lines[{i}]",
                    "Either InventoryItemDefinitionId or ProductId must be provided.");
            }
        }

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var result = await inventoryService.CreateEntryAsync(request, userId, ct);

            return CreatedAtAction(nameof(GetTransaction),
                new { id = result.InventoryTransactionId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GET single transaction (used by CreatedAtAction)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a single inventory transaction by id.</summary>
    [HttpGet("transactions/{id:long}")]
    [ProducesResponseType(typeof(InventoryTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransaction(long id, CancellationToken ct)
    {
        var result = await inventoryService.GetTransactionAsync(id, ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Sites lookup (lightweight, for UI selectors)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns all sites as a lightweight lookup list.</summary>
    [HttpGet("sites")]
    [ProducesResponseType(typeof(IEnumerable<SiteLookup>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSites(CancellationToken ct)
    {
        var sites = await db.Sites
            .OrderBy(s => s.Name)
            .Select(s => new SiteLookup
            {
                SiteId = s.SiteId,
                Name = s.Name,
            })
            .ToListAsync(ct);

        return Ok(sites);
    }
}
