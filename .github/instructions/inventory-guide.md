---
applyTo: "**/*.cs"
---

# Inventory & Ledger Development Guide (for VS Code Copilot + .NET Core)

> Purpose: This document is **the single source of truth** for how this project uses the new inventory tables.
> Use it as development context when implementing controllers/services in ASP.NET Core with EF Core.

---

## 1) Core Principles

### 1.1 Source of truth = Ledger
- The **ledger** is `inventory_transaction_lines`.
- Current stock is derived from `SUM(qty_delta)` grouped by:
  - `site_id`
  - `inventory_item_definition_id`

`inventory_balances` is **optional cache** for fast reads, but **must be kept consistent** with the ledger.

### 1.2 Split inventory models
We support both:
- **Serialized inventory**: tracked **per unit** using `inventory_serial_items`.
- **Non-serialized inventory**: tracked **by quantity** (ledger + balances).

**Never mix both in the same flow.**
If `inventory_item_definitions.is_serialized = true`, then stock movements must link to specific serial items.

### 1.3 Stable Inventory Identity (no hard dependency on product variants)
The inventory does **not** rely on a single "variant id".
We use `inventory_item_definitions` as the stable "SKU logical definition":
- always has `product_id`
- may have `size_id`
- may have N dynamic variants via `inventory_item_definition_variants`

---

## 2) Tables Overview (what each table means)

### 2.1 `inventory_item_definitions`
Represents **what is inventoried** (SKU logical definition).

Key columns:
- `inventory_item_definition_id` (PK)
- `product_id` (FK)
- `size_id` (nullable)
- `is_serialized` (bool)
- `sku_code` (optional unique)
- `is_active`, timestamps

### 2.2 `inventory_item_definition_variants`
Links item definition to dynamic variants:
- `(inventory_item_definition_id, product_variant_id)` PK

### 2.3 `inventory_transactions` (header)
A document/operation:
- entry, sale, transfer, adjustment, return, etc.

Key columns:
- `inventory_transaction_id` (PK)
- `transaction_type` (string or enum)
- `transaction_date`
- `reference`, `comments`, `user_id`

### 2.4 `inventory_transaction_lines` (ledger lines)
Each line is a stock movement.

Key columns:
- `inventory_transaction_line_id` (PK)
- `inventory_transaction_id` (FK)
- `site_id` (FK)
- `inventory_item_definition_id` (FK)
- `qty_delta` (int: +in / -out)
- `unit_cost` (optional, entries)
- `unit_price` (optional, sales)
- `sale_detail_id` (optional)

**Invariant:** `qty_delta != 0`

### 2.5 `inventory_balances` (cache)
Fast "current stock" per site/item definition.
- `(site_id, inventory_item_definition_id)` PK
- `on_hand`, `reserved`

**Invariant:** `on_hand >= 0` and `reserved >= 0`

### 2.6 `inventory_serial_items`
A physical unit tracked individually (serialized).
- `inventory_serial_item_id` (PK)
- `inventory_item_definition_id` (FK)
- `barcode` (unique)
- `serial_number` (optional)
- `current_site_id` (optional but recommended)
- `status` (Available/Reserved/Sold/Damaged/Returned/etc.)

### 2.7 `inventory_serial_item_moves`
Joins a ledger line to the serial items it moved:
- `(inventory_transaction_line_id, inventory_serial_item_id)` PK

**Invariant:** For serialized items, every `inventory_transaction_line` must link to the exact count of serial items that equals `ABS(qty_delta)`.

---

## 3) Transaction Types & Expected Behavior

Use consistent `transaction_type` values (string or enum). Recommended:

- `ENTRY`        : goods received (+)
- `SALE`         : sold (-)
- `RETURN`       : returned (+)
- `ADJUSTMENT`   : stock count correction (+/-)
- `TRANSFER_OUT` : move out from origin (-)
- `TRANSFER_IN`  : move into destination (+)

**Transfer rule:** Transfers are always represented as two transactions (or one transaction with two lines):
- origin site line: negative
- destination site line: positive
They should share the same `reference` so the UI can group them.

---

## 4) API/Service Design (recommended)

### 4.1 Controllers should be thin
Controllers should:
- validate request DTOs
- call a single domain service (e.g. `InventoryService`)
- return results

All inventory rules and DB writes must be inside the service layer.

### 4.2 Always use database transactions
Every inventory operation must be atomic:
- `inventory_transactions`
- `inventory_transaction_lines`
- optional `inventory_serial_item_moves`
- optional `inventory_balances` update
- optional updates to `inventory_serial_items` status/current_site

All must commit or rollback together.

---

## 5) Required Invariants & Validations

### 5.1 Prevent negative stock (non-serialized)
For non-serialized operations that reduce stock (`qty_delta < 0`):
- validate available quantity before committing:
  - `available = on_hand - reserved`
  - require `available >= ABS(qty_delta)`
- or compute from ledger if balances are not used.

### 5.2 Serialized: ensure serial items exist and are available
For `is_serialized = true` and `qty_delta < 0`:
- every serial item must:
  - belong to the same `inventory_item_definition_id`
  - be in the same site (`current_site_id == site_id`) if you track it
  - be in a movable status (usually `Available`)
- must provide **exactly** N serial items where N = `ABS(qty_delta)`
- insert join rows in `inventory_serial_item_moves`
- update serial item status & current_site accordingly.

### 5.3 Serialized: entries must create serial items
For `ENTRY` on serialized items:
- either:
  - create `inventory_serial_items` rows (one per unit)
  - then create ledger line `qty_delta = +N`
  - and link the created serial items to the ledger line
- or:
  - create ledger line per serial item (`qty_delta=+1`) (not recommended, too many rows)

### 5.4 Do not store duplicated identity
Do not store `product_id/size_id/variants` in ledger lines.
Ledger lines reference `inventory_item_definition_id` only.

---

## 6) How to Identify / Create `inventory_item_definitions`

### 6.1 Definition "Key"
A definition is uniquely determined by:
- `product_id`
- `size_id` (nullable)
- the set of `product_variant_id` assigned (possibly empty)
- and `is_serialized`

### 6.2 How to find or create
When receiving a request that describes an item by product/size/variants:
1) Normalize variant IDs:
   - remove duplicates
   - sort ascending
2) Query existing definition:
   - match `product_id`, `size_id`, `is_serialized`
   - and match the exact set of variants
3) If not found: create a new `inventory_item_definitions` row and add rows to `inventory_item_definition_variants`.

**Important:** The API should expose `inventory_item_definition_id` to clients once created so future calls use it directly.

---

## 7) Read Models (for UI)

### 7.1 Current stock per item & site (fast)
Prefer:
- `inventory_balances` if present and maintained

Otherwise:
- aggregate ledger:
  - group by `site_id, inventory_item_definition_id`
  - `SUM(qty_delta)` as on_hand
  - reserved comes from reservation subsystem (if used)

### 7.2 Movement history
Use ledger lines joined to transactions:
- filter by date range, site, product, item definition
- include reference/user/comments

### 7.3 Serialized unit details
- search by `barcode` / `serial_number`
- return:
  - current status
  - current site
  - movement history via `inventory_serial_item_moves` -> `inventory_transaction_lines` -> `inventory_transactions`

---

## 8) Implementation Patterns (EF Core)

### 8.1 Use DbContext sets
Expected DbSets:
- `InventoryItemDefinition`
- `InventoryItemDefinitionVariant`
- `InventoryTransaction`
- `InventoryTransactionLine`
- `InventoryBalance`
- `InventorySerialItem`
- `InventorySerialItemMove`

### 8.2 Concurrency
If using `inventory_balances`:
- use optimistic concurrency (rowversion/timestamp) or
- do updates with `UPDATE ... SET on_hand = on_hand + @delta WHERE ...`
- and validate rows affected.

### 8.3 Performance
- Index frequently filtered columns:
  - balances: `(site_id, inventory_item_definition_id)`
  - ledger lines: `(site_id, inventory_item_definition_id, transaction_date)` via joins
  - serial items: unique on `barcode`, optional on `serial_number`
- Avoid returning huge movement lists; use pagination.

---

## 9) Example Service Behaviors (high-level)

### 9.1 Goods Receipt (ENTRY)
Input:
- site_id
- list of items: { inventory_item_definition_id OR (product/size/variants), qty, unit_cost, serials? }

Steps:
1) create `inventory_transactions` header with type `ENTRY`
2) for each line:
   - if non-serialized:
     - insert ledger line `qty_delta = +qty`
     - update/increment `inventory_balances.on_hand`
   - if serialized:
     - create `qty` rows in `inventory_serial_items` (barcodes required)
     - insert ledger line `qty_delta = +qty`
     - insert join rows in `inventory_serial_item_moves`
     - set serial `status = Available`, `current_site_id = site_id`
3) commit

### 9.2 Sale (SALE)
Input:
- site_id
- list of items: { inventory_item_definition_id, qty OR serial_item_ids, unit_price, sale_detail_id }

Steps:
1) create header type `SALE`
2) for each line:
   - non-serialized:
     - validate available
     - insert ledger `qty_delta = -qty`
     - decrement balances
   - serialized:
     - validate serial items available & at site
     - insert ledger `qty_delta = -N`
     - join serial items to line
     - set serial status = Sold (and optionally sale_detail_id)
3) commit

### 9.3 Adjustment (ADJUSTMENT)
Input:
- site_id
- item definition + delta (+/-)
- reason/comment

Steps:
- insert header + line with `qty_delta = delta`
- update balances accordingly (ensure no negative)
- if serialized: must specify exact serial items moved or created/retired based on delta.

---

## 10) "Do / Don't" Checklist

### DO
- ✅ Always write a ledger line for every inventory change.
- ✅ Use transactions for atomic writes.
- ✅ Keep `inventory_balances` consistent (if used).
- ✅ For serialized, always link exact serial items to ledger lines.
- ✅ Validate available stock before reducing.

### DON'T
- ❌ Don't directly edit balances without a ledger record.
- ❌ Don't use `product_id` alone to move stock (use item definition).
- ❌ Don't mix serial and quantity logic in one endpoint without strict branching.
- ❌ Don't allow partial commits (e.g., ledger inserted but serial updates failed).

---

## 11) Suggested Endpoint Set (optional guidance)

- `POST /api/inventory/entries` (ENTRY)
- `POST /api/inventory/sales` (SALE)
- `POST /api/inventory/transfers` (TRANSFER)
- `POST /api/inventory/adjustments` (ADJUSTMENT)
- `GET  /api/inventory/balances?siteId=...`
- `GET  /api/inventory/movements?...`
- `GET  /api/inventory/serials/{barcode}`

---

## 12) Notes for Copilot
When generating code:
- Prefer a domain service `InventoryService` with methods:
  - `CreateEntryAsync(...)`
  - `CreateSaleAsync(...)`
  - `CreateTransferAsync(...)`
  - `CreateAdjustmentAsync(...)`
- Always wrap writes in `await using var tx = await _db.Database.BeginTransactionAsync();`
- Insert header -> lines -> serial joins -> balances update -> commit.
- Keep validation logic centralized and unit-testable.

---
