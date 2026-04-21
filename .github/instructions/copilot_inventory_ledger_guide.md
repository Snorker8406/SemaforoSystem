# Inventory & Ledger Guide (VS Code Copilot + .NET Core + Migration)

> Purpose: This document is the **source of truth** for how inventory (“existencias”) works in the destination PostgreSQL schema (DESTINO_Postgres_Schema(5).md).
> - Inventory is **ledger-based** (transactions + lines)
> - Supports **serialized** and **non-serialized** items
> - Uses `inventory_item_definitions` as the SKU-like identity (product + size + variants)
> - Provides reservation primitives for checkout flows

Recommended repo path:
- Development: `.github/copilot-instructions.inventory.md`
- Migration: `docs/migration/inventory-mapping.md`

---

## 0) What “Inventory” means in this system

### 0.1 Inventory is not stored as a single “stock table”
Inventory is derived from events:
- **Transactions** (`inventory_transactions`) are the headers
- **Lines** (`inventory_transaction_lines`) are the atomic movements (`qty_delta`)
- **Serial items** (`inventory_serial_items`) represent physical units when serialized

`inventory_balances` exists as a **cache** (fast reads), not as source of truth.

---

## 1) Inventory Identity: `inventory_item_definitions` (SKU logical)

### 1.1 Why this exists
`inventory_item_definitions` represents *what* you stock/sell:
- `product_id`
- optional `size_id`
- optional `product_visual_definition_id` (variants set)
- `is_serialized`
- `sku_code` unique

**Key columns**
- `inventory_item_definition_id` (PK, integer, sequence default)
- `product_id` (FK → products)
- `size_id` (FK → sizes, nullable)
- `product_visual_definition_id` (FK → product_visual_definitions, nullable)
- `is_serialized` (boolean)
- `sku_code` (varchar, UNIQUE)
- `is_active` (boolean)
- `created_at`, `updated_at` (timestamptz)

### 1.2 Variants for an item definition
Variants are linked via:
- `inventory_item_definition_variants(inventory_item_definition_id, product_variant_id)` (PK composite)

**Rule**
- Every stock/price/sale operation should ultimately target an `inventory_item_definition_id`.

---

## 2) Ledger: transactions + lines

### 2.1 `inventory_transactions` (header)
Represents a business event/document:
- `inventory_transaction_id` (bigint, sequence default)
- `transaction_type` (varchar(30))
- `transaction_date` (timestamptz, default now())
- `reference` (varchar(100))
- `comments` (text)
- `user_id` (varchar(200))
- `created_at` (timestamptz, default now())

**Note**
- In the current schema there are no FKs from this header to `sites`; site is defined at line level.

### 2.2 `inventory_transaction_lines` (detail)
Each line moves quantity for one SKU in one site:
- `inventory_transaction_line_id` (PK)
- `inventory_transaction_id` (FK → inventory_transactions)
- `site_id` (FK → sites)
- `inventory_item_definition_id` (FK → inventory_item_definitions)
- `qty_delta` (int): + enters, − leaves
- `unit_cost` (numeric, nullable; entries/adjustments)
- `unit_price` (numeric, nullable; sales)
- `sale_detail_id` (nullable; for traceability to sales)
- `source_site_id`, `target_site_id` (nullable; recommended for transfers)

**Rule**
- `qty_delta != 0`
- For transfers, prefer **two lines** (out/in) and link them with the same `reference` or a correlation id in the header.

### 2.3 Transaction types (recommended vocabulary)
Use a consistent string enum:
- `INITIAL_LOAD`
- `PURCHASE_IN`
- `SALE_OUT`
- `TRANSFER_OUT`
- `TRANSFER_IN`
- `ADJUSTMENT`
- `LOSS`
- `DAMAGE`
- `RETURN_IN`

---

## 3) Serialized inventory

### 3.1 `inventory_serial_items`
Used when `inventory_item_definitions.is_serialized=true`.

**Key columns**
- `inventory_serial_item_id` (bigint, PK)
- `inventory_item_definition_id` (FK)
- `current_site_id` (FK → sites)
- `barcode` (unique)
- `serial_number` (optional)
- `status` (smallint; Available/Reserved/Sold/Damaged/Returned/etc.)
- `created_at`, `deactivated_at` (timestamptz)

### 3.2 `inventory_serial_item_moves` (traceability)
Links serial units to ledger lines:
- PK (`inventory_transaction_line_id`, `inventory_serial_item_id`)
- FKs → `inventory_transaction_lines`, `inventory_serial_items`

**Hard rule**
- Any ledger movement that affects serialized items must record which barcodes moved via `inventory_serial_item_moves`.

---

## 4) Reservations (checkout concurrency)

### 4.1 `inventory_reservations`
Reserves a quantity of an item in a site.
- `inventory_reservation_id` (bigint)
- `site_id`, `inventory_item_definition_id`
- `quantity`
- `sale_order_id` (optional)
- `status` (smallint)
- `created_at` (timestamptz)
- `expires_at` (timestamptz, nullable)

### 4.2 `inventory_reservation_serial_items`
For serialized reservations:
- `(inventory_reservation_id, inventory_serial_item_id)` PK composite

**Rule**
- Serialized reservations should reserve specific serial items, not just a quantity.

---

## 5) Balances (cache) — `inventory_balances`

`inventory_balances` stores:
- `(site_id, inventory_item_definition_id)` PK
- `on_hand` (int)
- `reserved` (int)
- `updated_at` (timestamptz, default now())

**Guideline**
- Update balances only as a **cache** in the same DB transaction that writes the ledger.
- The source of truth is still: `SUM(inventory_transaction_lines.qty_delta)`.

---

## 6) UI Patterns (frontend behavior)

### 6.1 Selection flow must resolve to `inventory_item_definition_id`
Recommended UI flow:
1) Choose product + variants → resolve/create `product_visual_definition_id`
2) Choose size → resolve/create `inventory_item_definition_id`
3) Use `inventory_item_definition_id` for:
   - pricing (item → visual → product)
   - images (item → visual → product)
   - inventory (balances + availability)
   - sales lines

### 6.2 Screens to implement
- **Stock by Site**: list balances grouped by site
- **Kardex**: ledger view filtered by site + item definition + date range
- **Serial Search**: lookup by barcode, show current site/status + last movements

---

## 7) API Patterns (recommended)

### 7.1 Read endpoints
- `GET /api/inventory/balances?siteId=..&q=..`  (cache-first, optional computed fallback)
- `GET /api/inventory/items/{itemDefinitionId}/balances?siteId=..`
- `GET /api/inventory/serial/{barcode}`
- `GET /api/inventory/ledger?siteId=..&itemDefinitionId=..&from=..&to=..`

### 7.2 Write endpoints (ledger-only)
- `POST /api/inventory/transactions`
  - header + lines
  - for serialized: include list of barcodes per line
- `POST /api/inventory/reservations`
  - for serialized: include serial ids to reserve
- `POST /api/inventory/reservations/{id}/commit`
- `POST /api/inventory/reservations/{id}/cancel`

**Hard rule**
- Never mutate inventory state without recording a ledger transaction.

---

## 8) Migration from Legacy (SQL Server) — mapping essentials

Legacy core inventory table: `dbo.Existencias`
Typical fields include:
- Product ID, Site/Sucursal, Size/Talla (text), Quantity, Serial/Barcode fields

Migration strategy:
1) Preload catalog maps:
   - legacy product → `product_id`
   - legacy sucursal → `site_id`
   - legacy talla text → `size_id`
2) Resolve/create:
   - `product_visual_definition_id` (variants signature; may be “default” with no variants)
   - `inventory_item_definition_id` (visual definition + size)
3) Create ledger bootstrap:
   - one `INITIAL_LOAD` transaction per site
   - non-serialized: aggregate quantity per (site, item_definition) → one line with `+qty`
   - serialized: create one `inventory_serial_items` per unit and one line with `+1` per unit (or grouped line + moves)

Transfers: `dbo.Movimientos_Productos`
- model as `TRANSFER_OUT` (source) and `TRANSFER_IN` (target)
- link both with same `reference`

---

## 9) Status & integrity rules (implementation)

### 9.1 Prevent negative stock
Before writing a negative `qty_delta`:
- compute `available = on_hand - reserved`
- ensure `available >= abs(qty_delta)`
- for serialized: ensure the specific serials are Available and at the correct site.

### 9.2 Atomic write
Every inventory operation must be one DB transaction:
- insert header
- insert lines
- insert serial moves (if any)
- update serial item status/site
- update balances cache

---

## 10) Schema review (post-fix)

✅ The previously-detected issue (time-only audit columns) has been corrected:
- inventory audit columns now use `timestamp with time zone` with `now()` defaults
- `inventory_transactions.inventory_transaction_id` uses a sequence default

Remaining (optional improvements):
- Consider setting `inventory_transactions.created_at` to NOT NULL (currently nullable in schema) if you want strict audit guarantees.
- Consider adding indexes for common queries:
  - `inventory_transaction_lines(site_id, inventory_item_definition_id)`
  - `inventory_transactions(transaction_date)`
  - `inventory_serial_items(current_site_id, inventory_item_definition_id)`

---

## 11) Do / Don’t Checklist

### DO
- ✅ Use `inventory_item_definitions` as the inventory unit
- ✅ Write all changes as ledger entries
- ✅ Track serialized movements with explicit barcodes
- ✅ Use reservations for checkout concurrency

### DON’T
- ❌ Don’t treat balances as truth
- ❌ Don’t sell serialized items without recording which units moved
- ❌ Don’t allow writes that can create negative stock (unless explicitly allowed as ADJUSTMENT)

---
