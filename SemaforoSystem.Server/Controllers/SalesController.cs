using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Common;
using SemaforoSystem.Server.DTOs.Sales;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Controllers;

/// <summary>
/// Sales management API.
/// Implements the model described in copilot_sales_migration_api_ui_guide.md:
/// commercial document (sales + sales_lines + sale_payments) with optional
/// account linkage and inventory ledger generation on confirmation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SalesController(ApplicationDbContext db) : ControllerBase
{
    private const string StatusDraft = "DRAFT";
    private const string StatusCompleted = "COMPLETED";
    private const string StatusCanceled = "CANCELED";
    private const string StatusVoid = "VOID";

    private static readonly HashSet<string> AllowedLineTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PRODUCT", "COMBO", "FREE_TEXT", "SERVICE", "EMBROIDERY"
    };

    private static readonly HashSet<string> InventoryAffectingLineTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "PRODUCT"
        // COMBO is intentionally excluded until combo-explosion logic is implemented.
        // FREE_TEXT / SERVICE / EMBROIDERY never affect inventory by default.
    };

    // ═══════════════════════════════════════════════════════════════════
    //  CATALOGS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("catalogs/types")]
    [ProducesResponseType(typeof(IEnumerable<CatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaleTypes(CancellationToken ct)
    {
        var items = await db.SalesTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new CatalogItemResponse
            {
                Id = t.SaleTypeId,
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                Active = t.Active,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("catalogs/statuses")]
    [ProducesResponseType(typeof(IEnumerable<CatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSaleStatuses(CancellationToken ct)
    {
        var items = await db.SaleStatuses
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new CatalogItemResponse
            {
                Id = s.SaleStatusId,
                Code = s.Code,
                Name = s.Name,
                Description = s.Description,
                Active = s.Active,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("catalogs/payment-methods")]
    [ProducesResponseType(typeof(IEnumerable<CatalogItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods(CancellationToken ct)
    {
        var items = await db.PaymentMethods
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new CatalogItemResponse
            {
                Id = p.PaymentMethodId,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Active = p.Active,
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  LIST
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<SaleListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] SaleQueryParameters query, CancellationToken ct)
    {
        var q = db.Sales.AsNoTracking().AsQueryable();

        if (query.SiteId.HasValue) q = q.Where(s => s.SiteId == query.SiteId.Value);
        if (query.ClientId.HasValue) q = q.Where(s => s.ClientId == query.ClientId.Value);
        if (query.EmployeeId.HasValue) q = q.Where(s => s.EmployeeId == query.EmployeeId.Value);
        if (query.SaleTypeId.HasValue) q = q.Where(s => s.SaleTypeId == query.SaleTypeId.Value);
        if (query.SaleStatusId.HasValue) q = q.Where(s => s.SaleStatusId == query.SaleStatusId.Value);
        if (query.AccountId.HasValue) q = q.Where(s => s.AccountId == query.AccountId.Value);
        if (query.From.HasValue)
        {
            var from = ToUtc(query.From.Value);
            q = q.Where(s => s.SaleDate >= from);
        }
        if (query.To.HasValue)
        {
            // If only a date was provided (time at midnight), include the entire day.
            var to = query.To.Value;
            if (to.TimeOfDay == TimeSpan.Zero)
                to = to.AddDays(1).AddTicks(-1);
            to = ToUtc(to);
            q = q.Where(s => s.SaleDate <= to);
        }

        if (!string.IsNullOrWhiteSpace(query.StatusCode))
        {
            var code = query.StatusCode.Trim();
            q = q.Where(s => s.SaleStatus.Code == code);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(s =>
                (s.Folio != null && EF.Functions.ILike(s.Folio, $"%{term}%")) ||
                (s.ExternalReference != null && EF.Functions.ILike(s.ExternalReference, $"%{term}%")) ||
                (s.Client != null && s.Client.Name != null && EF.Functions.ILike(s.Client.Name, $"%{term}%")));
        }

        q = (query.SortBy?.ToLowerInvariant()) switch
        {
            "saledate" => query.SortDescending ? q.OrderByDescending(s => s.SaleDate) : q.OrderBy(s => s.SaleDate),
            "total" => query.SortDescending ? q.OrderByDescending(s => s.Total) : q.OrderBy(s => s.Total),
            "folio" => query.SortDescending ? q.OrderByDescending(s => s.Folio) : q.OrderBy(s => s.Folio),
            _ => query.SortDescending ? q.OrderByDescending(s => s.SaleId) : q.OrderByDescending(s => s.SaleId),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new SaleListItemResponse
            {
                SaleId = s.SaleId,
                Folio = s.Folio,
                SaleDate = s.SaleDate,
                SiteId = s.SiteId,
                SiteName = s.Site.Name,
                ClientId = s.ClientId,
                ClientName = s.Client != null ? s.Client.Name : null,
                EmployeeId = s.EmployeeId,
                EmployeeName = s.Employee.Name,
                SaleTypeId = s.SaleTypeId,
                SaleTypeCode = s.SaleType.Code,
                SaleTypeName = s.SaleType.Name,
                SaleStatusId = s.SaleStatusId,
                SaleStatusCode = s.SaleStatus.Code,
                SaleStatusName = s.SaleStatus.Name,
                AccountId = s.AccountId,
                Subtotal = s.Subtotal,
                DiscountTotal = s.DiscountTotal,
                TaxTotal = s.TaxTotal,
                Total = s.Total,
                PaidTotal = s.SalePayments.Sum(p => (decimal?)p.Amount) ?? 0m,
                Balance = s.Total - (s.SalePayments.Sum(p => (decimal?)p.Amount) ?? 0m),
            })
            .ToListAsync(ct);

        return Ok(new PagedResponse<SaleListItemResponse>
        {
            Items = items,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total,
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DETAIL
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(SaleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var sale = await db.Sales
            .AsNoTracking()
            .Include(s => s.Site)
            .Include(s => s.Client)
            .Include(s => s.Employee)
            .Include(s => s.SaleType)
            .Include(s => s.SaleStatus)
            .Include(s => s.SalesLines).ThenInclude(l => l.SaleLineSerialItems)
            .Include(s => s.SalePayments).ThenInclude(p => p.PaymentMethod)
            .FirstOrDefaultAsync(s => s.SaleId == id, ct);

        if (sale is null)
            return NotFound(new { message = $"Venta con ID {id} no encontrada." });

        return Ok(MapToDetail(sale));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CREATE
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost]
    [ProducesResponseType(typeof(SaleDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateSaleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (request.Lines is null || request.Lines.Count == 0)
            return BadRequest(new { message = "La venta requiere al menos una linea." });

        // Validate line types
        for (int i = 0; i < request.Lines.Count; i++)
        {
            var line = request.Lines[i];
            if (!AllowedLineTypes.Contains(line.LineType))
            {
                ModelState.AddModelError($"Lines[{i}].LineType",
                    $"Tipo de linea invalido. Permitidos: {string.Join(", ", AllowedLineTypes)}");
            }

            if (string.Equals(line.LineType, "FREE_TEXT", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(line.DescriptionSnapshot))
                    ModelState.AddModelError($"Lines[{i}].DescriptionSnapshot",
                        "La descripcion es obligatoria para lineas de texto libre.");
            }
        }
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Validate catalog references
        var site = await db.Sites.FirstOrDefaultAsync(s => s.SiteId == request.SiteId, ct);
        if (site is null) return NotFound(new { message = $"Site {request.SiteId} no encontrado." });

        var saleType = await db.SalesTypes.FirstOrDefaultAsync(t => t.SaleTypeId == request.SaleTypeId, ct);
        if (saleType is null) return NotFound(new { message = $"Sale type {request.SaleTypeId} no encontrado." });

        var employee = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId, ct);
        if (employee is null) return NotFound(new { message = $"Empleado {request.EmployeeId} no encontrado." });

        if (request.ClientId.HasValue)
        {
            var clientExists = await db.Clients.AnyAsync(c => c.ClientId == request.ClientId.Value, ct);
            if (!clientExists) return NotFound(new { message = $"Cliente {request.ClientId} no encontrado." });
        }

        if (request.AccountId.HasValue)
        {
            var accountExists = await db.Accounts.AnyAsync(a => a.AccountId == request.AccountId.Value, ct);
            if (!accountExists) return NotFound(new { message = $"Cuenta {request.AccountId} no encontrada." });
        }

        // Resolve status: explicit id, or DRAFT by code
        SaleStatus? status;
        if (request.SaleStatusId.HasValue)
        {
            status = await db.SaleStatuses.FirstOrDefaultAsync(s => s.SaleStatusId == request.SaleStatusId.Value, ct);
            if (status is null) return NotFound(new { message = $"Sale status {request.SaleStatusId} no encontrado." });
        }
        else
        {
            status = await db.SaleStatuses.FirstOrDefaultAsync(s => s.Code == StatusDraft, ct);
            if (status is null) return BadRequest(new { message = $"No existe el estatus '{StatusDraft}' en la base de datos." });
        }

        // Validate payments if included
        if (request.Payments is { Count: > 0 })
        {
            foreach (var p in request.Payments)
            {
                var pmExists = await db.PaymentMethods.AnyAsync(m => m.PaymentMethodId == p.PaymentMethodId, ct);
                if (!pmExists) return NotFound(new { message = $"Metodo de pago {p.PaymentMethodId} no encontrado." });
            }
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            var now = DateTime.UtcNow;
            var sale = new Sale
            {
                SaleTypeId = request.SaleTypeId,
                SaleStatusId = status.SaleStatusId,
                SiteId = request.SiteId,
                ClientId = request.ClientId,
                EmployeeId = request.EmployeeId,
                AccountId = request.AccountId,
                SaleDate = request.SaleDate ?? now,
                Folio = request.Folio,
                ExternalReference = request.ExternalReference,
                LegacySaleId = request.LegacySaleId,
                Notes = request.Notes,
                CreatedAt = now,
                UpdatedAt = now,
            };

            // Build lines
            int lineNumber = 1;
            foreach (var lineReq in request.Lines)
            {
                var line = BuildLine(lineReq, lineNumber++);
                sale.SalesLines.Add(line);
            }

            RecalculateTotals(sale);

            db.Sales.Add(sale);
            await db.SaveChangesAsync(ct);

            // Serial items: must be created after lines have IDs
            foreach (var (lineReq, line) in request.Lines.Zip(sale.SalesLines))
            {
                if (lineReq.SerialInventoryItemIds is { Count: > 0 })
                {
                    foreach (var serialId in lineReq.SerialInventoryItemIds.Distinct())
                    {
                        db.SaleLineSerialItems.Add(new SaleLineSerialItem
                        {
                            SaleLineId = line.SaleLineId,
                            InventorySerialItemId = serialId,
                        });
                    }
                }
            }

            // Payments
            if (request.Payments is { Count: > 0 })
            {
                foreach (var pReq in request.Payments)
                {
                    db.SalePayments.Add(new SalePayment
                    {
                        SaleId = sale.SaleId,
                        PaymentMethodId = pReq.PaymentMethodId,
                        PaymentDate = pReq.PaymentDate ?? now,
                        Amount = pReq.Amount,
                        Reference = pReq.Reference,
                        Comments = pReq.Comments,
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            // Optional immediate confirmation
            if (request.Confirm)
            {
                await ConfirmCoreAsync(sale.SaleId, ct);
            }

            await tx.CommitAsync(ct);

            var detail = await LoadDetailAsync(sale.SaleId, ct);
            return CreatedAtAction(nameof(GetById), new { id = sale.SaleId }, detail);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(ct);
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            await tx.RollbackAsync(ct);
            return NotFound(new { error = ex.Message });
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  UPDATE (header only; lines/payments via dedicated endpoints)
    // ═══════════════════════════════════════════════════════════════════

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(SaleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateSaleRequest request, CancellationToken ct)
    {
        var sale = await db.Sales.Include(s => s.SaleStatus).FirstOrDefaultAsync(s => s.SaleId == id, ct);
        if (sale is null) return NotFound(new { message = $"Venta {id} no encontrada." });

        if (!string.Equals(sale.SaleStatus.Code, StatusDraft, StringComparison.OrdinalIgnoreCase))
            return Conflict(new { message = "Solo se pueden editar ventas en estado DRAFT." });

        if (request.ClientId.HasValue)
        {
            var clientExists = await db.Clients.AnyAsync(c => c.ClientId == request.ClientId.Value, ct);
            if (!clientExists) return NotFound(new { message = $"Cliente {request.ClientId} no encontrado." });
            sale.ClientId = request.ClientId;
        }

        if (request.AccountId.HasValue)
        {
            var accountExists = await db.Accounts.AnyAsync(a => a.AccountId == request.AccountId.Value, ct);
            if (!accountExists) return NotFound(new { message = $"Cuenta {request.AccountId} no encontrada." });
            sale.AccountId = request.AccountId;
        }

        if (request.Folio != null) sale.Folio = request.Folio;
        if (request.ExternalReference != null) sale.ExternalReference = request.ExternalReference;
        if (request.Notes != null) sale.Notes = request.Notes;
        sale.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(await LoadDetailAsync(sale.SaleId, ct));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  STATUS
    // ═══════════════════════════════════════════════════════════════════

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateSaleStatusRequest request, CancellationToken ct)
    {
        var sale = await db.Sales.Include(s => s.SaleStatus).FirstOrDefaultAsync(s => s.SaleId == id, ct);
        if (sale is null) return NotFound(new { message = $"Venta {id} no encontrada." });

        var target = await db.SaleStatuses.FirstOrDefaultAsync(s => s.Code == request.StatusCode, ct);
        if (target is null) return NotFound(new { message = $"Estatus '{request.StatusCode}' no encontrado." });

        sale.SaleStatusId = target.SaleStatusId;
        sale.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CONFIRM
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Confirms a sale: recalculates totals, emits inventory ledger rows for
    /// inventory-backed lines and transitions the sale status to COMPLETED.
    /// </summary>
    [HttpPost("{id:long}/confirm")]
    [ProducesResponseType(typeof(SaleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await ConfirmCoreAsync(id, ct);
            await tx.CommitAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(ct);
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            await tx.RollbackAsync(ct);
            return NotFound(new { error = ex.Message });
        }

        return Ok(await LoadDetailAsync(id, ct));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  CANCEL
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Cancels a sale. Never deletes rows.
    /// If the sale was COMPLETED, generates compensating RETURN_IN inventory movements.
    /// </summary>
    [HttpPost("{id:long}/cancel")]
    [ProducesResponseType(typeof(SaleDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(long id, [FromBody] CancelSaleRequest? request, CancellationToken ct)
    {
        var sale = await db.Sales
            .Include(s => s.SaleStatus)
            .Include(s => s.SalesLines).ThenInclude(l => l.InventoryTransactionLines)
            .FirstOrDefaultAsync(s => s.SaleId == id, ct);
        if (sale is null) return NotFound(new { message = $"Venta {id} no encontrada." });

        if (string.Equals(sale.SaleStatus.Code, StatusCanceled, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sale.SaleStatus.Code, StatusVoid, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "La venta ya esta cancelada." });
        }

        var canceledStatus = await db.SaleStatuses.FirstOrDefaultAsync(s => s.Code == StatusCanceled, ct);
        if (canceledStatus is null)
            return Conflict(new { message = $"No existe el estatus '{StatusCanceled}' en la base de datos." });

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var wasCompleted = string.Equals(sale.SaleStatus.Code, StatusCompleted, StringComparison.OrdinalIgnoreCase);
            if (wasCompleted)
            {
                // Gather inventory transaction lines generated by this sale's lines
                var saleLineIds = sale.SalesLines.Select(l => l.SaleLineId).ToList();
                var originalLines = await db.InventoryTransactionLines
                    .Where(l => l.SaleLineId.HasValue && saleLineIds.Contains(l.SaleLineId!.Value))
                    .ToListAsync(ct);

                if (originalLines.Count > 0)
                {
                    var compensating = new InventoryTransaction
                    {
                        TransactionType = "RETURN_IN",
                        TransactionDate = DateTime.UtcNow,
                        Reference = $"SALE-CANCEL-{sale.SaleId}",
                        Comments = request?.Reason,
                        UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        CreatedAt = DateTime.UtcNow,
                    };
                    db.InventoryTransactions.Add(compensating);
                    await db.SaveChangesAsync(ct);

                    foreach (var orig in originalLines)
                    {
                        var compensatingLine = new InventoryTransactionLine
                        {
                            InventoryTransactionId = compensating.InventoryTransactionId,
                            SiteId = orig.SiteId,
                            InventoryItemDefinitionId = orig.InventoryItemDefinitionId,
                            QtyDelta = -(orig.QtyDelta ?? 0),
                            UnitPrice = orig.UnitPrice,
                            UnitCost = orig.UnitCost,
                            SaleLineId = orig.SaleLineId,
                        };
                        db.InventoryTransactionLines.Add(compensatingLine);

                        await UpsertBalanceAsync(orig.SiteId, orig.InventoryItemDefinitionId,
                            onHandDelta: -(orig.QtyDelta ?? 0), ct);
                    }

                    await db.SaveChangesAsync(ct);
                }
            }

            sale.SaleStatusId = canceledStatus.SaleStatusId;
            sale.UpdatedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(request?.Reason))
            {
                sale.Notes = string.IsNullOrWhiteSpace(sale.Notes)
                    ? $"[CANCEL] {request.Reason}"
                    : $"{sale.Notes}\n[CANCEL] {request.Reason}";
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(ct);
            return Conflict(new { error = ex.Message });
        }

        return Ok(await LoadDetailAsync(id, ct));
    }

    // ═══════════════════════════════════════════════════════════════════
    //  PAYMENTS
    // ═══════════════════════════════════════════════════════════════════

    [HttpGet("{id:long}/payments")]
    [ProducesResponseType(typeof(IEnumerable<SalePaymentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayments(long id, CancellationToken ct)
    {
        var saleExists = await db.Sales.AnyAsync(s => s.SaleId == id, ct);
        if (!saleExists) return NotFound(new { message = $"Venta {id} no encontrada." });

        var payments = await db.SalePayments
            .AsNoTracking()
            .Where(p => p.SaleId == id)
            .Include(p => p.PaymentMethod)
            .OrderBy(p => p.PaymentDate)
            .Select(p => new SalePaymentResponse
            {
                SalePaymentId = p.SalePaymentId,
                PaymentMethodId = p.PaymentMethodId,
                PaymentMethodCode = p.PaymentMethod.Code,
                PaymentMethodName = p.PaymentMethod.Name,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Reference = p.Reference,
                Comments = p.Comments,
            })
            .ToListAsync(ct);

        return Ok(payments);
    }

    [HttpPost("{id:long}/payments")]
    [ProducesResponseType(typeof(SalePaymentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddPayment(long id, [FromBody] CreateSalePaymentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var sale = await db.Sales.Include(s => s.SaleStatus).FirstOrDefaultAsync(s => s.SaleId == id, ct);
        if (sale is null) return NotFound(new { message = $"Venta {id} no encontrada." });

        if (string.Equals(sale.SaleStatus.Code, StatusCanceled, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(sale.SaleStatus.Code, StatusVoid, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = "No se pueden agregar pagos a una venta cancelada." });
        }

        var pmExists = await db.PaymentMethods.AnyAsync(m => m.PaymentMethodId == request.PaymentMethodId, ct);
        if (!pmExists) return NotFound(new { message = $"Metodo de pago {request.PaymentMethodId} no encontrado." });

        var payment = new SalePayment
        {
            SaleId = id,
            PaymentMethodId = request.PaymentMethodId,
            PaymentDate = request.PaymentDate ?? DateTime.UtcNow,
            Amount = request.Amount,
            Reference = request.Reference,
            Comments = request.Comments,
        };
        db.SalePayments.Add(payment);
        await db.SaveChangesAsync(ct);

        await db.Entry(payment).Reference(p => p.PaymentMethod).LoadAsync(ct);

        var response = new SalePaymentResponse
        {
            SalePaymentId = payment.SalePaymentId,
            PaymentMethodId = payment.PaymentMethodId,
            PaymentMethodCode = payment.PaymentMethod.Code,
            PaymentMethodName = payment.PaymentMethod.Name,
            PaymentDate = payment.PaymentDate,
            Amount = payment.Amount,
            Reference = payment.Reference,
            Comments = payment.Comments,
        };

        return CreatedAtAction(nameof(GetPayments), new { id }, response);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    private static SalesLine BuildLine(CreateSaleLineRequest req, int defaultLineNumber)
    {
        var lineTotal = req.LineTotal ?? ((req.UnitPrice * req.Quantity) - req.DiscountAmount + req.TaxAmount);

        return new SalesLine
        {
            LineNumber = req.LineNumber ?? defaultLineNumber,
            LineType = req.LineType.ToUpperInvariant(),
            ProductId = req.ProductId,
            ProductVisualDefinitionId = req.ProductVisualDefinitionId,
            ProductComboVisualDefinitionId = req.ProductComboVisualDefinitionId,
            InventoryItemDefinitionId = req.InventoryItemDefinitionId,
            SizeId = req.SizeId,
            DescriptionSnapshot = req.DescriptionSnapshot,
            Quantity = req.Quantity,
            UnitPrice = req.UnitPrice,
            DiscountAmount = req.DiscountAmount,
            TaxAmount = req.TaxAmount,
            LineTotal = lineTotal,
            Notes = req.Notes,
            LegacySaleDetailId = req.LegacySaleDetailId,
        };
    }

    private static void RecalculateTotals(Sale sale)
    {
        sale.Subtotal = sale.SalesLines.Sum(l => l.UnitPrice * l.Quantity);
        sale.DiscountTotal = sale.SalesLines.Sum(l => l.DiscountAmount);
        sale.TaxTotal = sale.SalesLines.Sum(l => l.TaxAmount);
        sale.Total = sale.SalesLines.Sum(l => l.LineTotal);
    }

    private async Task ConfirmCoreAsync(long saleId, CancellationToken ct)
    {
        var sale = await db.Sales
            .Include(s => s.SaleStatus)
            .Include(s => s.SalesLines)
            .FirstOrDefaultAsync(s => s.SaleId == saleId, ct)
            ?? throw new KeyNotFoundException($"Venta {saleId} no encontrada.");

        if (string.Equals(sale.SaleStatus.Code, StatusCompleted, StringComparison.OrdinalIgnoreCase))
            return; // idempotent: already confirmed

        if (!string.Equals(sale.SaleStatus.Code, StatusDraft, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Solo se pueden confirmar ventas en estado DRAFT (actual: {sale.SaleStatus.Code}).");

        var completedStatus = await db.SaleStatuses.FirstOrDefaultAsync(s => s.Code == StatusCompleted, ct)
            ?? throw new InvalidOperationException($"No existe el estatus '{StatusCompleted}' en la base de datos.");

        // Recalculate totals from current lines
        RecalculateTotals(sale);

        // Build inventory ledger only for inventory-affecting lines
        var inventoryLines = sale.SalesLines
            .Where(l => InventoryAffectingLineTypes.Contains(l.LineType) && l.InventoryItemDefinitionId.HasValue)
            .ToList();

        if (inventoryLines.Count > 0)
        {
            // Stock availability check
            foreach (var line in inventoryLines)
            {
                var balance = await db.InventoryBalances
                    .FirstOrDefaultAsync(b => b.SiteId == sale.SiteId
                        && b.InventoryItemDefinitionId == line.InventoryItemDefinitionId!.Value, ct);

                var available = (balance?.OnHand ?? 0) - (balance?.Reserved ?? 0);
                if (available < line.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Stock insuficiente para la definicion {line.InventoryItemDefinitionId} en el sitio {sale.SiteId}. " +
                        $"Disponible: {available}, requerido: {line.Quantity}.");
                }
            }

            var header = new InventoryTransaction
            {
                TransactionType = "SALE_OUT",
                TransactionDate = DateTime.UtcNow,
                Reference = $"SALE-{sale.SaleId}",
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.UtcNow,
            };
            db.InventoryTransactions.Add(header);
            await db.SaveChangesAsync(ct);

            foreach (var line in inventoryLines)
            {
                db.InventoryTransactionLines.Add(new InventoryTransactionLine
                {
                    InventoryTransactionId = header.InventoryTransactionId,
                    SiteId = sale.SiteId,
                    InventoryItemDefinitionId = line.InventoryItemDefinitionId!.Value,
                    QtyDelta = -line.Quantity,
                    UnitPrice = line.UnitPrice,
                    SaleLineId = line.SaleLineId,
                });

                await UpsertBalanceAsync(sale.SiteId, line.InventoryItemDefinitionId.Value,
                    onHandDelta: -line.Quantity, ct);
            }

            await db.SaveChangesAsync(ct);
        }

        sale.SaleStatusId = completedStatus.SaleStatusId;
        sale.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task UpsertBalanceAsync(int siteId, int definitionId, int onHandDelta, CancellationToken ct)
    {
        var balance = await db.InventoryBalances
            .FirstOrDefaultAsync(b => b.SiteId == siteId && b.InventoryItemDefinitionId == definitionId, ct);

        if (balance is null)
        {
            db.InventoryBalances.Add(new InventoryBalance
            {
                SiteId = siteId,
                InventoryItemDefinitionId = definitionId,
                OnHand = onHandDelta,
                Reserved = 0,
                UpdatedAt = DateTime.UtcNow,
            });
        }
        else
        {
            balance.OnHand = (balance.OnHand ?? 0) + onHandDelta;
            balance.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<SaleDetailResponse> LoadDetailAsync(long id, CancellationToken ct)
    {
        var sale = await db.Sales
            .AsNoTracking()
            .Include(s => s.Site)
            .Include(s => s.Client)
            .Include(s => s.Employee)
            .Include(s => s.SaleType)
            .Include(s => s.SaleStatus)
            .Include(s => s.SalesLines).ThenInclude(l => l.SaleLineSerialItems)
            .Include(s => s.SalePayments).ThenInclude(p => p.PaymentMethod)
            .FirstAsync(s => s.SaleId == id, ct);

        return MapToDetail(sale);
    }

    private static SaleDetailResponse MapToDetail(Sale s)
    {
        var paid = s.SalePayments.Sum(p => p.Amount);
        return new SaleDetailResponse
        {
            SaleId = s.SaleId,
            Folio = s.Folio,
            SaleDate = s.SaleDate,
            SiteId = s.SiteId,
            SiteName = s.Site?.Name,
            ClientId = s.ClientId,
            ClientName = s.Client?.Name,
            EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee?.Name,
            SaleTypeId = s.SaleTypeId,
            SaleTypeCode = s.SaleType?.Code,
            SaleTypeName = s.SaleType?.Name,
            SaleStatusId = s.SaleStatusId,
            SaleStatusCode = s.SaleStatus?.Code,
            SaleStatusName = s.SaleStatus?.Name,
            AccountId = s.AccountId,
            Subtotal = s.Subtotal,
            DiscountTotal = s.DiscountTotal,
            TaxTotal = s.TaxTotal,
            Total = s.Total,
            PaidTotal = paid,
            Balance = s.Total - paid,
            ExternalReference = s.ExternalReference,
            LegacySaleId = s.LegacySaleId,
            Notes = s.Notes,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt,
            Lines = s.SalesLines
                .OrderBy(l => l.LineNumber)
                .Select(l => new SaleLineResponse
                {
                    SaleLineId = l.SaleLineId,
                    LineNumber = l.LineNumber,
                    LineType = l.LineType,
                    ProductId = l.ProductId,
                    ProductVisualDefinitionId = l.ProductVisualDefinitionId,
                    ProductComboVisualDefinitionId = l.ProductComboVisualDefinitionId,
                    InventoryItemDefinitionId = l.InventoryItemDefinitionId,
                    SizeId = l.SizeId,
                    DescriptionSnapshot = l.DescriptionSnapshot,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice,
                    DiscountAmount = l.DiscountAmount,
                    TaxAmount = l.TaxAmount,
                    LineTotal = l.LineTotal,
                    Notes = l.Notes,
                    LegacySaleDetailId = l.LegacySaleDetailId,
                    SerialInventoryItemIds = l.SaleLineSerialItems
                        .Select(si => si.InventorySerialItemId)
                        .ToList(),
                })
                .ToList(),
            Payments = s.SalePayments
                .OrderBy(p => p.PaymentDate)
                .Select(p => new SalePaymentResponse
                {
                    SalePaymentId = p.SalePaymentId,
                    PaymentMethodId = p.PaymentMethodId,
                    PaymentMethodCode = p.PaymentMethod?.Code,
                    PaymentMethodName = p.PaymentMethod?.Name,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    Comments = p.Comments,
                })
                .ToList(),
        };
    }
}
