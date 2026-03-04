using Microsoft.EntityFrameworkCore;
using SemaforoSystem.Server.DTOs.Inventory;
using SemaforoSystem.Server.Models;

namespace SemaforoSystem.Server.Services;

public class InventoryService(ApplicationDbContext db)
{
    // ═══════════════════════════════════════════════════════════════════
    //  ENTRY (Goods Receipt)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates an ENTRY inventory transaction (goods receipt).
    /// Supports both serialized and non-serialized items.
    /// All writes are wrapped in a single DB transaction.
    /// </summary>
    public async Task<InventoryTransactionResponse> CreateEntryAsync(
        CreateEntryRequest request, string? userId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            // ── 1. Create transaction header ──────────────────────────
            var header = new InventoryTransaction
            {
                TransactionType = "ENTRY",
                TransactionDate = DateTimeOffset.UtcNow,
                Reference = request.Reference,
                Comments = request.Comments,
                UserId = userId,
                CreatedAt = TimeOnly.FromDateTime(DateTime.UtcNow),
            };

            db.InventoryTransactions.Add(header);
            await db.SaveChangesAsync(ct);

            var response = new InventoryTransactionResponse
            {
                InventoryTransactionId = header.InventoryTransactionId,
                TransactionType = header.TransactionType,
                TransactionDate = header.TransactionDate,
                Reference = header.Reference,
                Comments = header.Comments,
                UserId = header.UserId,
                Lines = [],
            };

            // ── 2. Process each line ──────────────────────────────────
            foreach (var lineReq in request.Lines)
            {
                // 2a. Resolve or create the inventory item definition
                var definition = await ResolveDefinitionAsync(lineReq, ct);

                bool isSerialized = definition.IsSerialized;

                // 2b. Create ledger line
                var ledgerLine = new InventoryTransactionLine
                {
                    InventoryTransactionId = header.InventoryTransactionId,
                    SiteId = request.SiteId,
                    InventoryItemDefinitionId = definition.InventoryItemDefinitionId,
                    QtyDelta = lineReq.Quantity,
                    UnitCost = lineReq.UnitCost,
                };

                db.InventoryTransactionLines.Add(ledgerLine);
                await db.SaveChangesAsync(ct);

                var lineResponse = new InventoryTransactionLineResponse
                {
                    InventoryTransactionLineId = ledgerLine.InventoryTransactionLineId,
                    SiteId = request.SiteId,
                    InventoryItemDefinitionId = definition.InventoryItemDefinitionId,
                    SkuCode = definition.SkuCode,
                    NameSnapshot = definition.NameSnapshot,
                    QtyDelta = ledgerLine.QtyDelta,
                    UnitCost = ledgerLine.UnitCost,
                };

                // 2d. Serialized: auto-generate barcodes, create serial items + join rows
                if (isSerialized)
                {
                    // Load the product to get name + current serial count
                    var product = await db.Products.FindAsync([definition.ProductId], ct)
                        ?? throw new KeyNotFoundException($"Product {definition.ProductId} not found.");

                    long currentCount = product.SerialCount ?? 0;
                    char prefix = GetBarcodePrefix(product.Name);

                    lineResponse.SerialItems = [];

                    for (int i = 0; i < lineReq.Quantity; i++)
                    {
                        long serialNumber = currentCount + 1 + i;
                        string barcode = $"{product.ProductId}{prefix}{serialNumber}";

                        var serial = new InventorySerialItem
                        {
                            InventoryItemDefinitionId = definition.InventoryItemDefinitionId,
                            CurrentSiteId = request.SiteId,
                            Barcode = barcode,
                            SerialNumber = serialNumber,
                            Status = 1, // Available
                            CreatedAt = DateTimeOffset.UtcNow,
                        };

                        db.InventorySerialItems.Add(serial);
                        await db.SaveChangesAsync(ct);

                        // Join row: inventory_serial_item_moves
                        ledgerLine.InventorySerialItems.Add(serial);

                        lineResponse.SerialItems.Add(new SerialItemInfo
                        {
                            InventorySerialItemId = serial.InventorySerialItemId,
                            Barcode = serial.Barcode,
                            SerialNumber = serial.SerialNumber,
                            Status = serial.Status,
                        });
                    }

                    // Update the product's serial count
                    product.SerialCount = currentCount + lineReq.Quantity;
                    await db.SaveChangesAsync(ct);
                }

                // 2e. Update balance cache (upsert)
                await UpsertBalanceAsync(request.SiteId, definition.InventoryItemDefinitionId,
                    onHandDelta: lineReq.Quantity, reservedDelta: 0, ct);

                response.Lines.Add(lineResponse);
            }

            await tx.CommitAsync(ct);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  READ
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns a single inventory transaction with its lines.</summary>
    public async Task<InventoryTransactionResponse?> GetTransactionAsync(
        long transactionId, CancellationToken ct = default)
    {
        var header = await db.InventoryTransactions
            .Include(t => t.InventoryTransactionLines)
                .ThenInclude(l => l.InventoryItemDefinition)
            .Include(t => t.InventoryTransactionLines)
                .ThenInclude(l => l.Site)
            .Include(t => t.InventoryTransactionLines)
                .ThenInclude(l => l.InventorySerialItems)
            .FirstOrDefaultAsync(t => t.InventoryTransactionId == transactionId, ct);

        if (header is null) return null;

        return new InventoryTransactionResponse
        {
            InventoryTransactionId = header.InventoryTransactionId,
            TransactionType = header.TransactionType,
            TransactionDate = header.TransactionDate,
            Reference = header.Reference,
            Comments = header.Comments,
            UserId = header.UserId,
            Lines = header.InventoryTransactionLines.Select(l => new InventoryTransactionLineResponse
            {
                InventoryTransactionLineId = l.InventoryTransactionLineId,
                SiteId = l.SiteId,
                SiteName = l.Site?.Name,
                InventoryItemDefinitionId = l.InventoryItemDefinitionId,
                SkuCode = l.InventoryItemDefinition?.SkuCode,
                NameSnapshot = l.InventoryItemDefinition?.NameSnapshot,
                QtyDelta = l.QtyDelta,
                UnitCost = l.UnitCost,
                UnitPrice = l.UnitPrice,
                SerialItems = l.InventorySerialItems.Select(s => new SerialItemInfo
                {
                    InventorySerialItemId = s.InventorySerialItemId,
                    Barcode = s.Barcode,
                    SerialNumber = s.SerialNumber,
                    Status = s.Status,
                }).ToList(),
            }).ToList(),
        };
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Private helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Resolves an existing InventoryItemDefinition or creates one
    /// based on product/size/variants from the line request.
    /// </summary>
    private async Task<InventoryItemDefinition> ResolveDefinitionAsync(
        CreateEntryLineRequest lineReq, CancellationToken ct)
    {
        // Fast path: caller already knows the id
        if (lineReq.InventoryItemDefinitionId.HasValue)
        {
            var existing = await db.InventoryItemDefinitions
                .FirstOrDefaultAsync(d => d.InventoryItemDefinitionId == lineReq.InventoryItemDefinitionId.Value, ct)
                ?? throw new KeyNotFoundException(
                    $"InventoryItemDefinition {lineReq.InventoryItemDefinitionId.Value} not found.");

            return existing;
        }

        // Slow path: resolve by product/size/variants
        if (!lineReq.ProductId.HasValue)
            throw new InvalidOperationException(
                "Either InventoryItemDefinitionId or ProductId must be provided.");

        int productId = lineReq.ProductId.Value;
        int? sizeId = lineReq.SizeId;
        bool isSerialized = lineReq.IsSerialized
            ?? (await db.Products.FindAsync([productId], ct))?.Serialize
            ?? false;

        var normalizedVariants = (lineReq.VariantIds ?? [])
            .Distinct()
            .OrderBy(v => v)
            .ToList();

        // Try to find an existing definition matching the exact key
        var candidatesQuery = db.InventoryItemDefinitions
            .Include(d => d.ProductVariants)
            .Where(d => d.ProductId == productId
                     && d.SizeId == sizeId
                     && d.IsSerialized == isSerialized);

        var candidates = await candidatesQuery.ToListAsync(ct);

        var match = candidates.FirstOrDefault(d =>
        {
            var existingVariants = d.ProductVariants
                .Select(v => v.ProductVariantId)
                .OrderBy(v => v)
                .ToList();

            return existingVariants.SequenceEqual(normalizedVariants);
        });

        if (match is not null)
            return match;

        // Create new definition
        var product = await db.Products.FindAsync([productId], ct)
            ?? throw new KeyNotFoundException($"Product {productId} not found.");

        var sizeName = sizeId.HasValue
            ? (await db.Sizes.FindAsync([sizeId.Value], ct))?.SizeValue
            : null;

        var definition = new InventoryItemDefinition
        {
            ProductId = productId,
            SizeId = sizeId,
            IsSerialized = isSerialized,
            SkuCode = GenerateSkuCode(productId, sizeId, normalizedVariants),
            NameSnapshot = BuildNameSnapshot(product.Name, sizeName),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.InventoryItemDefinitions.Add(definition);
        await db.SaveChangesAsync(ct);

        // Link variants
        if (normalizedVariants.Count > 0)
        {
            var variants = await db.ProductVariants
                .Where(v => normalizedVariants.Contains(v.ProductVariantId))
                .ToListAsync(ct);

            if (variants.Count != normalizedVariants.Count)
                throw new KeyNotFoundException("One or more variant ids not found.");

            foreach (var v in variants)
                definition.ProductVariants.Add(v);

            await db.SaveChangesAsync(ct);
        }

        return definition;
    }

    /// <summary>
    /// Upserts the inventory_balances row, incrementing on_hand / reserved.
    /// </summary>
    private async Task UpsertBalanceAsync(
        int siteId, int definitionId, int onHandDelta, int reservedDelta, CancellationToken ct)
    {
        var balance = await db.InventoryBalances
            .FirstOrDefaultAsync(b => b.SiteId == siteId
                                   && b.InventoryItemDefinitionId == definitionId, ct);

        if (balance is null)
        {
            balance = new InventoryBalance
            {
                SiteId = siteId,
                InventoryItemDefinitionId = definitionId,
                OnHand = onHandDelta,
                Reserved = reservedDelta,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            db.InventoryBalances.Add(balance);
        }
        else
        {
            balance.OnHand = (balance.OnHand ?? 0) + onHandDelta;
            balance.Reserved = (balance.Reserved ?? 0) + reservedDelta;
            balance.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
    }

    private static string GenerateSkuCode(int productId, int? sizeId, List<int> variantIds)
    {
        var parts = new List<string> { $"P{productId}" };

        if (sizeId.HasValue) parts.Add($"S{sizeId.Value}");
        if (variantIds.Count > 0) parts.Add($"V{string.Join("-", variantIds)}");

        return string.Join("_", parts);
    }

    private static string BuildNameSnapshot(string? productName, string? sizeName)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(productName)) parts.Add(productName);
        if (!string.IsNullOrWhiteSpace(sizeName)) parts.Add($"[{sizeName}]");
        return parts.Count > 0 ? string.Join(" ", parts) : "Unknown";
    }

    /// <summary>
    /// Gets the first letter (uppercase) of the product name for barcode prefix.
    /// Falls back to 'X' if name is empty.
    /// </summary>
    private static char GetBarcodePrefix(string? productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return 'X';

        return char.ToUpperInvariant(productName.TrimStart()[0]);
    }
}
