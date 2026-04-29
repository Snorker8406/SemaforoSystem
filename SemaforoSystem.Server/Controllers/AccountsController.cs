using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Accounts;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

/// <summary>
/// Customer accounts receivable API (credit + layaway).
/// Implements copilot_accounts_credit_layaway_guide.md:
/// ledger-driven balance from account_transactions, normalized items,
/// optional credit / layaway extensions, optional installment schedule.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AccountsController(ApplicationDbContext db) : ControllerBase
{
    // Statuses
    private const string StatusOpen = "OPEN";
    private const string StatusPaid = "PAID";
    private const string StatusOverdue = "OVERDUE";
    private const string StatusCanceled = "CANCELED";
    private const string StatusClosed = "CLOSED";

    // Types
    private const string TypeCredit = "CREDIT";
    private const string TypeLayaway = "LAYAWAY";

    // Transaction types (account_transactions.transaction_type)
    private const string TxCharge = "CHARGE";
    private const string TxPayment = "PAYMENT";
    private const string TxDiscount = "DISCOUNT";
    private const string TxInterest = "INTEREST";
    private const string TxAdjustment = "ADJUSTMENT";
    private const string TxCancellation = "CANCELLATION";

    private static readonly HashSet<string> AllowedTransactionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        TxCharge, TxPayment, TxDiscount, TxInterest, TxAdjustment, TxCancellation
    };

    // ═══════════════════════════════════════════════════════════════════
    //  CATALOGS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("catalogs/types")]
    [ProducesResponseType(typeof(IEnumerable<AccountCatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountTypes(CancellationToken ct) =>
        Ok(await db.AccountTypes.AsNoTracking().OrderBy(t => t.Name)
            .Select(t => new AccountCatalogItemResponse
            {
                Id = t.AccountTypeId,
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive,
            }).ToListAsync(ct));

    [HttpGet("catalogs/statuses")]
    [ProducesResponseType(typeof(IEnumerable<AccountCatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountStatuses(CancellationToken ct) =>
        Ok(await db.AccountStatuses.AsNoTracking().OrderBy(s => s.Name)
            .Select(s => new AccountCatalogItemResponse
            {
                Id = s.AccountStatusId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
            }).ToListAsync(ct));

    [HttpGet("catalogs/transaction-types")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public IActionResult GetTransactionTypes() => Ok(AllowedTransactionTypes.OrderBy(t => t));

    // ═══════════════════════════════════════════════════════════════════
    //  LIST
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<AccountListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] AccountQueryParameters query, CancellationToken ct)
    {
        var q = db.Accounts.AsNoTracking().AsQueryable();

        if (query.ClientId.HasValue) q = q.Where(a => a.ClientId == query.ClientId.Value);
        if (query.SiteId.HasValue) q = q.Where(a => a.SiteId == query.SiteId.Value);
        if (query.AccountTypeId.HasValue) q = q.Where(a => a.AccountTypeId == query.AccountTypeId.Value);
        if (query.AccountStatusId.HasValue) q = q.Where(a => a.AccountStatusId == query.AccountStatusId.Value);
        if (!string.IsNullOrWhiteSpace(query.TypeCode))
        {
            var tc = query.TypeCode.Trim();
            q = q.Where(a => a.AccountType.Code == tc);
        }
        if (!string.IsNullOrWhiteSpace(query.StatusCode))
        {
            var sc = query.StatusCode.Trim();
            q = q.Where(a => a.AccountStatus.Code == sc);
        }
        if (query.From.HasValue) q = q.Where(a => a.OpeningDate >= query.From.Value);
        if (query.To.HasValue) q = q.Where(a => a.OpeningDate <= query.To.Value);
        if (query.Overdue == true)
        {
            var today = DateTime.UtcNow;
            q = q.Where(a => a.DueDate != null && a.DueDate < today);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(a =>
                (a.Reference != null && EF.Functions.ILike(a.Reference, $"%{term}%")) ||
                EF.Functions.ILike(a.Client.Name, $"%{term}%") ||
                EF.Functions.ILike(a.Client.LastName, $"%{term}%") ||
                (a.Client.LastNameMother != null && EF.Functions.ILike(a.Client.LastNameMother, $"%{term}%")));
        }

        q = (query.SortBy?.ToLowerInvariant()) switch
        {
            "openingdate" => query.SortDescending ? q.OrderByDescending(a => a.OpeningDate) : q.OrderBy(a => a.OpeningDate),
            "duedate" => query.SortDescending ? q.OrderByDescending(a => a.DueDate) : q.OrderBy(a => a.DueDate),
            "client" => query.SortDescending ? q.OrderByDescending(a => a.Client.Name) : q.OrderBy(a => a.Client.Name),
            _ => query.SortDescending ? q.OrderByDescending(a => a.AccountId) : q.OrderByDescending(a => a.AccountId),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new
            {
                a.AccountId,
                a.ClientId,
                ClientName = (a.Client.Name ?? string.Empty)
                    + " " + (a.Client.LastName ?? string.Empty)
                    + " " + (a.Client.LastNameMother ?? string.Empty),
                a.SiteId,
                SiteName = a.Site.Name,
                a.AccountTypeId,
                AccountTypeCode = a.AccountType.Code,
                AccountTypeName = a.AccountType.Name,
                a.AccountStatusId,
                AccountStatusCode = a.AccountStatus.Code,
                AccountStatusName = a.AccountStatus.Name,
                a.OpeningDate,
                a.DueDate,
                a.CurrencyCode,
                a.Reference,
                // Balance = SUM(transactions.amount). Charges are positive, payments negative.
                BalanceSum = a.AccountTransactions.Sum(t => (decimal?)t.Amount) ?? 0m,
                TotalCharged = a.AccountTransactions
                    .Where(t => t.TransactionType == TxCharge || t.TransactionType == TxInterest)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
                TotalPaidAbs = a.AccountTransactions
                    .Where(t => t.TransactionType == TxPayment || t.TransactionType == TxDiscount)
                    .Sum(t => (decimal?)t.Amount) ?? 0m,
            })
            .ToListAsync(ct);

        var result = items.Select(a => new AccountListItemResponse
        {
            AccountId = a.AccountId,
            ClientId = a.ClientId,
            ClientName = a.ClientName.Trim(),
            SiteId = a.SiteId,
            SiteName = a.SiteName,
            AccountTypeId = a.AccountTypeId,
            AccountTypeCode = a.AccountTypeCode,
            AccountTypeName = a.AccountTypeName,
            AccountStatusId = a.AccountStatusId,
            AccountStatusCode = a.AccountStatusCode,
            AccountStatusName = a.AccountStatusName,
            OpeningDate = a.OpeningDate,
            DueDate = a.DueDate,
            CurrencyCode = a.CurrencyCode,
            Reference = a.Reference,
            // Absolute values for UI, while Balance is the canonical SUM.
            TotalCharged = a.TotalCharged,
            TotalPaid = Math.Abs(a.TotalPaidAbs),
            Balance = a.BalanceSum,
        }).ToList();

        return Ok(new PagedResponse<AccountListItemResponse>
        {
            Items = result,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DETAIL
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var account = await LoadAccountAsync(id, ct);
        if (account is null) return NotFound(new { message = $"Cuenta {id} no encontrada." });
        return Ok(MapToDetail(account));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CREATE
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Validate references
        if (!await db.Clients.AnyAsync(c => c.ClientId == request.ClientId, ct))
            return NotFound(new { message = $"Cliente {request.ClientId} no encontrado." });
        if (!await db.Sites.AnyAsync(s => s.SiteId == request.SiteId, ct))
            return NotFound(new { message = $"Site {request.SiteId} no encontrado." });
        if (!await db.Employees.AnyAsync(e => e.EmployeeId == request.OpenedByEmployeeId, ct))
            return NotFound(new { message = $"Empleado {request.OpenedByEmployeeId} no encontrado." });

        // Resolve account type
        AccountType? accountType;
        if (request.AccountTypeId.HasValue)
        {
            accountType = await db.AccountTypes.FirstOrDefaultAsync(t => t.AccountTypeId == request.AccountTypeId.Value, ct);
            if (accountType is null) return NotFound(new { message = $"Tipo de cuenta {request.AccountTypeId} no encontrado." });
        }
        else if (!string.IsNullOrWhiteSpace(request.AccountTypeCode))
        {
            var code = request.AccountTypeCode.Trim().ToUpperInvariant();
            accountType = await db.AccountTypes.FirstOrDefaultAsync(t => t.Code == code, ct);
            if (accountType is null) return NotFound(new { message = $"Tipo de cuenta '{request.AccountTypeCode}' no encontrado." });
        }
        else
        {
            return BadRequest(new { message = "Debe especificar AccountTypeId o AccountTypeCode." });
        }

        // Resolve status (default OPEN)
        AccountStatus? status;
        if (request.AccountStatusId.HasValue)
        {
            status = await db.AccountStatuses.FirstOrDefaultAsync(s => s.AccountStatusId == request.AccountStatusId.Value, ct);
        }
        else
        {
            var code = (request.AccountStatusCode ?? StatusOpen).Trim().ToUpperInvariant();
            status = await db.AccountStatuses.FirstOrDefaultAsync(s => s.Code == code, ct);
        }
        if (status is null) return NotFound(new { message = "Estatus de cuenta no encontrado." });

        // Validate type-specific extension
        bool isCredit = string.Equals(accountType.Code, TypeCredit, StringComparison.OrdinalIgnoreCase);
        bool isLayaway = string.Equals(accountType.Code, TypeLayaway, StringComparison.OrdinalIgnoreCase);
        if (isLayaway && request.Layaway is null)
            return BadRequest(new { message = "La cuenta tipo LAYAWAY requiere la seccion 'Layaway'." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;

            var account = new Account
            {
                ClientId = request.ClientId,
                SiteId = request.SiteId,
                OpenedByEmployeeId = request.OpenedByEmployeeId,
                AccountTypeId = accountType.AccountTypeId,
                AccountStatusId = status.AccountStatusId,
                OpeningDate = request.OpeningDate ?? now,
                DueDate = request.DueDate,
                CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "MXN" : request.CurrencyCode!,
                Reference = request.Reference,
                Notes = request.Notes,
                CreatedAt = now,
                UpdatedAt = now,
            };

            decimal itemsTotal = 0m;
            foreach (var itReq in request.Items)
            {
                var lineTotal = itReq.LineTotal ?? ((itReq.UnitPrice * itReq.Quantity) - itReq.DiscountAmount);
                itemsTotal += lineTotal;

                account.AccountItems.Add(new AccountItem
                {
                    InventoryItemDefinitionId = itReq.InventoryItemDefinitionId,
                    ProductId = itReq.ProductId,
                    ProductVisualDefinitionId = itReq.ProductVisualDefinitionId,
                    DescriptionSnapshot = itReq.DescriptionSnapshot,
                    Quantity = itReq.Quantity,
                    UnitPrice = itReq.UnitPrice,
                    DiscountAmount = itReq.DiscountAmount,
                    LineTotal = lineTotal,
                    CreatedAt = now,
                });
            }

            // Type-specific extensions
            if (isCredit && request.Credit is not null)
            {
                account.CreditAccount = new CreditAccount
                {
                    CreditDays = request.Credit.CreditDays,
                    GraceUntil = request.Credit.GraceUntil,
                    OriginalDueDate = request.Credit.OriginalDueDate,
                    ExtendedDueDate = request.Credit.ExtendedDueDate,
                    CreditLimitSnapshot = request.Credit.CreditLimitSnapshot,
                    RequiresGuarantor = request.Credit.RequiresGuarantor,
                };
            }
            if (isLayaway)
            {
                account.LayawayAccount = new LayawayAccount
                {
                    DepositAmount = request.Layaway!.DepositAmount,
                    ExpectedArrivalDate = request.Layaway.ExpectedArrivalDate,
                    ReadyDate = request.Layaway.ReadyDate,
                    DeliveryDate = request.Layaway.DeliveryDate,
                    IsReady = request.Layaway.IsReady,
                    ExpirationDate = request.Layaway.ExpirationDate,
                    CancellationPolicyNotes = request.Layaway.CancellationPolicyNotes,
                };
            }

            // Installments
            if (request.Installments is { Count: > 0 })
            {
                var numbers = new HashSet<int>();
                foreach (var inst in request.Installments)
                {
                    if (!numbers.Add(inst.InstallmentNumber))
                        return BadRequest(new { message = $"Numero de mensualidad duplicado: {inst.InstallmentNumber}." });

                    account.AccountInstallments.Add(new AccountInstallment
                    {
                        InstallmentNumber = inst.InstallmentNumber,
                        DueDate = inst.DueDate,
                        ExpectedAmount = inst.ExpectedAmount,
                        PaidAmount = 0m,
                        IsPaid = false,
                        Notes = inst.Notes,
                    });
                }
            }

            db.Accounts.Add(account);
            await db.SaveChangesAsync(ct);

            // Serial items (need AccountItemId)
            var itemsByIndex = account.AccountItems.ToList();
            for (int i = 0; i < request.Items.Count; i++)
            {
                var serials = request.Items[i].SerialInventoryItemIds;
                if (serials is { Count: > 0 })
                {
                    var itemId = itemsByIndex[i].AccountItemId;
                    foreach (var serialId in serials.Distinct())
                    {
                        db.AccountItemSerialItems.Add(new AccountItemSerialItem
                        {
                            AccountItemId = itemId,
                            InventorySerialItemId = serialId,
                        });
                    }
                }
            }

            // Initial CHARGE transaction (canonical ledger entry)
            if (request.GenerateInitialCharge && itemsTotal > 0m)
            {
                db.AccountTransactions.Add(new AccountTransaction
                {
                    AccountId = account.AccountId,
                    TransactionType = TxCharge,
                    TransactionDate = now,
                    Amount = itemsTotal, // CHARGE is positive
                    Reference = "INITIAL_CHARGE",
                    CreatedByEmployeeId = request.OpenedByEmployeeId,
                    CreatedAt = now,
                });
            }

            // Initial PAYMENT (e.g. layaway deposit captured immediately)
            if (request.InitialPaymentAmount is > 0m)
            {
                db.AccountTransactions.Add(new AccountTransaction
                {
                    AccountId = account.AccountId,
                    TransactionType = TxPayment,
                    TransactionDate = now,
                    Amount = -Math.Abs(request.InitialPaymentAmount.Value),
                    PaymentMethod = request.InitialPaymentMethod,
                    Reference = "INITIAL_PAYMENT",
                    CreatedByEmployeeId = request.OpenedByEmployeeId,
                    CreatedAt = now,
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var reloaded = await LoadAccountAsync(account.AccountId, ct);
            return CreatedAtAction(nameof(GetById), new { id = account.AccountId }, MapToDetail(reloaded!));
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(ct);
            return BadRequest(new { error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UPDATE HEADER
    // ═══════════════════════════════════════════════════════════════════

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateAccountRequest request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        if (request.AccountStatusId.HasValue)
        {
            var exists = await db.AccountStatuses.AnyAsync(s => s.AccountStatusId == request.AccountStatusId.Value, ct);
            if (!exists) return NotFound(new { message = $"Estatus {request.AccountStatusId} no encontrado." });
            account.AccountStatusId = request.AccountStatusId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.AccountStatusCode))
        {
            var code = request.AccountStatusCode.Trim().ToUpperInvariant();
            var st = await db.AccountStatuses.FirstOrDefaultAsync(s => s.Code == code, ct);
            if (st is null) return NotFound(new { message = $"Estatus '{request.AccountStatusCode}' no encontrado." });
            account.AccountStatusId = st.AccountStatusId;
        }

        if (request.DueDate.HasValue) account.DueDate = request.DueDate;
        if (!string.IsNullOrWhiteSpace(request.CurrencyCode)) account.CurrencyCode = request.CurrencyCode!;
        if (request.Reference != null) account.Reference = request.Reference;
        if (request.Notes != null) account.Notes = request.Notes;
        account.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        var reloaded = await LoadAccountAsync(id, ct);
        return Ok(MapToDetail(reloaded!));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STATUS
    // ═══════════════════════════════════════════════════════════════════

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateAccountStatusRequest request, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        var code = request.StatusCode.Trim().ToUpperInvariant();
        var target = await db.AccountStatuses.FirstOrDefaultAsync(s => s.Code == code, ct);
        if (target is null) return NotFound(new { message = $"Estatus '{request.StatusCode}' no encontrado." });

        account.AccountStatusId = target.AccountStatusId;
        if (code == StatusClosed) account.ClosedDate ??= DateTime.UtcNow;
        if (code == StatusCanceled) account.CanceledDate ??= DateTime.UtcNow;
        account.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CANCEL
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(typeof(AccountDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelAccountRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var account = await db.Accounts
            .Include(a => a.AccountStatus)
            .Include(a => a.AccountTransactions)
            .FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        if (string.Equals(account.AccountStatus.Code, StatusCanceled, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "La cuenta ya esta cancelada." });

        var canceledStatus = await db.AccountStatuses.FirstOrDefaultAsync(s => s.Code == StatusCanceled, ct);
        if (canceledStatus is null) return Conflict(new { message = "Estatus CANCELED no existe." });

        if (!await db.Employees.AnyAsync(e => e.EmployeeId == request.CanceledByEmployeeId, ct))
            return NotFound(new { message = $"Empleado {request.CanceledByEmployeeId} no encontrado." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTime.UtcNow;
            var balance = account.AccountTransactions.Sum(t => t.Amount);

            // If there is an outstanding balance, create a compensating CANCELLATION entry
            // that neutralizes it so SUM(amount) = 0 for the canceled account.
            if (balance != 0m)
            {
                db.AccountTransactions.Add(new AccountTransaction
                {
                    AccountId = account.AccountId,
                    TransactionType = TxCancellation,
                    TransactionDate = now,
                    Amount = -balance,
                    Reference = $"ACCOUNT-CANCEL-{account.AccountId}",
                    Comments = request.Reason,
                    CreatedByEmployeeId = request.CanceledByEmployeeId,
                    CreatedAt = now,
                });
            }

            account.AccountStatusId = canceledStatus.AccountStatusId;
            account.CanceledDate ??= now;
            account.UpdatedAt = now;
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                account.Notes = string.IsNullOrWhiteSpace(account.Notes)
                    ? $"[CANCEL] {request.Reason}"
                    : $"{account.Notes}\n[CANCEL] {request.Reason}";
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(ct);
            return Conflict(new { error = ex.Message });
        }

        var reloaded = await LoadAccountAsync(id, ct);
        return Ok(MapToDetail(reloaded!));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ITEMS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{id:long}/items")]
    [ProducesResponseType(typeof(IEnumerable<AccountItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetItems(long id, CancellationToken ct)
    {
        var accountExists = await db.Accounts.AnyAsync(a => a.AccountId == id, ct);
        if (!accountExists) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        var items = await db.AccountItems
            .AsNoTracking()
            .Where(i => i.AccountId == id)
            .Include(i => i.AccountItemSerialItems)
            .OrderBy(i => i.AccountItemId)
            .ToListAsync(ct);

        return Ok(items.Select(MapItem));
    }

    [HttpPost("{id:long}/items")]
    [ProducesResponseType(typeof(AccountItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(long id, [FromBody] CreateAccountItemRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var account = await db.Accounts.Include(a => a.AccountStatus).FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        if (string.Equals(account.AccountStatus.Code, StatusCanceled, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "No se pueden agregar items a una cuenta cancelada." });

        var lineTotal = request.LineTotal ?? ((request.UnitPrice * request.Quantity) - request.DiscountAmount);
        var now = DateTime.UtcNow;

        var item = new AccountItem
        {
            AccountId = id,
            InventoryItemDefinitionId = request.InventoryItemDefinitionId,
            ProductId = request.ProductId,
            ProductVisualDefinitionId = request.ProductVisualDefinitionId,
            DescriptionSnapshot = request.DescriptionSnapshot,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            DiscountAmount = request.DiscountAmount,
            LineTotal = lineTotal,
            CreatedAt = now,
        };
        db.AccountItems.Add(item);
        await db.SaveChangesAsync(ct);

        if (request.SerialInventoryItemIds is { Count: > 0 })
        {
            foreach (var serialId in request.SerialInventoryItemIds.Distinct())
            {
                db.AccountItemSerialItems.Add(new AccountItemSerialItem
                {
                    AccountItemId = item.AccountItemId,
                    InventorySerialItemId = serialId,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        account.UpdatedAt = now;
        await db.SaveChangesAsync(ct);

        await db.Entry(item).Collection(i => i.AccountItemSerialItems).LoadAsync(ct);
        return CreatedAtAction(nameof(GetItems), new { id }, MapItem(item));
    }

    [HttpDelete("{id:long}/items/{itemId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteItem(long id, long itemId, CancellationToken ct)
    {
        var item = await db.AccountItems
            .Include(i => i.AccountItemSerialItems)
            .FirstOrDefaultAsync(i => i.AccountItemId == itemId && i.AccountId == id, ct);
        if (item is null) return NotFound(new { message = $"Item {itemId} no encontrado en la cuenta {id}." });

        db.AccountItemSerialItems.RemoveRange(item.AccountItemSerialItems);
        db.AccountItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  TRANSACTIONS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{id:long}/transactions")]
    [ProducesResponseType(typeof(IEnumerable<AccountTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTransactions(long id, CancellationToken ct)
    {
        var exists = await db.Accounts.AnyAsync(a => a.AccountId == id, ct);
        if (!exists) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        var txs = await db.AccountTransactions
            .AsNoTracking()
            .Where(t => t.AccountId == id)
            .Include(t => t.CreatedByEmployee)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.AccountTransactionId)
            .ToListAsync(ct);

        return Ok(txs.Select(MapTransaction));
    }

    [HttpPost("{id:long}/transactions")]
    [ProducesResponseType(typeof(AccountTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTransaction(long id, [FromBody] CreateAccountTransactionRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var type = request.TransactionType.Trim().ToUpperInvariant();
        if (!AllowedTransactionTypes.Contains(type))
        {
            ModelState.AddModelError(nameof(request.TransactionType),
                $"Tipo invalido. Permitidos: {string.Join(", ", AllowedTransactionTypes)}");
            return BadRequest(ModelState);
        }

        var account = await db.Accounts.Include(a => a.AccountStatus).FirstOrDefaultAsync(a => a.AccountId == id, ct);
        if (account is null) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        if (string.Equals(account.AccountStatus.Code, StatusCanceled, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "La cuenta esta cancelada; no acepta nuevas transacciones." });

        if (!await db.Employees.AnyAsync(e => e.EmployeeId == request.CreatedByEmployeeId, ct))
            return NotFound(new { message = $"Empleado {request.CreatedByEmployeeId} no encontrado." });

        // Apply canonical sign convention per the guide:
        //   CHARGE / INTEREST  => positive
        //   PAYMENT / DISCOUNT => negative
        //   ADJUSTMENT / CANCELLATION => caller may provide any sign; we allow positive amount
        //   and require them to use the Reference/Comments to explain direction.
        decimal signedAmount = type switch
        {
            TxCharge or TxInterest => Math.Abs(request.Amount),
            TxPayment or TxDiscount => -Math.Abs(request.Amount),
            _ => request.Amount, // ADJUSTMENT / CANCELLATION pass-through
        };

        var now = DateTime.UtcNow;
        var tx = new AccountTransaction
        {
            AccountId = id,
            TransactionType = type,
            TransactionDate = request.TransactionDate ?? now,
            Amount = signedAmount,
            PaymentMethod = request.PaymentMethod,
            Reference = request.Reference,
            Comments = request.Comments,
            CreatedByEmployeeId = request.CreatedByEmployeeId,
            CreatedAt = now,
        };

        await using var dbTx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            db.AccountTransactions.Add(tx);
            await db.SaveChangesAsync(ct);

            // If PAYMENT, try to apply against next unpaid installment automatically.
            if (type == TxPayment)
            {
                await AutoApplyPaymentToInstallmentsAsync(id, -signedAmount, ct);
            }

            // Auto status: balance <= 0 => PAID
            var balance = await db.AccountTransactions
                .Where(t => t.AccountId == id)
                .SumAsync(t => (decimal?)t.Amount, ct) ?? 0m;
            if (balance <= 0m)
            {
                var paid = await db.AccountStatuses.FirstOrDefaultAsync(s => s.Code == StatusPaid, ct);
                if (paid is not null && account.AccountStatusId != paid.AccountStatusId)
                {
                    account.AccountStatusId = paid.AccountStatusId;
                    account.ClosedDate ??= now;
                }
            }
            else if (account.DueDate.HasValue && account.DueDate.Value < now)
            {
                var overdue = await db.AccountStatuses.FirstOrDefaultAsync(s => s.Code == StatusOverdue, ct);
                if (overdue is not null && account.AccountStatusId != overdue.AccountStatusId)
                    account.AccountStatusId = overdue.AccountStatusId;
            }

            account.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            await dbTx.CommitAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            await dbTx.RollbackAsync(ct);
            return Conflict(new { error = ex.Message });
        }

        await db.Entry(tx).Reference(t => t.CreatedByEmployee).LoadAsync(ct);
        return CreatedAtAction(nameof(GetTransactions), new { id }, MapTransaction(tx));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  INSTALLMENTS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{id:long}/installments")]
    [ProducesResponseType(typeof(IEnumerable<AccountInstallmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInstallments(long id, CancellationToken ct)
    {
        var exists = await db.Accounts.AnyAsync(a => a.AccountId == id, ct);
        if (!exists) return NotFound(new { message = $"Cuenta {id} no encontrada." });

        var list = await db.AccountInstallments
            .AsNoTracking()
            .Where(i => i.AccountId == id)
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new AccountInstallmentResponse
            {
                AccountInstallmentId = i.AccountInstallmentId,
                InstallmentNumber = i.InstallmentNumber,
                DueDate = i.DueDate,
                ExpectedAmount = i.ExpectedAmount,
                PaidAmount = i.PaidAmount,
                IsPaid = i.IsPaid,
                PaidDate = i.PaidDate,
                Notes = i.Notes,
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpPost("{id:long}/installments")]
    [ProducesResponseType(typeof(AccountInstallmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddInstallment(long id, [FromBody] CreateInstallmentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!await db.Accounts.AnyAsync(a => a.AccountId == id, ct))
            return NotFound(new { message = $"Cuenta {id} no encontrada." });

        var duplicate = await db.AccountInstallments
            .AnyAsync(i => i.AccountId == id && i.InstallmentNumber == request.InstallmentNumber, ct);
        if (duplicate)
            return Conflict(new { message = $"Ya existe la mensualidad {request.InstallmentNumber} para esta cuenta." });

        var entity = new AccountInstallment
        {
            AccountId = id,
            InstallmentNumber = request.InstallmentNumber,
            DueDate = request.DueDate,
            ExpectedAmount = request.ExpectedAmount,
            PaidAmount = 0m,
            IsPaid = false,
            Notes = request.Notes,
        };
        db.AccountInstallments.Add(entity);
        await db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetInstallments), new { id }, new AccountInstallmentResponse
        {
            AccountInstallmentId = entity.AccountInstallmentId,
            InstallmentNumber = entity.InstallmentNumber,
            DueDate = entity.DueDate,
            ExpectedAmount = entity.ExpectedAmount,
            PaidAmount = entity.PaidAmount,
            IsPaid = entity.IsPaid,
            PaidDate = entity.PaidDate,
            Notes = entity.Notes,
        });
    }

    [HttpPatch("{id:long}/installments/{installmentId:long}")]
    [ProducesResponseType(typeof(AccountInstallmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInstallment(long id, long installmentId, [FromBody] UpdateInstallmentRequest request, CancellationToken ct)
    {
        var entity = await db.AccountInstallments
            .FirstOrDefaultAsync(i => i.AccountId == id && i.AccountInstallmentId == installmentId, ct);
        if (entity is null) return NotFound(new { message = $"Mensualidad {installmentId} no encontrada." });

        if (request.DueDate.HasValue) entity.DueDate = request.DueDate.Value;
        if (request.ExpectedAmount.HasValue) entity.ExpectedAmount = request.ExpectedAmount.Value;
        if (request.PaidAmount.HasValue) entity.PaidAmount = request.PaidAmount.Value;
        if (request.IsPaid.HasValue) entity.IsPaid = request.IsPaid.Value;
        if (request.PaidDate.HasValue) entity.PaidDate = request.PaidDate;
        if (request.Notes != null) entity.Notes = request.Notes;

        // Auto-mark as paid when PaidAmount >= ExpectedAmount.
        if (entity.PaidAmount >= entity.ExpectedAmount && !entity.IsPaid)
        {
            entity.IsPaid = true;
            entity.PaidDate ??= DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return Ok(new AccountInstallmentResponse
        {
            AccountInstallmentId = entity.AccountInstallmentId,
            InstallmentNumber = entity.InstallmentNumber,
            DueDate = entity.DueDate,
            ExpectedAmount = entity.ExpectedAmount,
            PaidAmount = entity.PaidAmount,
            IsPaid = entity.IsPaid,
            PaidDate = entity.PaidDate,
            Notes = entity.Notes,
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LAYAWAY OPERATIONAL ACTIONS
    // ═══════════════════════════════════════════════════════════════════

    [HttpPatch("{id:long}/layaway")]
    [ProducesResponseType(typeof(LayawayAccountResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLayawayDates(long id, [FromBody] UpdateLayawayDatesRequest request, CancellationToken ct)
    {
        var layaway = await db.LayawayAccounts.FirstOrDefaultAsync(l => l.AccountId == id, ct);
        if (layaway is null) return NotFound(new { message = $"La cuenta {id} no es de tipo LAYAWAY." });

        if (request.ExpectedArrivalDate.HasValue) layaway.ExpectedArrivalDate = request.ExpectedArrivalDate;
        if (request.ReadyDate.HasValue) layaway.ReadyDate = request.ReadyDate;
        if (request.DeliveryDate.HasValue) layaway.DeliveryDate = request.DeliveryDate;
        if (request.IsReady.HasValue) layaway.IsReady = request.IsReady.Value;
        if (request.ExpirationDate.HasValue) layaway.ExpirationDate = request.ExpirationDate;

        await db.SaveChangesAsync(ct);

        return Ok(new LayawayAccountResponse
        {
            DepositAmount = layaway.DepositAmount,
            ExpectedArrivalDate = layaway.ExpectedArrivalDate,
            ReadyDate = layaway.ReadyDate,
            DeliveryDate = layaway.DeliveryDate,
            IsReady = layaway.IsReady,
            ExpirationDate = layaway.ExpirationDate,
            CancellationPolicyNotes = layaway.CancellationPolicyNotes,
        });
    }

    [HttpPatch("{id:long}/layaway/ready")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkLayawayReady(long id, CancellationToken ct)
    {
        var layaway = await db.LayawayAccounts.FirstOrDefaultAsync(l => l.AccountId == id, ct);
        if (layaway is null) return NotFound(new { message = $"La cuenta {id} no es de tipo LAYAWAY." });
        layaway.IsReady = true;
        layaway.ReadyDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:long}/layaway/deliver")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeliverLayaway(long id, CancellationToken ct)
    {
        var layaway = await db.LayawayAccounts.FirstOrDefaultAsync(l => l.AccountId == id, ct);
        if (layaway is null) return NotFound(new { message = $"La cuenta {id} no es de tipo LAYAWAY." });
        layaway.DeliveryDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private async Task AutoApplyPaymentToInstallmentsAsync(long accountId, decimal paymentAbs, CancellationToken ct)
    {
        if (paymentAbs <= 0m) return;

        var pending = await db.AccountInstallments
            .Where(i => i.AccountId == accountId && !i.IsPaid)
            .OrderBy(i => i.InstallmentNumber)
            .ToListAsync(ct);

        var remaining = paymentAbs;
        var now = DateTime.UtcNow;
        foreach (var inst in pending)
        {
            if (remaining <= 0m) break;

            var due = inst.ExpectedAmount - inst.PaidAmount;
            if (due <= 0m)
            {
                inst.IsPaid = true;
                inst.PaidDate ??= now;
                continue;
            }

            var apply = Math.Min(remaining, due);
            inst.PaidAmount += apply;
            remaining -= apply;

            if (inst.PaidAmount >= inst.ExpectedAmount)
            {
                inst.IsPaid = true;
                inst.PaidDate ??= now;
            }
        }
    }

    private Task<Account?> LoadAccountAsync(long id, CancellationToken ct) =>
        db.Accounts
            .AsNoTracking()
            .Include(a => a.Client)
            .Include(a => a.Site)
            .Include(a => a.OpenedByEmployee)
            .Include(a => a.AccountType)
            .Include(a => a.AccountStatus)
            .Include(a => a.AccountItems).ThenInclude(i => i.AccountItemSerialItems)
            .Include(a => a.AccountTransactions).ThenInclude(t => t.CreatedByEmployee)
            .Include(a => a.AccountInstallments)
            .Include(a => a.CreditAccount)
            .Include(a => a.LayawayAccount)
            .FirstOrDefaultAsync(a => a.AccountId == id, ct);

    private static AccountDetailResponse MapToDetail(Account a)
    {
        var totalCharged = a.AccountTransactions
            .Where(t => t.TransactionType == TxCharge || t.TransactionType == TxInterest)
            .Sum(t => t.Amount);
        var totalPaidAbs = Math.Abs(a.AccountTransactions
            .Where(t => t.TransactionType == TxPayment || t.TransactionType == TxDiscount)
            .Sum(t => t.Amount));
        var balance = a.AccountTransactions.Sum(t => t.Amount);

        return new AccountDetailResponse
        {
            AccountId = a.AccountId,
            ClientId = a.ClientId,
            ClientName = ($"{a.Client?.Name} {a.Client?.LastName} {a.Client?.LastNameMother}").Trim(),
            SiteId = a.SiteId,
            SiteName = a.Site?.Name,
            AccountTypeId = a.AccountTypeId,
            AccountTypeCode = a.AccountType?.Code,
            AccountTypeName = a.AccountType?.Name,
            AccountStatusId = a.AccountStatusId,
            AccountStatusCode = a.AccountStatus?.Code,
            AccountStatusName = a.AccountStatus?.Name,
            OpeningDate = a.OpeningDate,
            DueDate = a.DueDate,
            CurrencyCode = a.CurrencyCode,
            Reference = a.Reference,
            TotalCharged = totalCharged,
            TotalPaid = totalPaidAbs,
            Balance = balance,
            OpenedByEmployeeId = a.OpenedByEmployeeId,
            OpenedByEmployeeName = a.OpenedByEmployee?.Name,
            ClosedDate = a.ClosedDate,
            CanceledDate = a.CanceledDate,
            Notes = a.Notes,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            Items = a.AccountItems.Select(MapItem).ToList(),
            Transactions = a.AccountTransactions
                .OrderBy(t => t.TransactionDate)
                .ThenBy(t => t.AccountTransactionId)
                .Select(MapTransaction).ToList(),
            Installments = a.AccountInstallments
                .OrderBy(i => i.InstallmentNumber)
                .Select(i => new AccountInstallmentResponse
                {
                    AccountInstallmentId = i.AccountInstallmentId,
                    InstallmentNumber = i.InstallmentNumber,
                    DueDate = i.DueDate,
                    ExpectedAmount = i.ExpectedAmount,
                    PaidAmount = i.PaidAmount,
                    IsPaid = i.IsPaid,
                    PaidDate = i.PaidDate,
                    Notes = i.Notes,
                }).ToList(),
            Credit = a.CreditAccount is null ? null : new CreditAccountResponse
            {
                CreditDays = a.CreditAccount.CreditDays,
                GraceUntil = a.CreditAccount.GraceUntil,
                OriginalDueDate = a.CreditAccount.OriginalDueDate,
                LastExtensionDate = a.CreditAccount.LastExtensionDate,
                ExtendedDueDate = a.CreditAccount.ExtendedDueDate,
                CreditLimitSnapshot = a.CreditAccount.CreditLimitSnapshot,
                RequiresGuarantor = a.CreditAccount.RequiresGuarantor,
            },
            Layaway = a.LayawayAccount is null ? null : new LayawayAccountResponse
            {
                DepositAmount = a.LayawayAccount.DepositAmount,
                ExpectedArrivalDate = a.LayawayAccount.ExpectedArrivalDate,
                ReadyDate = a.LayawayAccount.ReadyDate,
                DeliveryDate = a.LayawayAccount.DeliveryDate,
                IsReady = a.LayawayAccount.IsReady,
                ExpirationDate = a.LayawayAccount.ExpirationDate,
                CancellationPolicyNotes = a.LayawayAccount.CancellationPolicyNotes,
            },
        };
    }

    private static AccountItemResponse MapItem(AccountItem i) => new()
    {
        AccountItemId = i.AccountItemId,
        InventoryItemDefinitionId = i.InventoryItemDefinitionId,
        ProductId = i.ProductId,
        ProductVisualDefinitionId = i.ProductVisualDefinitionId,
        DescriptionSnapshot = i.DescriptionSnapshot,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        DiscountAmount = i.DiscountAmount,
        LineTotal = i.LineTotal,
        CreatedAt = i.CreatedAt,
        SerialInventoryItemIds = i.AccountItemSerialItems
            .Select(s => s.InventorySerialItemId).ToList(),
    };

    private static AccountTransactionResponse MapTransaction(AccountTransaction t) => new()
    {
        AccountTransactionId = t.AccountTransactionId,
        AccountId = t.AccountId,
        TransactionType = t.TransactionType,
        TransactionDate = t.TransactionDate,
        Amount = t.Amount,
        PaymentMethod = t.PaymentMethod,
        Reference = t.Reference,
        Comments = t.Comments,
        CreatedByEmployeeId = t.CreatedByEmployeeId,
        CreatedByEmployeeName = t.CreatedByEmployee?.Name,
        CreatedAt = t.CreatedAt,
    };
}
