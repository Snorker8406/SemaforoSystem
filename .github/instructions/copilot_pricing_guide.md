# Pricing Module Guide (VS Code Copilot + .NET Core)

> Purpose: This document is the **source of truth** for how pricing works in this project:
> - Multiple price lists (e.g., Retail/Wholesale/VIP)
> - Size-linked pricing (via `inventory_item_definitions`)
> - Dynamic variants (via `product_visual_definitions`)
> - Historical prices and promotions (time validity)
> - UI-friendly price resolution (fallback + promo precedence)
>
> Recommended repo path: `.github/copilot-instructions.pricing.md`  
> You can merge this into your main Copilot instructions file.

---

## 1) Core Principles

### 1.1 Prices are time-versioned (history is preserved)
We do **not** overwrite prices in-place.  
Instead we insert new rows with `valid_from` / `valid_to`.

- `valid_to IS NULL` means "open-ended" (current until changed)
- Past prices remain for audit/reporting

### 1.2 Prices resolve by **scope specificity**
Prices can exist at different scopes. The effective price for a selected configuration is resolved in this order:

1) **ITEM_DEFINITION** (most specific; size + variants)
2) **VISUAL_DEFINITION** (same variants set, size ignored)
3) **PRODUCT** (generic fallback)

### 1.3 Promotions are just price rows
Promotions are stored as normal price rows with:
- `price_kind = 'PROMO'`
- `valid_from/valid_to` set to the promo window
- optional `priority` (higher wins)

A promo does **not** replace the base price; it temporarily wins during its validity.

### 1.4 Source of truth for “what is being priced”
We price items by stable identifiers:
- `inventory_item_definitions` for size-linked configurations (SKU logical)
- `product_visual_definitions` for size-agnostic variant combinations
- `products` as last fallback

---

## 2) Tables Overview (What they mean)

### 2.1 `price_lists`
Defines independent lists (Retail, Wholesale, VIP, etc.).

Key columns:
- `price_list_id` (PK)
- `name` (unique)
- `currency` (default MXN)
- `is_default` (only one default)
- `is_active`

**Usage**
- UI selects a list based on user role / customer type.
- API always receives `price_list_id` (or uses default).

---

### 2.2 `price_item_definition_entries`
Prices for an **exact inventory definition** (size-linked and variant-linked).

Key columns:
- `price_entry_id` (PK)
- `price_list_id` (FK -> price_lists)
- `inventory_item_definition_id` (FK -> inventory_item_definitions)
- `price_amount` (numeric)
- `price_kind` (`BASE`|`PROMO`)
- `priority` (int; promo tie-breaker)
- `valid_from`, `valid_to`
- audit: `created_at`, `created_by`, `reason`

**When to use**
- Price differs by **size** (common in apparel/footwear).
- Price differs by a specific combination (rare but supported).

---

### 2.3 `price_visual_definition_entries`
Prices for a **visual definition** (product + variants set, size ignored).

Key columns:
- `price_entry_id` (PK)
- `price_list_id` (FK -> price_lists)
- `product_visual_definition_id` (FK -> product_visual_definitions)
- `price_amount`
- `price_kind`, `priority`
- `valid_from`, `valid_to`

**When to use**
- Same price across multiple sizes for the same “look” (variants set).

---

### 2.4 `price_product_entries`
Prices for a **product** (fallback).

Key columns:
- `price_entry_id` (PK)
- `price_list_id` (FK)
- `product_id` (FK -> products)
- `price_amount`
- `price_kind`, `priority`
- `valid_from`, `valid_to`

**When to use**
- Products without variants/tallas
- Final fallback when no more specific price exists

---

## 3) Validity & Overlap Rules (Important)

### 3.1 BASE prices must not overlap
For each target + price list:
- you must not have two BASE rows whose validity windows overlap.

This is enforced using exclusion constraints (`btree_gist`) in the DB.

### 3.2 PROMO prices may overlap, but resolution uses priority
PROMO overlap is allowed. The chosen promo is determined by:
- higher `priority`
- if tie: newest `valid_from`

---

## 4) Effective Price Resolution (How API/UI should think)

### 4.1 Required inputs
To compute a price reliably, the app should have:
- `price_list_id`
- `inventory_item_definition_id` (preferred)
- optional `as_of` timestamp (default now)

### 4.2 Fallback order
Given `inventory_item_definition_id` (selected configuration):
1) Look for valid entries in `price_item_definition_entries`
2) If none, use `inventory_item_definitions.product_visual_definition_id` and try `price_visual_definition_entries`
3) If none, use `inventory_item_definitions.product_id` and try `price_product_entries`

### 4.3 Within a scope
Within the selected scope:
- PROMO wins over BASE
- higher `priority` wins (for promos)
- newest `valid_from` wins (final tie-break)

### 4.4 DB helper
A helper function exists:
- `public.get_effective_price(price_list_id, inventory_item_definition_id, as_of)`

Use it in read paths if convenient. Alternatively replicate logic in service layer.

---

## 5) UI Recommendations

### 5.1 The UI should work with `inventory_item_definition_id`
For consistent behavior across:
- Stock
- Images (via visual definition fallback)
- Pricing

The UI flow should end with an `inventory_item_definition_id` for the selected configuration.

### 5.2 Selecting a price list
Common strategy:
- default to `price_lists.is_default = true`
- override based on:
  - authenticated user role (admin/retail/wholesale)
  - customer profile (VIP)
  - store/site context

### 5.3 Displaying prices
- Always display the **effective** price result
- If a promo is active, optionally show:
  - "Promo price" and the base price as strikethrough
  - Use `price_kind` returned by resolution

---

## 6) Write Operations (How to change prices)

### 6.1 Setting a new BASE price (recommended flow)
To set a new base price for a target:
1) Close current BASE row (set `valid_to = now()`)
2) Insert new BASE row with `valid_from = now()` and `valid_to = NULL`

This ensures historical continuity and prevents overlap.

### 6.2 Adding a PROMO
Insert a new row with:
- `price_kind = 'PROMO'`
- `valid_from`, `valid_to` (promo window)
- `priority` (optional)
- `promo_name` / `promo_code` (optional)

Do not modify base rows.

### 6.3 Always record the sold price in sales
Even with history, store final price in `sales_details` (or equivalent):
- `unit_price` / `final_unit_price`
- optional `source_price_entry_id` (traceability)

Reason: refunds, discounts, taxes, rounding, and later price changes must not alter the sale.

---

## 7) API Endpoint Patterns (suggested)

### 7.1 Read endpoints
- `GET /api/prices/lists` (active price lists)
- `GET /api/prices/effective?priceListId=..&itemDefinitionId=..` (effective price)
- `GET /api/prices/history?scope=..&id=..&priceListId=..` (history)

### 7.2 Write endpoints
- `POST /api/prices/base` (set base price; closes previous base)
- `POST /api/prices/promo` (add promo)
- `DELETE /api/prices/promo/{priceEntryId}` (optional: cancel promo by setting valid_to=now)

---

## 8) EF Core Implementation Notes

### 8.1 Model naming
Recommended entity names:
- `PriceList`
- `PriceItemDefinitionEntry`
- `PriceVisualDefinitionEntry`
- `PriceProductEntry`

### 8.2 Query efficiency
Index usage expects filters by:
- `price_list_id`
- target id (`inventory_item_definition_id` / `product_visual_definition_id` / `product_id`)
- and validity window (as_of)

When fetching effective price in C#:
- filter `valid_from <= as_of` AND (`valid_to IS NULL OR as_of < valid_to`)
- order by scope rank + promo/base + priority + valid_from desc

### 8.3 Consistency
Price writes should use transactions.
When setting new base price:
- update old row + insert new row in the same transaction.

---

## 9) Do / Don’t Checklist

### DO
- ✅ Use **item definition pricing** for size-linked pricing
- ✅ Use **visual definition pricing** when price is same across sizes
- ✅ Use **product pricing** as fallback
- ✅ Preserve history with `valid_from/valid_to`
- ✅ Use promos as time-bounded price entries
- ✅ Store sold price on sales line items

### DON’T
- ❌ Don’t overwrite base price rows (no history)
- ❌ Don’t allow overlapping base prices for the same target in a list
- ❌ Don’t compute price from product alone when UI already knows `inventory_item_definition_id`
- ❌ Don’t return “current price” without considering active promos

---

## 10) Example Scenarios

### 10.1 Size-linked base price
- List: Retail
- Item Definition: “CHALECO TINTO LOYOLA” size M
- Store base price in `price_item_definition_entries` (BASE, open-ended)

### 10.2 Promo for a single size
- Insert PROMO row for that item definition with `valid_to` in 7 days
- Promo wins during that window

### 10.3 Same price across sizes (variants-only)
- Store base price in `price_visual_definition_entries`
- All sizes fallback to the same visual definition price unless overridden at item-definition

### 10.4 Product fallback
- Products without sizes/variants: store base in `price_product_entries`

---
