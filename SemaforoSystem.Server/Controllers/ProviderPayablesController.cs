using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Payables;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

/// <summary>
/// Accounts payable API for providers.
/// Implements section 8 of the supplier guide:
/// payable headers + lines + ledger transactions, ledger-driven balance.
/// </summary>
/// <remarks>
/// The payable balance is derived from <c>provider_payable_transactions</c> per the canonical rule.
/// No business-truth balance column is persisted on <c>provider_payables</c>.
/// </remarks>
[ApiController]
[Route("api/provider-payables")]
[Authorize]
[Produces("application/json")]
public class ProviderPayablesController(ApplicationDbContext db) : ControllerBase
{
    // ── Status codes ────────────────────────────────────────────
    private const string StatusOpen = "OPEN";
    private const string StatusPartiallyPaid = "PARTIALLY_PAID";
    private const string StatusPaid = "PAID";
    private const string StatusOverdue = "OVERDUE";
    private const string StatusCanceled = "CANCELED";
    private const string StatusClosed = "CLOSED";

    // ── Transaction types ───────────────────────────────────────
    private const string TxCharge = "CHARGE";
    private const string TxPayment = "PAYMENT";
    private const string TxDiscount = "DISCOUNT";
    private const string TxInterest = "INTEREST";
    private const string TxAdjustment = "ADJUSTMENT";
    private const string TxCancellation = "CANCELLATION";
    private const string TxCreditNoteApplied = "CREDIT_NOTE_APPLIED";

    private static readonly HashSet<string> AllowedTransactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        TxCharge, TxPayment, TxDiscount, TxInterest,
        TxAdjustment, TxCancellation, TxCreditNoteApplied,
    };

    /// <summary>Sign convention against the payable balance (positive = increases what we owe).</summary>
    private static decimal SignedAmount(string type, decimal amount) => type.ToUpperInvariant() switch
    {
        TxCharge => amount,
        TxInterest => amount,
        TxPayment => -amount,
        TxDiscount => -amount,
        TxCreditNoteApplied => -amount,
        TxCancellation => -amount,
        TxAdjustment => amount, // caller passes signed amount via DTO; treat as positive add
        _ => amount,
    };

    /// <summary>Counts toward "paid" totals (positive value).</summary>
    private static bool IsPaymentLike(string type) => type.ToUpperInvariant() is
        TxPayment or TxDiscount or TxCreditNoteApplied;

    /// <summary>Normalizes incoming DateTime to UTC for PostgreSQL timestamptz columns.</summary>
    private static DateTime? ToUtc(DateTime? value) => value is null ? null : ToUtc(value.Value);

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    // ═══════════════════════════════════════════════════════════════════
    //  CATALOGS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns the provider payable types catalog.</summary>
    [HttpGet("catalogs/types")]
    [ProducesResponseType(typeof(IEnumerable<PayableCatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTypes(CancellationToken ct) =>
        Ok(await db.ProviderPayableTypes.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new PayableCatalogItemResponse
            {
                Id = t.ProviderPayableTypeId,
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive,
            }).ToListAsync(ct));

    /// <summary>Returns the provider payable status catalog.</summary>
    [HttpGet("catalogs/statuses")]
    [ProducesResponseType(typeof(IEnumerable<PayableCatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatuses(CancellationToken ct) =>
        Ok(await db.ProviderPayableStatuses.AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new PayableCatalogItemResponse
            {
                Id = s.ProviderPayableStatusId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
            }).ToListAsync(ct));

    /// <summary>Returns the provider payment methods catalog.</summary>
    [HttpGet("catalogs/payment-methods")]
    [ProducesResponseType(typeof(IEnumerable<PayableCatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods(CancellationToken ct) =>
        Ok(await db.ProviderPaymentMethods.AsNoTracking().OrderBy(m => m.Name)
            .Select(m => new PayableCatalogItemResponse
            {
                Id = m.ProviderPaymentMethodId,
                Code = m.Code,
                Name = m.Name,
                Description = m.Description,
                IsActive = m.IsActive,
            }).ToListAsync(ct));

    /// <summary>Returns the allowed ledger transaction types.</summary>
    [HttpGet("catalogs/transaction-types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetTransactionTypes() => Ok(AllowedTransactionTypes.OrderBy(t => t));

    // ═══════════════════════════════════════════════════════════════════
    //  LIST
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Lists payables with filters, sorting, pagination and ledger-derived balances.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProviderPayableListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProviderPayableListItemResponse>>> GetAll(
        [FromQuery] ProviderPayableQueryParameters query,
        CancellationToken ct)
    {
        var q = db.ProviderPayables
            .Include(p => p.Provider)
            .Include(p => p.Site)
            .Include(p => p.ProviderPayableType)
            .Include(p => p.ProviderPayableStatus)
            .AsNoTracking()
            .AsQueryable();

        if (query.ProviderId.HasValue)
            q = q.Where(p => p.ProviderId == query.ProviderId.Value);
        if (query.SiteId.HasValue)
            q = q.Where(p => p.SiteId == query.SiteId.Value);
        if (query.ProviderPayableTypeId.HasValue)
            q = q.Where(p => p.ProviderPayableTypeId == query.ProviderPayableTypeId.Value);
        if (query.ProviderPayableStatusId.HasValue)
            q = q.Where(p => p.ProviderPayableStatusId == query.ProviderPayableStatusId.Value);
        if (!string.IsNullOrWhiteSpace(query.TypeCode))
        {
            var tc = query.TypeCode.ToLower();
            q = q.Where(p => p.ProviderPayableType.Code.ToLower() == tc);
        }
        if (!string.IsNullOrWhiteSpace(query.StatusCode))
        {
            var sc = query.StatusCode.ToLower();
            q = q.Where(p => p.ProviderPayableStatus.Code.ToLower() == sc);
        }
        if (!string.IsNullOrWhiteSpace(query.CurrencyCode))
        {
            var cc = query.CurrencyCode.ToUpper();
            q = q.Where(p => p.CurrencyCode.ToUpper() == cc);
        }
        if (!string.IsNullOrWhiteSpace(query.DocumentNumber))
        {
            var dn = query.DocumentNumber.ToLower();
            q = q.Where(p => p.DocumentNumber != null && p.DocumentNumber.ToLower().Contains(dn));
        }
        if (query.DocumentDateFrom.HasValue)
            q = q.Where(p => p.DocumentDate >= query.DocumentDateFrom.Value);
        if (query.DocumentDateTo.HasValue)
            q = q.Where(p => p.DocumentDate <= query.DocumentDateTo.Value);
        if (query.DueDateFrom.HasValue)
            q = q.Where(p => p.DueDate != null && p.DueDate >= query.DueDateFrom.Value);
        if (query.DueDateTo.HasValue)
            q = q.Where(p => p.DueDate != null && p.DueDate <= query.DueDateTo.Value);
        if (query.PurchaseOrderId.HasValue)
            q = q.Where(p => p.PurchaseOrderId == query.PurchaseOrderId.Value);
        if (query.PurchaseReceiptId.HasValue)
            q = q.Where(p => p.PurchaseReceiptId == query.PurchaseReceiptId.Value);

        if (query.OnlyOpen == true)
        {
            q = q.Where(p =>
                p.ProviderPayableStatus.Code != StatusPaid &&
                p.ProviderPayableStatus.Code != StatusCanceled &&
                p.ProviderPayableStatus.Code != StatusClosed);
        }

        var today = DateTime.UtcNow.Date;
        if (query.OnlyOverdue == true)
        {
            q = q.Where(p =>
                p.DueDate != null &&
                p.DueDate < today &&
                p.ProviderPayableStatus.Code != StatusPaid &&
                p.ProviderPayableStatus.Code != StatusCanceled &&
                p.ProviderPayableStatus.Code != StatusClosed);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.ToLower();
            q = q.Where(p =>
                (p.DocumentNumber != null && p.DocumentNumber.ToLower().Contains(term)) ||
                (p.Reference != null && p.Reference.ToLower().Contains(term)) ||
                p.Provider.LegalName.ToLower().Contains(term) ||
                (p.Provider.TradeName != null && p.Provider.TradeName.ToLower().Contains(term)));
        }

        q = query.SortBy?.ToLower() switch
        {
            "documentdate" => query.SortDescending ? q.OrderByDescending(p => p.DocumentDate) : q.OrderBy(p => p.DocumentDate),
            "duedate" => query.SortDescending ? q.OrderByDescending(p => p.DueDate) : q.OrderBy(p => p.DueDate),
            "documentnumber" => query.SortDescending ? q.OrderByDescending(p => p.DocumentNumber) : q.OrderBy(p => p.DocumentNumber),
            "provider" => query.SortDescending ? q.OrderByDescending(p => p.Provider.LegalName) : q.OrderBy(p => p.Provider.LegalName),
            "total" => query.SortDescending ? q.OrderByDescending(p => p.Total) : q.OrderBy(p => p.Total),
            "status" => query.SortDescending ? q.OrderByDescending(p => p.ProviderPayableStatus.Name) : q.OrderBy(p => p.ProviderPayableStatus.Name),
            "createdat" => query.SortDescending ? q.OrderByDescending(p => p.CreatedAt) : q.OrderBy(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.DocumentDate),
        };

        var totalCount = await q.CountAsync(ct);

        var pageRows = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new
            {
                Payable = p,
                Charged = db.ProviderPayableTransactions
                    .Where(t => t.ProviderPayableId == p.ProviderPayableId &&
                                t.TransactionType.ToUpper() == TxCharge)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                Paid = db.ProviderPayableTransactions
                    .Where(t => t.ProviderPayableId == p.ProviderPayableId &&
                                (t.TransactionType.ToUpper() == TxPayment ||
                                 t.TransactionType.ToUpper() == TxDiscount ||
                                 t.TransactionType.ToUpper() == TxCreditNoteApplied))
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                Interest = db.ProviderPayableTransactions
                    .Where(t => t.ProviderPayableId == p.ProviderPayableId &&
                                t.TransactionType.ToUpper() == TxInterest)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                CancelOrAdjust = db.ProviderPayableTransactions
                    .Where(t => t.ProviderPayableId == p.ProviderPayableId &&
                                (t.TransactionType.ToUpper() == TxCancellation ||
                                 t.TransactionType.ToUpper() == TxAdjustment))
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .ToListAsync(ct);

        var items = pageRows.Select(x =>
        {
            var charged = x.Charged > 0 ? x.Charged : x.Payable.Total;
            var balance = charged + x.Interest - x.Paid - x.CancelOrAdjust;
            var isOverdue = x.Payable.DueDate.HasValue &&
                            x.Payable.DueDate.Value.Date < today &&
                            x.Payable.ProviderPayableStatus.Code != StatusPaid &&
                            x.Payable.ProviderPayableStatus.Code != StatusCanceled &&
                            x.Payable.ProviderPayableStatus.Code != StatusClosed &&
                            balance > 0m;

            return new ProviderPayableListItemResponse
            {
                ProviderPayableId = x.Payable.ProviderPayableId,
                ProviderId = x.Payable.ProviderId,
                ProviderLegalName = x.Payable.Provider.LegalName,
                ProviderTradeName = x.Payable.Provider.TradeName,
                SiteId = x.Payable.SiteId,
                SiteName = x.Payable.Site.Name,
                ProviderPayableTypeId = x.Payable.ProviderPayableTypeId,
                TypeCode = x.Payable.ProviderPayableType.Code,
                TypeName = x.Payable.ProviderPayableType.Name,
                ProviderPayableStatusId = x.Payable.ProviderPayableStatusId,
                StatusCode = x.Payable.ProviderPayableStatus.Code,
                StatusName = x.Payable.ProviderPayableStatus.Name,
                DocumentNumber = x.Payable.DocumentNumber,
                Reference = x.Payable.Reference,
                DocumentDate = x.Payable.DocumentDate,
                DueDate = x.Payable.DueDate,
                IsOverdue = isOverdue,
                CurrencyCode = x.Payable.CurrencyCode,
                Subtotal = x.Payable.Subtotal,
                DiscountTotal = x.Payable.DiscountTotal,
                TaxTotal = x.Payable.TaxTotal,
                Total = x.Payable.Total,
                ChargedAmount = charged,
                PaidAmount = x.Paid,
                Balance = balance,
                PurchaseOrderId = x.Payable.PurchaseOrderId,
                PurchaseReceiptId = x.Payable.PurchaseReceiptId,
                CreatedAt = x.Payable.CreatedAt,
                UpdatedAt = x.Payable.UpdatedAt,
            };
        }).ToList();

        return Ok(new PagedResponse<ProviderPayableListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GET DETAIL
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a single payable with header, lines and ledger transactions.</summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ProviderPayableDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProviderPayableDetailResponse>> GetById(long id, CancellationToken ct)
    {
        var detail = await BuildDetailAsync(id, ct);
        if (detail is null)
            return NotFound(new { message = $"Payable with ID {id} was not found." });
        return Ok(detail);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CREATE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Creates a new payable header (with optional lines).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProviderPayableDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProviderPayableDetailResponse>> Create(
        [FromBody] CreateProviderPayableRequest request,
        CancellationToken ct)
    {
        if (!await db.Providers.AnyAsync(p => p.ProviderId == request.ProviderId, ct))
            return NotFound(new { message = $"Provider {request.ProviderId} was not found." });

        if (!await db.ProviderPayableTypes.AnyAsync(t => t.ProviderPayableTypeId == request.ProviderPayableTypeId, ct))
            return UnprocessableEntity(new { message = $"Payable type {request.ProviderPayableTypeId} does not exist." });

        if (!await db.Sites.AnyAsync(s => s.SiteId == request.SiteId, ct))
            return NotFound(new { message = $"Site {request.SiteId} was not found." });

        if (!await db.Employees.AnyAsync(e => e.EmployeeId == request.OpenedByEmployeeId, ct))
            return NotFound(new { message = $"Employee {request.OpenedByEmployeeId} was not found." });

        int statusId;
        if (request.ProviderPayableStatusId.HasValue)
        {
            if (!await db.ProviderPayableStatuses.AnyAsync(s => s.ProviderPayableStatusId == request.ProviderPayableStatusId.Value, ct))
                return UnprocessableEntity(new { message = $"Payable status {request.ProviderPayableStatusId} does not exist." });
            statusId = request.ProviderPayableStatusId.Value;
        }
        else
        {
            var openStatus = await db.ProviderPayableStatuses
                .FirstOrDefaultAsync(s => s.Code.ToUpper() == StatusOpen, ct);
            if (openStatus is null)
                return UnprocessableEntity(new { message = "Default OPEN status not found in catalog." });
            statusId = openStatus.ProviderPayableStatusId;
        }

        if (request.PurchaseOrderId.HasValue &&
            !await db.PurchaseOrders.AnyAsync(po => po.PurchaseOrderId == request.PurchaseOrderId.Value, ct))
            return NotFound(new { message = $"Purchase order {request.PurchaseOrderId} was not found." });

        if (request.PurchaseReceiptId.HasValue &&
            !await db.PurchaseReceipts.AnyAsync(pr => pr.PurchaseReceiptId == request.PurchaseReceiptId.Value, ct))
            return NotFound(new { message = $"Purchase receipt {request.PurchaseReceiptId} was not found." });

        var lineNumbers = request.Lines.Select(l => l.LineNumber).ToList();
        if (lineNumbers.Count != lineNumbers.Distinct().Count())
            return BadRequest(new { message = "Line numbers must be unique within the payable." });

        var now = DateTime.UtcNow;

        // ── Compute totals from lines if not supplied ──────────
        var computedSubtotal = request.Lines.Sum(l => l.Quantity * l.UnitCost);
        var computedDiscount = request.Lines.Sum(l => l.DiscountAmount);
        var computedTax = request.Lines.Sum(l => l.TaxAmount);
        var computedTotal = request.Lines.Sum(l =>
            l.LineTotal ?? (l.Quantity * l.UnitCost - l.DiscountAmount + l.TaxAmount));

        var payable = new ProviderPayable
        {
            ProviderId = request.ProviderId,
            ProviderPayableTypeId = request.ProviderPayableTypeId,
            ProviderPayableStatusId = statusId,
            SiteId = request.SiteId,
            OpenedByEmployeeId = request.OpenedByEmployeeId,
            PurchaseOrderId = request.PurchaseOrderId,
            PurchaseReceiptId = request.PurchaseReceiptId,
            DocumentNumber = request.DocumentNumber?.Trim(),
            Reference = request.Reference?.Trim(),
            DocumentDate = ToUtc(request.DocumentDate) ?? now,
            DueDate = ToUtc(request.DueDate),
            CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode)
                ? "MXN" : request.CurrencyCode.Trim().ToUpper(),
            Subtotal = request.Subtotal ?? computedSubtotal,
            DiscountTotal = request.DiscountTotal ?? computedDiscount,
            TaxTotal = request.TaxTotal ?? computedTax,
            Total = request.Total ?? computedTotal,
            Notes = request.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var l in request.Lines)
        {
            payable.ProviderPayableLines.Add(new ProviderPayableLine
            {
                LineNumber = l.LineNumber,
                PurchaseOrderLineId = l.PurchaseOrderLineId,
                PurchaseReceiptLineId = l.PurchaseReceiptLineId,
                ProductId = l.ProductId,
                ProductVisualDefinitionId = l.ProductVisualDefinitionId,
                InventoryItemDefinitionId = l.InventoryItemDefinitionId,
                DescriptionSnapshot = l.DescriptionSnapshot.Trim(),
                Quantity = l.Quantity,
                UnitCost = l.UnitCost,
                DiscountAmount = l.DiscountAmount,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal ?? (l.Quantity * l.UnitCost - l.DiscountAmount + l.TaxAmount),
                Notes = l.Notes,
            });
        }

        // ── Seed ledger with the initial CHARGE so balance = total ──
        if (payable.Total > 0m)
        {
            payable.ProviderPayableTransactions.Add(new ProviderPayableTransaction
            {
                TransactionType = TxCharge,
                TransactionDate = payable.DocumentDate,
                Amount = payable.Total,
                Reference = payable.DocumentNumber,
                Comments = "Initial charge",
                CreatedByEmployeeId = request.OpenedByEmployeeId,
                CreatedAt = now,
            });
        }

        db.ProviderPayables.Add(payable);
        await db.SaveChangesAsync(ct);

        var detail = await BuildDetailAsync(payable.ProviderPayableId, ct);
        return CreatedAtAction(nameof(GetById),
            new { id = payable.ProviderPayableId }, detail);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UPDATE HEADER
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Updates the payable header. Cannot modify provider, site, opener or origin order/receipt.</summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ProviderPayableDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProviderPayableDetailResponse>> Update(
        long id,
        [FromBody] UpdateProviderPayableRequest request,
        CancellationToken ct)
    {
        var payable = await db.ProviderPayables
            .Include(p => p.ProviderPayableStatus)
            .FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);

        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        if (payable.ProviderPayableStatus.Code is StatusPaid or StatusCanceled or StatusClosed)
            return Conflict(new { message = $"Payable {id} cannot be modified in status {payable.ProviderPayableStatus.Code}." });

        if (!await db.ProviderPayableTypes.AnyAsync(t => t.ProviderPayableTypeId == request.ProviderPayableTypeId, ct))
            return UnprocessableEntity(new { message = $"Payable type {request.ProviderPayableTypeId} does not exist." });

        if (!await db.ProviderPayableStatuses.AnyAsync(s => s.ProviderPayableStatusId == request.ProviderPayableStatusId, ct))
            return UnprocessableEntity(new { message = $"Payable status {request.ProviderPayableStatusId} does not exist." });

        if (request.PurchaseOrderId.HasValue &&
            !await db.PurchaseOrders.AnyAsync(po => po.PurchaseOrderId == request.PurchaseOrderId.Value, ct))
            return NotFound(new { message = $"Purchase order {request.PurchaseOrderId} was not found." });

        if (request.PurchaseReceiptId.HasValue &&
            !await db.PurchaseReceipts.AnyAsync(pr => pr.PurchaseReceiptId == request.PurchaseReceiptId.Value, ct))
            return NotFound(new { message = $"Purchase receipt {request.PurchaseReceiptId} was not found." });

        payable.ProviderPayableTypeId = request.ProviderPayableTypeId;
        payable.ProviderPayableStatusId = request.ProviderPayableStatusId;
        payable.PurchaseOrderId = request.PurchaseOrderId;
        payable.PurchaseReceiptId = request.PurchaseReceiptId;
        payable.DocumentNumber = request.DocumentNumber?.Trim();
        payable.Reference = request.Reference?.Trim();
        payable.DocumentDate = ToUtc(request.DocumentDate);
        payable.DueDate = ToUtc(request.DueDate);
        payable.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode)
            ? payable.CurrencyCode : request.CurrencyCode.Trim().ToUpper();
        payable.Subtotal = request.Subtotal;
        payable.DiscountTotal = request.DiscountTotal;
        payable.TaxTotal = request.TaxTotal;
        payable.Total = request.Total;
        payable.Notes = request.Notes;
        payable.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(await BuildDetailAsync(id, ct));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PATCH STATUS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Changes the payable status (e.g. close, cancel, reopen).</summary>
    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateStatus(
        long id,
        [FromBody] UpdateProviderPayableStatusRequest request,
        CancellationToken ct)
    {
        var payable = await db.ProviderPayables.FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        if (!await db.ProviderPayableStatuses.AnyAsync(s => s.ProviderPayableStatusId == request.ProviderPayableStatusId, ct))
            return UnprocessableEntity(new { message = $"Payable status {request.ProviderPayableStatusId} does not exist." });

        payable.ProviderPayableStatusId = request.ProviderPayableStatusId;
        payable.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.Comments))
        {
            payable.Notes = string.IsNullOrWhiteSpace(payable.Notes)
                ? request.Comments
                : $"{payable.Notes}\n[status] {request.Comments}";
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DELETE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Deletes a payable. Blocked when ledger transactions exist.</summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        var payable = await db.ProviderPayables.FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        // Block delete when there is a non-trivial ledger (anything beyond the initial CHARGE).
        var nonChargeTxs = await db.ProviderPayableTransactions
            .CountAsync(t => t.ProviderPayableId == id &&
                             t.TransactionType.ToUpper() != TxCharge, ct);

        if (nonChargeTxs > 0)
            return Conflict(new
            {
                message = "Cannot delete this payable because it has ledger transactions (payments, discounts, adjustments…). Use status CANCELED instead.",
            });

        // Blocking link from purchase_order_expenses.
        var linkedExpenses = await db.PurchaseOrderExpenses
            .CountAsync(e => e.ProviderPayableId == id, ct);
        if (linkedExpenses > 0)
            return Conflict(new
            {
                message = "Cannot delete this payable because it has linked purchase order expenses.",
            });

        var lines = await db.ProviderPayableLines.Where(l => l.ProviderPayableId == id).ToListAsync(ct);
        var charges = await db.ProviderPayableTransactions.Where(t => t.ProviderPayableId == id).ToListAsync(ct);

        db.ProviderPayableLines.RemoveRange(lines);
        db.ProviderPayableTransactions.RemoveRange(charges);
        db.ProviderPayables.Remove(payable);

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LINES
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Lists lines of a payable.</summary>
    [HttpGet("{id:long}/lines")]
    [ProducesResponseType(typeof(IEnumerable<ProviderPayableLineResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProviderPayableLineResponse>>> ListLines(long id, CancellationToken ct)
    {
        if (!await db.ProviderPayables.AnyAsync(p => p.ProviderPayableId == id, ct))
            return NotFound(new { message = $"Payable {id} was not found." });

        var items = await db.ProviderPayableLines.AsNoTracking()
            .Where(l => l.ProviderPayableId == id)
            .OrderBy(l => l.LineNumber)
            .Select(l => MapLine(l))
            .ToListAsync(ct);

        return Ok(items);
    }

    /// <summary>Creates a payable line.</summary>
    [HttpPost("{id:long}/lines")]
    [ProducesResponseType(typeof(ProviderPayableLineResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderPayableLineResponse>> CreateLine(
        long id,
        [FromBody] CreateProviderPayableLineRequest request,
        CancellationToken ct)
    {
        var payable = await db.ProviderPayables
            .Include(p => p.ProviderPayableStatus)
            .FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        if (payable.ProviderPayableStatus.Code is StatusPaid or StatusCanceled or StatusClosed)
            return Conflict(new { message = $"Cannot add lines to a payable in status {payable.ProviderPayableStatus.Code}." });

        var numberExists = await db.ProviderPayableLines
            .AnyAsync(l => l.ProviderPayableId == id && l.LineNumber == request.LineNumber, ct);
        if (numberExists)
            return Conflict(new { message = $"Line number {request.LineNumber} already exists for payable {id}." });

        var entity = new ProviderPayableLine
        {
            ProviderPayableId = id,
            LineNumber = request.LineNumber,
            PurchaseOrderLineId = request.PurchaseOrderLineId,
            PurchaseReceiptLineId = request.PurchaseReceiptLineId,
            ProductId = request.ProductId,
            ProductVisualDefinitionId = request.ProductVisualDefinitionId,
            InventoryItemDefinitionId = request.InventoryItemDefinitionId,
            DescriptionSnapshot = request.DescriptionSnapshot.Trim(),
            Quantity = request.Quantity,
            UnitCost = request.UnitCost,
            DiscountAmount = request.DiscountAmount,
            TaxAmount = request.TaxAmount,
            LineTotal = request.LineTotal ?? (request.Quantity * request.UnitCost - request.DiscountAmount + request.TaxAmount),
            Notes = request.Notes,
        };

        db.ProviderPayableLines.Add(entity);
        payable.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(ListLines), new { id }, MapLine(entity));
    }

    /// <summary>Updates a payable line.</summary>
    [HttpPut("{id:long}/lines/{lineId:long}")]
    [ProducesResponseType(typeof(ProviderPayableLineResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProviderPayableLineResponse>> UpdateLine(
        long id,
        long lineId,
        [FromBody] UpdateProviderPayableLineRequest request,
        CancellationToken ct)
    {
        var payable = await db.ProviderPayables
            .Include(p => p.ProviderPayableStatus)
            .FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        if (payable.ProviderPayableStatus.Code is StatusPaid or StatusCanceled or StatusClosed)
            return Conflict(new { message = $"Cannot modify lines of a payable in status {payable.ProviderPayableStatus.Code}." });

        var entity = await db.ProviderPayableLines
            .FirstOrDefaultAsync(l => l.ProviderPayableLineId == lineId && l.ProviderPayableId == id, ct);
        if (entity is null)
            return NotFound(new { message = $"Line {lineId} was not found for payable {id}." });

        if (entity.LineNumber != request.LineNumber)
        {
            var numberExists = await db.ProviderPayableLines
                .AnyAsync(l => l.ProviderPayableId == id &&
                               l.LineNumber == request.LineNumber &&
                               l.ProviderPayableLineId != lineId, ct);
            if (numberExists)
                return Conflict(new { message = $"Line number {request.LineNumber} already exists for payable {id}." });
        }

        entity.LineNumber = request.LineNumber;
        entity.PurchaseOrderLineId = request.PurchaseOrderLineId;
        entity.PurchaseReceiptLineId = request.PurchaseReceiptLineId;
        entity.ProductId = request.ProductId;
        entity.ProductVisualDefinitionId = request.ProductVisualDefinitionId;
        entity.InventoryItemDefinitionId = request.InventoryItemDefinitionId;
        entity.DescriptionSnapshot = request.DescriptionSnapshot.Trim();
        entity.Quantity = request.Quantity;
        entity.UnitCost = request.UnitCost;
        entity.DiscountAmount = request.DiscountAmount;
        entity.TaxAmount = request.TaxAmount;
        entity.LineTotal = request.LineTotal ?? (request.Quantity * request.UnitCost - request.DiscountAmount + request.TaxAmount);
        entity.Notes = request.Notes;

        payable.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(MapLine(entity));
    }

    /// <summary>Deletes a payable line.</summary>
    [HttpDelete("{id:long}/lines/{lineId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteLine(long id, long lineId, CancellationToken ct)
    {
        var payable = await db.ProviderPayables
            .Include(p => p.ProviderPayableStatus)
            .FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        if (payable.ProviderPayableStatus.Code is StatusPaid or StatusCanceled or StatusClosed)
            return Conflict(new { message = $"Cannot delete lines of a payable in status {payable.ProviderPayableStatus.Code}." });

        var entity = await db.ProviderPayableLines
            .FirstOrDefaultAsync(l => l.ProviderPayableLineId == lineId && l.ProviderPayableId == id, ct);
        if (entity is null)
            return NotFound(new { message = $"Line {lineId} was not found for payable {id}." });

        db.ProviderPayableLines.Remove(entity);
        payable.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRANSACTIONS (LEDGER)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Lists ledger transactions of a payable, ordered by transaction date.</summary>
    [HttpGet("{id:long}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<ProviderPayableTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<ProviderPayableTransactionResponse>>> ListTransactions(
        long id,
        CancellationToken ct)
    {
        if (!await db.ProviderPayables.AnyAsync(p => p.ProviderPayableId == id, ct))
            return NotFound(new { message = $"Payable {id} was not found." });

        var items = await db.ProviderPayableTransactions
            .Include(t => t.ProviderPaymentMethod)
            .Include(t => t.CreatedByEmployee)
            .AsNoTracking()
            .Where(t => t.ProviderPayableId == id)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.ProviderPayableTransactionId)
            .ToListAsync(ct);

        return Ok(items.Select(MapTransaction).ToList());
    }

    /// <summary>Records a new ledger transaction (payment, discount, interest, etc.).</summary>
    [HttpPost("{id:long}/transactions")]
    [ProducesResponseType(typeof(ProviderPayableTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ProviderPayableTransactionResponse>> CreateTransaction(
        long id,
        [FromBody] CreateProviderPayableTransactionRequest request,
        CancellationToken ct)
    {
        var payable = await db.ProviderPayables
            .Include(p => p.ProviderPayableStatus)
            .FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        if (payable.ProviderPayableStatus.Code is StatusCanceled)
            return Conflict(new { message = "Cannot post transactions to a CANCELED payable." });

        var type = request.TransactionType?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!AllowedTransactionTypes.Contains(type))
            return BadRequest(new
            {
                message = $"Invalid transaction type '{request.TransactionType}'. Allowed: {string.Join(", ", AllowedTransactionTypes)}.",
            });

        if (request.ProviderPaymentMethodId.HasValue &&
            !await db.ProviderPaymentMethods.AnyAsync(m => m.ProviderPaymentMethodId == request.ProviderPaymentMethodId.Value, ct))
            return UnprocessableEntity(new { message = $"Payment method {request.ProviderPaymentMethodId} does not exist." });

        if (!await db.Employees.AnyAsync(e => e.EmployeeId == request.CreatedByEmployeeId, ct))
            return NotFound(new { message = $"Employee {request.CreatedByEmployeeId} was not found." });

        if (request.Amount <= 0m)
            return BadRequest(new { message = "Amount must be greater than zero." });

        var now = DateTime.UtcNow;

        var entity = new ProviderPayableTransaction
        {
            ProviderPayableId = id,
            TransactionType = type,
            ProviderPaymentMethodId = request.ProviderPaymentMethodId,
            TransactionDate = ToUtc(request.TransactionDate) ?? now,
            Amount = request.Amount,
            Reference = request.Reference?.Trim(),
            Comments = request.Comments,
            CreatedByEmployeeId = request.CreatedByEmployeeId,
            CreatedAt = now,
        };

        db.ProviderPayableTransactions.Add(entity);
        payable.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        // ── Auto-transition status based on the resulting balance ──
        await RefreshStatusFromLedgerAsync(payable, ct);

        var saved = await db.ProviderPayableTransactions
            .Include(t => t.ProviderPaymentMethod)
            .Include(t => t.CreatedByEmployee)
            .AsNoTracking()
            .FirstAsync(t => t.ProviderPayableTransactionId == entity.ProviderPayableTransactionId, ct);

        return CreatedAtAction(nameof(ListTransactions), new { id }, MapTransaction(saved));
    }

    /// <summary>Reverses (deletes) a ledger transaction. Use with care, audit-only environments may forbid this.</summary>
    [HttpDelete("{id:long}/transactions/{transactionId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(long id, long transactionId, CancellationToken ct)
    {
        var payable = await db.ProviderPayables.FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);
        if (payable is null)
            return NotFound(new { message = $"Payable {id} was not found." });

        var entity = await db.ProviderPayableTransactions
            .FirstOrDefaultAsync(t => t.ProviderPayableTransactionId == transactionId && t.ProviderPayableId == id, ct);
        if (entity is null)
            return NotFound(new { message = $"Transaction {transactionId} was not found for payable {id}." });

        db.ProviderPayableTransactions.Remove(entity);
        payable.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await RefreshStatusFromLedgerAsync(payable, ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private async Task<ProviderPayableDetailResponse?> BuildDetailAsync(long id, CancellationToken ct)
    {
        var payable = await db.ProviderPayables
            .Include(p => p.Provider)
            .Include(p => p.Site)
            .Include(p => p.ProviderPayableType)
            .Include(p => p.ProviderPayableStatus)
            .Include(p => p.OpenedByEmployee)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderPayableId == id, ct);

        if (payable is null) return null;

        var lines = await db.ProviderPayableLines.AsNoTracking()
            .Where(l => l.ProviderPayableId == id)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(ct);

        var txs = await db.ProviderPayableTransactions
            .Include(t => t.ProviderPaymentMethod)
            .Include(t => t.CreatedByEmployee)
            .AsNoTracking()
            .Where(t => t.ProviderPayableId == id)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.ProviderPayableTransactionId)
            .ToListAsync(ct);

        var charged = txs.Where(t => string.Equals(t.TransactionType, TxCharge, StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
        var paid = txs.Where(t => IsPaymentLike(t.TransactionType)).Sum(t => t.Amount);
        var interest = txs.Where(t => string.Equals(t.TransactionType, TxInterest, StringComparison.OrdinalIgnoreCase)).Sum(t => t.Amount);
        var cancelOrAdjust = txs
            .Where(t => string.Equals(t.TransactionType, TxCancellation, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(t.TransactionType, TxAdjustment, StringComparison.OrdinalIgnoreCase))
            .Sum(t => t.Amount);

        if (charged <= 0m) charged = payable.Total;
        var balance = charged + interest - paid - cancelOrAdjust;
        var today = DateTime.UtcNow.Date;
        var isOverdue = payable.DueDate.HasValue &&
                        payable.DueDate.Value.Date < today &&
                        payable.ProviderPayableStatus.Code != StatusPaid &&
                        payable.ProviderPayableStatus.Code != StatusCanceled &&
                        payable.ProviderPayableStatus.Code != StatusClosed &&
                        balance > 0m;

        var employeeName = FormatEmployeeName(payable.OpenedByEmployee);

        return new ProviderPayableDetailResponse
        {
            ProviderPayableId = payable.ProviderPayableId,
            ProviderId = payable.ProviderId,
            ProviderLegalName = payable.Provider.LegalName,
            ProviderTradeName = payable.Provider.TradeName,
            SiteId = payable.SiteId,
            SiteName = payable.Site.Name,
            ProviderPayableTypeId = payable.ProviderPayableTypeId,
            TypeCode = payable.ProviderPayableType.Code,
            TypeName = payable.ProviderPayableType.Name,
            ProviderPayableStatusId = payable.ProviderPayableStatusId,
            StatusCode = payable.ProviderPayableStatus.Code,
            StatusName = payable.ProviderPayableStatus.Name,
            DocumentNumber = payable.DocumentNumber,
            Reference = payable.Reference,
            DocumentDate = payable.DocumentDate,
            DueDate = payable.DueDate,
            IsOverdue = isOverdue,
            CurrencyCode = payable.CurrencyCode,
            Subtotal = payable.Subtotal,
            DiscountTotal = payable.DiscountTotal,
            TaxTotal = payable.TaxTotal,
            Total = payable.Total,
            ChargedAmount = charged,
            PaidAmount = paid,
            Balance = balance,
            PurchaseOrderId = payable.PurchaseOrderId,
            PurchaseReceiptId = payable.PurchaseReceiptId,
            CreatedAt = payable.CreatedAt,
            UpdatedAt = payable.UpdatedAt,
            OpenedByEmployeeId = payable.OpenedByEmployeeId,
            OpenedByEmployeeName = employeeName,
            Notes = payable.Notes,
            Lines = lines.Select(MapLine).ToList(),
            Transactions = txs.Select(MapTransaction).ToList(),
        };
    }

    private async Task RefreshStatusFromLedgerAsync(ProviderPayable payable, CancellationToken ct)
    {
        // Don't auto-touch terminal/manual states.
        var current = await db.ProviderPayableStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProviderPayableStatusId == payable.ProviderPayableStatusId, ct);
        if (current is null) return;
        if (current.Code is StatusCanceled or StatusClosed) return;

        var charged = await db.ProviderPayableTransactions
            .Where(t => t.ProviderPayableId == payable.ProviderPayableId &&
                        t.TransactionType.ToUpper() == TxCharge)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
        if (charged <= 0m) charged = payable.Total;

        var paid = await db.ProviderPayableTransactions
            .Where(t => t.ProviderPayableId == payable.ProviderPayableId &&
                        (t.TransactionType.ToUpper() == TxPayment ||
                         t.TransactionType.ToUpper() == TxDiscount ||
                         t.TransactionType.ToUpper() == TxCreditNoteApplied))
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var interest = await db.ProviderPayableTransactions
            .Where(t => t.ProviderPayableId == payable.ProviderPayableId &&
                        t.TransactionType.ToUpper() == TxInterest)
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var cancelOrAdjust = await db.ProviderPayableTransactions
            .Where(t => t.ProviderPayableId == payable.ProviderPayableId &&
                        (t.TransactionType.ToUpper() == TxCancellation ||
                         t.TransactionType.ToUpper() == TxAdjustment))
            .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;

        var balance = charged + interest - paid - cancelOrAdjust;

        string targetCode;
        if (balance <= 0m && paid > 0m) targetCode = StatusPaid;
        else if (paid > 0m && balance > 0m) targetCode = StatusPartiallyPaid;
        else if (payable.DueDate.HasValue && payable.DueDate.Value.Date < DateTime.UtcNow.Date && balance > 0m)
            targetCode = StatusOverdue;
        else targetCode = StatusOpen;

        if (string.Equals(current.Code, targetCode, StringComparison.OrdinalIgnoreCase))
            return;

        var target = await db.ProviderPayableStatuses
            .FirstOrDefaultAsync(s => s.Code.ToUpper() == targetCode, ct);
        if (target is null) return;

        var tracked = await db.ProviderPayables.FirstAsync(p => p.ProviderPayableId == payable.ProviderPayableId, ct);
        tracked.ProviderPayableStatusId = target.ProviderPayableStatusId;
        tracked.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static string? FormatEmployeeName(Employee? e)
    {
        if (e is null) return null;
        var parts = new[] { e.Name, e.FirstLastName, e.SecondLastName }
            .Where(s => !string.IsNullOrWhiteSpace(s));
        var name = string.Join(' ', parts).Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private static ProviderPayableLineResponse MapLine(ProviderPayableLine l) => new()
    {
        ProviderPayableLineId = l.ProviderPayableLineId,
        ProviderPayableId = l.ProviderPayableId,
        LineNumber = l.LineNumber,
        PurchaseOrderLineId = l.PurchaseOrderLineId,
        PurchaseReceiptLineId = l.PurchaseReceiptLineId,
        ProductId = l.ProductId,
        ProductVisualDefinitionId = l.ProductVisualDefinitionId,
        InventoryItemDefinitionId = l.InventoryItemDefinitionId,
        DescriptionSnapshot = l.DescriptionSnapshot,
        Quantity = l.Quantity,
        UnitCost = l.UnitCost,
        DiscountAmount = l.DiscountAmount,
        TaxAmount = l.TaxAmount,
        LineTotal = l.LineTotal,
        Notes = l.Notes,
    };

    private static ProviderPayableTransactionResponse MapTransaction(ProviderPayableTransaction t) => new()
    {
        ProviderPayableTransactionId = t.ProviderPayableTransactionId,
        ProviderPayableId = t.ProviderPayableId,
        TransactionType = t.TransactionType,
        ProviderPaymentMethodId = t.ProviderPaymentMethodId,
        ProviderPaymentMethodCode = t.ProviderPaymentMethod?.Code,
        ProviderPaymentMethodName = t.ProviderPaymentMethod?.Name,
        TransactionDate = t.TransactionDate,
        Amount = t.Amount,
        Reference = t.Reference,
        Comments = t.Comments,
        CreatedByEmployeeId = t.CreatedByEmployeeId,
        CreatedByEmployeeName = FormatEmployeeName(t.CreatedByEmployee),
        CreatedAt = t.CreatedAt,
    };
}
