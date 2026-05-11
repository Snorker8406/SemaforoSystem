using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Providers;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

/// <summary>
/// CRUD and maintenance endpoints for the provider master directory
/// (providers, contacts, addresses, bank accounts).
/// </summary>
/// <remarks>
/// Per the supplier/purchasing guide this controller manages master data only.
/// It must not produce inventory movements or payable transactions.
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProvidersController(ApplicationDbContext db) : ControllerBase
{
    // ───────────────────────────── helpers ──────────────────────────────

    private static ProviderResponse MapToResponse(
        Provider p,
        int contactsCount,
        int addressesCount,
        int bankAccountsCount) => new()
        {
            ProviderId = p.ProviderId,
            ProviderStatusId = p.ProviderStatusId,
            ProviderStatusCode = p.ProviderStatus?.Code ?? string.Empty,
            ProviderStatusName = p.ProviderStatus?.Name ?? string.Empty,
            LegalName = p.LegalName,
            TradeName = p.TradeName,
            TaxId = p.TaxId,
            Website = p.Website,
            Notes = p.Notes,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            ContactsCount = contactsCount,
            AddressesCount = addressesCount,
            BankAccountsCount = bankAccountsCount,
        };

    private static ProviderContactResponse MapContact(ProviderContact c) => new()
    {
        ProviderContactId = c.ProviderContactId,
        ProviderId = c.ProviderId,
        Name = c.Name,
        Role = c.Role,
        Email = c.Email,
        Phone = c.Phone,
        Cellphone = c.Cellphone,
        Whatsapp = c.Whatsapp,
        IsPrimary = c.IsPrimary,
        Notes = c.Notes,
        CreatedAt = c.CreatedAt,
    };

    private static ProviderAddressResponse MapAddress(ProviderAddress a) => new()
    {
        ProviderAddressId = a.ProviderAddressId,
        ProviderId = a.ProviderId,
        AddressType = a.AddressType,
        AddressLine = a.AddressLine,
        City = a.City,
        State = a.State,
        PostalCode = a.PostalCode,
        Country = a.Country,
        IsPrimary = a.IsPrimary,
        CreatedAt = a.CreatedAt,
    };

    private static ProviderBankAccountResponse MapBank(ProviderBankAccount b) => new()
    {
        ProviderBankAccountId = b.ProviderBankAccountId,
        ProviderId = b.ProviderId,
        BankName = b.BankName,
        AccountHolder = b.AccountHolder,
        AccountNumber = b.AccountNumber,
        Clabe = b.Clabe,
        CurrencyCode = b.CurrencyCode,
        IsPrimary = b.IsPrimary,
        Notes = b.Notes,
        CreatedAt = b.CreatedAt,
    };

    private Task<bool> ProviderExistsAsync(int id, CancellationToken ct)
        => db.Providers.AnyAsync(p => p.ProviderId == id, ct);

    private async Task ClearPrimaryContactsAsync(int providerId, long? excludeId, CancellationToken ct)
    {
        var rows = await db.ProviderContacts
            .Where(c => c.ProviderId == providerId && c.IsPrimary &&
                        (excludeId == null || c.ProviderContactId != excludeId))
            .ToListAsync(ct);

        foreach (var r in rows) r.IsPrimary = false;
    }

    private async Task ClearPrimaryAddressesAsync(int providerId, long? excludeId, CancellationToken ct)
    {
        var rows = await db.ProviderAddresses
            .Where(a => a.ProviderId == providerId && a.IsPrimary &&
                        (excludeId == null || a.ProviderAddressId != excludeId))
            .ToListAsync(ct);

        foreach (var r in rows) r.IsPrimary = false;
    }

    private async Task ClearPrimaryBankAccountsAsync(int providerId, long? excludeId, CancellationToken ct)
    {
        var rows = await db.ProviderBankAccounts
            .Where(b => b.ProviderId == providerId && b.IsPrimary &&
                        (excludeId == null || b.ProviderBankAccountId != excludeId))
            .ToListAsync(ct);

        foreach (var r in rows) r.IsPrimary = false;
    }

    // ═════════════════════════════════════════════════════════════════════
    //                          PROVIDER MASTER CRUD
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns a paginated, filterable and sortable list of providers.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProviderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProviderResponse>>> GetAll(
        [FromQuery] ProviderQueryParameters query,
        CancellationToken ct)
    {
        var q = db.Providers
            .Include(p => p.ProviderStatus)
            .AsNoTracking()
            .AsQueryable();

        // ── Filters ──
        if (query.ProviderStatusId.HasValue)
            q = q.Where(p => p.ProviderStatusId == query.ProviderStatusId.Value);

        if (!string.IsNullOrWhiteSpace(query.StatusCode))
        {
            var code = query.StatusCode.ToLower();
            q = q.Where(p => p.ProviderStatus.Code.ToLower() == code);
        }

        if (!string.IsNullOrWhiteSpace(query.TaxId))
            q = q.Where(p => p.TaxId != null && p.TaxId.ToLower() == query.TaxId.ToLower());

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(p =>
                p.LegalName.ToLower().Contains(term) ||
                (p.TradeName != null && p.TradeName.ToLower().Contains(term)) ||
                (p.TaxId != null && p.TaxId.ToLower().Contains(term)) ||
                (p.Website != null && p.Website.ToLower().Contains(term)));
        }

        // ── Sorting ──
        q = query.SortBy?.ToLower() switch
        {
            "legalname" => query.SortDescending ? q.OrderByDescending(p => p.LegalName) : q.OrderBy(p => p.LegalName),
            "tradename" => query.SortDescending ? q.OrderByDescending(p => p.TradeName) : q.OrderBy(p => p.TradeName),
            "taxid" => query.SortDescending ? q.OrderByDescending(p => p.TaxId) : q.OrderBy(p => p.TaxId),
            "status" => query.SortDescending ? q.OrderByDescending(p => p.ProviderStatus.Name) : q.OrderBy(p => p.ProviderStatus.Name),
            "createdat" => query.SortDescending ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            "updatedat" => query.SortDescending ? q.OrderByDescending(p => p.UpdatedAt) : q.OrderBy(p => p.UpdatedAt),
            _ => q.OrderBy(p => p.LegalName),
        };

        var totalCount = await q.CountAsync(ct);

        var page = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new
            {
                Provider = p,
                Contacts = db.ProviderContacts.Count(c => c.ProviderId == p.ProviderId),
                Addresses = db.ProviderAddresses.Count(a => a.ProviderId == p.ProviderId),
                Banks = db.ProviderBankAccounts.Count(b => b.ProviderId == p.ProviderId),
            })
            .ToListAsync(ct);

        var items = page
            .Select(x => MapToResponse(x.Provider, x.Contacts, x.Addresses, x.Banks))
            .ToList();

        return Ok(new PagedResponse<ProviderResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    /// <summary>
    /// Returns a provider summary by ID (without nested collections).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProviderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderResponse>> GetById(int id, CancellationToken ct)
    {
        var provider = await db.Providers
            .Include(p => p.ProviderStatus)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderId == id, ct);

        if (provider is null)
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        var contacts = await db.ProviderContacts.CountAsync(c => c.ProviderId == id, ct);
        var addresses = await db.ProviderAddresses.CountAsync(a => a.ProviderId == id, ct);
        var banks = await db.ProviderBankAccounts.CountAsync(b => b.ProviderId == id, ct);

        return Ok(MapToResponse(provider, contacts, addresses, banks));
    }

    /// <summary>
    /// Returns a provider including nested contacts, addresses and bank accounts.
    /// </summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType(typeof(ProviderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderDetailResponse>> GetDetail(int id, CancellationToken ct)
    {
        var provider = await db.Providers
            .Include(p => p.ProviderStatus)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderId == id, ct);

        if (provider is null)
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        var contacts = await db.ProviderContacts.AsNoTracking()
            .Where(c => c.ProviderId == id)
            .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
            .ToListAsync(ct);

        var addresses = await db.ProviderAddresses.AsNoTracking()
            .Where(a => a.ProviderId == id)
            .OrderByDescending(a => a.IsPrimary).ThenBy(a => a.AddressType)
            .ToListAsync(ct);

        var banks = await db.ProviderBankAccounts.AsNoTracking()
            .Where(b => b.ProviderId == id)
            .OrderByDescending(b => b.IsPrimary).ThenBy(b => b.BankName)
            .ToListAsync(ct);

        var summary = MapToResponse(provider, contacts.Count, addresses.Count, banks.Count);
        var detail = new ProviderDetailResponse
        {
            ProviderId = summary.ProviderId,
            ProviderStatusId = summary.ProviderStatusId,
            ProviderStatusCode = summary.ProviderStatusCode,
            ProviderStatusName = summary.ProviderStatusName,
            LegalName = summary.LegalName,
            TradeName = summary.TradeName,
            TaxId = summary.TaxId,
            Website = summary.Website,
            Notes = summary.Notes,
            CreatedAt = summary.CreatedAt,
            UpdatedAt = summary.UpdatedAt,
            ContactsCount = summary.ContactsCount,
            AddressesCount = summary.AddressesCount,
            BankAccountsCount = summary.BankAccountsCount,
            Contacts = contacts.Select(MapContact).ToList(),
            Addresses = addresses.Select(MapAddress).ToList(),
            BankAccounts = banks.Select(MapBank).ToList(),
        };

        return Ok(detail);
    }

    /// <summary>
    /// Creates a new provider master record.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProviderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProviderResponse>> Create(
        [FromBody] CreateProviderRequest request,
        CancellationToken ct)
    {
        var statusExists = await db.ProviderStatuses
            .AnyAsync(s => s.ProviderStatusId == request.ProviderStatusId, ct);

        if (!statusExists)
            return UnprocessableEntity(new { message = $"ProviderStatus with ID {request.ProviderStatusId} does not exist." });

        var nameExists = await db.Providers
            .AnyAsync(p => p.LegalName.ToLower() == request.LegalName.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"A provider with the legal name '{request.LegalName}' already exists." });

        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            var taxIdExists = await db.Providers
                .AnyAsync(p => p.TaxId != null && p.TaxId.ToLower() == request.TaxId.ToLower(), ct);

            if (taxIdExists)
                return Conflict(new { message = $"A provider with tax id '{request.TaxId}' already exists." });
        }

        var now = DateTime.UtcNow;
        var provider = new Provider
        {
            ProviderStatusId = request.ProviderStatusId,
            LegalName = request.LegalName.Trim(),
            TradeName = request.TradeName?.Trim(),
            TaxId = request.TaxId?.Trim(),
            Website = request.Website?.Trim(),
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Providers.Add(provider);
        await db.SaveChangesAsync(ct);

        await db.Entry(provider).Reference(p => p.ProviderStatus).LoadAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = provider.ProviderId },
            MapToResponse(provider, 0, 0, 0));
    }

    /// <summary>
    /// Fully updates a provider master record.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProviderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProviderResponse>> Update(
        int id,
        [FromBody] UpdateProviderRequest request,
        CancellationToken ct)
    {
        var provider = await db.Providers
            .Include(p => p.ProviderStatus)
            .FirstOrDefaultAsync(p => p.ProviderId == id, ct);

        if (provider is null)
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        if (provider.ProviderStatusId != request.ProviderStatusId)
        {
            var statusExists = await db.ProviderStatuses
                .AnyAsync(s => s.ProviderStatusId == request.ProviderStatusId, ct);

            if (!statusExists)
                return UnprocessableEntity(new { message = $"ProviderStatus with ID {request.ProviderStatusId} does not exist." });
        }

        var nameExists = await db.Providers
            .AnyAsync(p => p.ProviderId != id && p.LegalName.ToLower() == request.LegalName.ToLower(), ct);

        if (nameExists)
            return Conflict(new { message = $"A provider with the legal name '{request.LegalName}' already exists." });

        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            var taxIdExists = await db.Providers
                .AnyAsync(p => p.ProviderId != id && p.TaxId != null && p.TaxId.ToLower() == request.TaxId.ToLower(), ct);

            if (taxIdExists)
                return Conflict(new { message = $"A provider with tax id '{request.TaxId}' already exists." });
        }

        provider.ProviderStatusId = request.ProviderStatusId;
        provider.LegalName = request.LegalName.Trim();
        provider.TradeName = request.TradeName?.Trim();
        provider.TaxId = request.TaxId?.Trim();
        provider.Website = request.Website?.Trim();
        provider.Notes = request.Notes;
        provider.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await db.Entry(provider).Reference(p => p.ProviderStatus).LoadAsync(ct);

        var contacts = await db.ProviderContacts.CountAsync(c => c.ProviderId == id, ct);
        var addresses = await db.ProviderAddresses.CountAsync(a => a.ProviderId == id, ct);
        var banks = await db.ProviderBankAccounts.CountAsync(b => b.ProviderId == id, ct);

        return Ok(MapToResponse(provider, contacts, addresses, banks));
    }

    /// <summary>
    /// Updates only the provider status (e.g. ACTIVE / INACTIVE / BLOCKED).
    /// </summary>
    [HttpPatch("{id:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromBody] UpdateProviderStatusRequest request,
        CancellationToken ct)
    {
        var provider = await db.Providers.FirstOrDefaultAsync(p => p.ProviderId == id, ct);
        if (provider is null)
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        var statusExists = await db.ProviderStatuses
            .AnyAsync(s => s.ProviderStatusId == request.ProviderStatusId, ct);

        if (!statusExists)
            return UnprocessableEntity(new { message = $"ProviderStatus with ID {request.ProviderStatusId} does not exist." });

        provider.ProviderStatusId = request.ProviderStatusId;
        provider.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a provider. Blocked when the provider has related operational data
    /// (purchase orders, receipts, payables, product relations) per the supplier guide.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var provider = await db.Providers.FirstOrDefaultAsync(p => p.ProviderId == id, ct);
        if (provider is null)
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        // Guard against deleting providers with related operational data.
        var purchaseOrders = await db.PurchaseOrders.CountAsync(po => po.ProviderId == id, ct);
        var purchaseReceipts = await db.PurchaseReceipts.CountAsync(pr => pr.ProviderId == id, ct);
        var payables = await db.ProviderPayables.CountAsync(pp => pp.ProviderId == id, ct);
        var productLinks = await db.ProductProviders.CountAsync(pp => pp.ProviderId == id, ct);
        var visualLinks = await db.ProductVisualDefinitionProviders.CountAsync(pv => pv.ProviderId == id, ct);

        if (purchaseOrders > 0 || purchaseReceipts > 0 || payables > 0 || productLinks > 0 || visualLinks > 0)
        {
            return Conflict(new
            {
                message = "Cannot delete this provider because it has related records. Consider setting its status to INACTIVE or BLOCKED instead.",
                relatedCounts = new
                {
                    purchaseOrders,
                    purchaseReceipts,
                    payables,
                    productLinks,
                    visualLinks,
                },
            });
        }

        // Cascade-clean own directory rows (contacts, addresses, bank accounts).
        var contacts = await db.ProviderContacts.Where(c => c.ProviderId == id).ToListAsync(ct);
        var addresses = await db.ProviderAddresses.Where(a => a.ProviderId == id).ToListAsync(ct);
        var banks = await db.ProviderBankAccounts.Where(b => b.ProviderId == id).ToListAsync(ct);

        db.ProviderContacts.RemoveRange(contacts);
        db.ProviderAddresses.RemoveRange(addresses);
        db.ProviderBankAccounts.RemoveRange(banks);
        db.Providers.Remove(provider);

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═════════════════════════════════════════════════════════════════════
    //                              CONTACTS
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Lists contacts for a provider.</summary>
    [HttpGet("{id:int}/contacts")]
    [ProducesResponseType(typeof(IEnumerable<ProviderContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProviderContactResponse>>> ListContacts(int id, CancellationToken ct)
    {
        if (!await ProviderExistsAsync(id, ct))
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        var items = await db.ProviderContacts.AsNoTracking()
            .Where(c => c.ProviderId == id)
            .OrderByDescending(c => c.IsPrimary).ThenBy(c => c.Name)
            .Select(c => MapContact(c))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Creates a contact for the given provider.</summary>
    [HttpPost("{id:int}/contacts")]
    [ProducesResponseType(typeof(ProviderContactResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderContactResponse>> CreateContact(
        int id,
        [FromBody] CreateProviderContactRequest request,
        CancellationToken ct)
    {
        if (!await ProviderExistsAsync(id, ct))
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        if (request.IsPrimary)
            await ClearPrimaryContactsAsync(id, null, ct);

        var entity = new ProviderContact
        {
            ProviderId = id,
            Name = request.Name.Trim(),
            Role = request.Role?.Trim(),
            Email = request.Email?.Trim(),
            Phone = request.Phone?.Trim(),
            Cellphone = request.Cellphone?.Trim(),
            Whatsapp = request.Whatsapp,
            IsPrimary = request.IsPrimary,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        db.ProviderContacts.Add(entity);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListContacts), new { id }, MapContact(entity));
    }

    /// <summary>Updates a contact for the given provider.</summary>
    [HttpPut("{id:int}/contacts/{contactId:long}")]
    [ProducesResponseType(typeof(ProviderContactResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderContactResponse>> UpdateContact(
        int id,
        long contactId,
        [FromBody] UpdateProviderContactRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProviderContacts
            .FirstOrDefaultAsync(c => c.ProviderContactId == contactId && c.ProviderId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Contact {contactId} was not found for provider {id}." });

        if (request.IsPrimary && !entity.IsPrimary)
            await ClearPrimaryContactsAsync(id, contactId, ct);

        entity.Name = request.Name.Trim();
        entity.Role = request.Role?.Trim();
        entity.Email = request.Email?.Trim();
        entity.Phone = request.Phone?.Trim();
        entity.Cellphone = request.Cellphone?.Trim();
        entity.Whatsapp = request.Whatsapp;
        entity.IsPrimary = request.IsPrimary;
        entity.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Ok(MapContact(entity));
    }

    /// <summary>Deletes a contact for the given provider.</summary>
    [HttpDelete("{id:int}/contacts/{contactId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(int id, long contactId, CancellationToken ct)
    {
        var entity = await db.ProviderContacts
            .FirstOrDefaultAsync(c => c.ProviderContactId == contactId && c.ProviderId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Contact {contactId} was not found for provider {id}." });

        db.ProviderContacts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═════════════════════════════════════════════════════════════════════
    //                              ADDRESSES
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Lists addresses for a provider.</summary>
    [HttpGet("{id:int}/addresses")]
    [ProducesResponseType(typeof(IEnumerable<ProviderAddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProviderAddressResponse>>> ListAddresses(int id, CancellationToken ct)
    {
        if (!await ProviderExistsAsync(id, ct))
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        var items = await db.ProviderAddresses.AsNoTracking()
            .Where(a => a.ProviderId == id)
            .OrderByDescending(a => a.IsPrimary).ThenBy(a => a.AddressType)
            .Select(a => MapAddress(a))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Creates an address for the given provider.</summary>
    [HttpPost("{id:int}/addresses")]
    [ProducesResponseType(typeof(ProviderAddressResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderAddressResponse>> CreateAddress(
        int id,
        [FromBody] CreateProviderAddressRequest request,
        CancellationToken ct)
    {
        if (!await ProviderExistsAsync(id, ct))
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        if (request.IsPrimary)
            await ClearPrimaryAddressesAsync(id, null, ct);

        var entity = new ProviderAddress
        {
            ProviderId = id,
            AddressType = request.AddressType.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City?.Trim(),
            State = request.State?.Trim(),
            PostalCode = request.PostalCode?.Trim(),
            Country = request.Country?.Trim(),
            IsPrimary = request.IsPrimary,
            CreatedAt = DateTime.UtcNow,
        };

        db.ProviderAddresses.Add(entity);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListAddresses), new { id }, MapAddress(entity));
    }

    /// <summary>Updates an address for the given provider.</summary>
    [HttpPut("{id:int}/addresses/{addressId:long}")]
    [ProducesResponseType(typeof(ProviderAddressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderAddressResponse>> UpdateAddress(
        int id,
        long addressId,
        [FromBody] UpdateProviderAddressRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProviderAddresses
            .FirstOrDefaultAsync(a => a.ProviderAddressId == addressId && a.ProviderId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Address {addressId} was not found for provider {id}." });

        if (request.IsPrimary && !entity.IsPrimary)
            await ClearPrimaryAddressesAsync(id, addressId, ct);

        entity.AddressType = request.AddressType.Trim();
        entity.AddressLine = request.AddressLine.Trim();
        entity.City = request.City?.Trim();
        entity.State = request.State?.Trim();
        entity.PostalCode = request.PostalCode?.Trim();
        entity.Country = request.Country?.Trim();
        entity.IsPrimary = request.IsPrimary;

        await db.SaveChangesAsync(ct);
        return Ok(MapAddress(entity));
    }

    /// <summary>Deletes an address for the given provider.</summary>
    [HttpDelete("{id:int}/addresses/{addressId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(int id, long addressId, CancellationToken ct)
    {
        var entity = await db.ProviderAddresses
            .FirstOrDefaultAsync(a => a.ProviderAddressId == addressId && a.ProviderId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Address {addressId} was not found for provider {id}." });

        db.ProviderAddresses.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═════════════════════════════════════════════════════════════════════
    //                            BANK ACCOUNTS
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Lists bank accounts for a provider.</summary>
    [HttpGet("{id:int}/bank-accounts")]
    [ProducesResponseType(typeof(IEnumerable<ProviderBankAccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProviderBankAccountResponse>>> ListBankAccounts(int id, CancellationToken ct)
    {
        if (!await ProviderExistsAsync(id, ct))
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        var items = await db.ProviderBankAccounts.AsNoTracking()
            .Where(b => b.ProviderId == id)
            .OrderByDescending(b => b.IsPrimary).ThenBy(b => b.BankName)
            .Select(b => MapBank(b))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Creates a bank account for the given provider.</summary>
    [HttpPost("{id:int}/bank-accounts")]
    [ProducesResponseType(typeof(ProviderBankAccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderBankAccountResponse>> CreateBankAccount(
        int id,
        [FromBody] CreateProviderBankAccountRequest request,
        CancellationToken ct)
    {
        if (!await ProviderExistsAsync(id, ct))
            return NotFound(new { message = $"Provider with ID {id} was not found." });

        if (request.IsPrimary)
            await ClearPrimaryBankAccountsAsync(id, null, ct);

        var entity = new ProviderBankAccount
        {
            ProviderId = id,
            BankName = request.BankName.Trim(),
            AccountHolder = request.AccountHolder?.Trim(),
            AccountNumber = request.AccountNumber?.Trim(),
            Clabe = request.Clabe?.Trim(),
            CurrencyCode = request.CurrencyCode.Trim().ToUpper(),
            IsPrimary = request.IsPrimary,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
        };

        db.ProviderBankAccounts.Add(entity);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListBankAccounts), new { id }, MapBank(entity));
    }

    /// <summary>Updates a bank account for the given provider.</summary>
    [HttpPut("{id:int}/bank-accounts/{bankAccountId:long}")]
    [ProducesResponseType(typeof(ProviderBankAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderBankAccountResponse>> UpdateBankAccount(
        int id,
        long bankAccountId,
        [FromBody] UpdateProviderBankAccountRequest request,
        CancellationToken ct)
    {
        var entity = await db.ProviderBankAccounts
            .FirstOrDefaultAsync(b => b.ProviderBankAccountId == bankAccountId && b.ProviderId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bank account {bankAccountId} was not found for provider {id}." });

        if (request.IsPrimary && !entity.IsPrimary)
            await ClearPrimaryBankAccountsAsync(id, bankAccountId, ct);

        entity.BankName = request.BankName.Trim();
        entity.AccountHolder = request.AccountHolder?.Trim();
        entity.AccountNumber = request.AccountNumber?.Trim();
        entity.Clabe = request.Clabe?.Trim();
        entity.CurrencyCode = request.CurrencyCode.Trim().ToUpper();
        entity.IsPrimary = request.IsPrimary;
        entity.Notes = request.Notes;

        await db.SaveChangesAsync(ct);
        return Ok(MapBank(entity));
    }

    /// <summary>Deletes a bank account for the given provider.</summary>
    [HttpDelete("{id:int}/bank-accounts/{bankAccountId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBankAccount(int id, long bankAccountId, CancellationToken ct)
    {
        var entity = await db.ProviderBankAccounts
            .FirstOrDefaultAsync(b => b.ProviderBankAccountId == bankAccountId && b.ProviderId == id, ct);

        if (entity is null)
            return NotFound(new { message = $"Bank account {bankAccountId} was not found for provider {id}." });

        db.ProviderBankAccounts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═════════════════════════════════════════════════════════════════════
    //                          STATUS CATALOG
    // ═════════════════════════════════════════════════════════════════════

    /// <summary>Returns the catalog of provider statuses (ACTIVE / INACTIVE / BLOCKED / ...).</summary>
    [HttpGet("statuses")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatuses(CancellationToken ct)
    {
        var items = await db.ProviderStatuses.AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new
            {
                s.ProviderStatusId,
                s.Code,
                s.Name,
                s.Description,
                s.IsActive,
            })
            .ToListAsync(ct);

        return Ok(items);
    }
}
