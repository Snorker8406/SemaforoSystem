# Schools ↔ Uniforms Migration (Visual Definitions)

This project models *uniforms* at the **visual level** (product + variants, ignoring size).  
The old approach (`product_schools` linked to `products`) is not correct when a school requires a specific combination like:

- Product: **CHALECO**
- Variants: **TINTO** (Color system) + **LOYOLA** (another variant system)

That “uniform” is not the base product; it is a **ProductVisualDefinition**.

---

## 1) Key Definitions

### 1.1 `product_visual_definitions` (PVD)
**Purpose:** “Catalog/visual identity” of an item.

- Identifies: **product_id + set of variants**
- **Ignores size**
- Used for:
  - school ↔ uniform mapping
  - images fallback (same photo across sizes)
  - optional pricing fallback (when price is same across sizes)

**Important invariant:** every product must have a “base” PVD with **empty variant set** (for products without variants).

### 1.2 `inventory_item_definitions` (IID)
**Purpose:** “Inventorable identity” (SKU logical).

- Identifies: **product_id + size_id (optional) + set of variants**
- Used for:
  - inventory ledger
  - stock balances
  - price-by-size (most common)
  - sales/reservations

**Relationship:** many IIDs (sizes) can point to one PVD (same variants, ignore size).

---

## 2) What changes we made

### 2.1 Drop the incorrect table
- Removed: `public.product_schools`

### 2.2 New table for schools
- Added: `public.product_visual_definition_schools`

This maps:
- `school_id` ↔ `product_visual_definition_id`

Meaning:
> “This school sells/uses this uniform (product + variants), across all sizes.”

---

## 3) Base PVD for products without variants

Some products have **no variants**.  
We still create a PVD for them with an **empty variant set**.

Implementation detail:
- `variants_hash = sha256("")` for empty set

This guarantees every product can be referenced as a PVD, even with zero variants.

---

## 4) Helper function (service contract)

### 4.1 `get_or_create_product_visual_definition(product_id, variant_ids[])`
Use this from your .NET services when you need a PVD.

Behavior:
- `variant_ids = NULL` or `[]` → returns the **base PVD**
- Otherwise:
  1) dedupe variants
  2) sort ascending
  3) compute `variants_hash = sha256("id1,id2,id3")`
  4) returns existing PVD or creates it + inserts PVD↔variants links

This prevents duplicates and keeps the mapping stable.

---

## 5) Trigger behavior (enabled)

A trigger on `products` automatically creates the **base PVD** whenever a new product is inserted.  
This keeps the invariant true without relying on application code.

---

## 6) How the UI/API should use this

### 6.1 School catalog (“what uniforms does this school handle?”)
Query by school:
- `product_visual_definition_schools` → `product_visual_definitions` → `products`

### 6.2 Selecting size to buy / check stock
Once the user chooses a size:
- resolve `inventory_item_definition_id` from:
  - `product_id`
  - `size_id`
  - variant set
Then:
- inventory uses IID (ledger)
- price uses IID (price-by-size)
- images use:
  - ITEM_DEFINITION override → VISUAL_DEFINITION → PRODUCT fallback

---

## 7) Recommended placement in repo

- SQL migration:
  - `/db/migrations/XXXX_school_uniforms_visual_definitions.sql`
- Copilot instructions:
  - `.github/copilot-instructions.schools-uniforms.md`

---

## 8) Checklist

- [ ] `product_schools` does not exist
- [ ] `product_visual_definition_schools` exists and has rows
- [ ] Every product has a base PVD (empty variants)
- [ ] Inserts into `products` auto-create base PVD (trigger)
- [ ] Application uses PVD for school-uniform mapping

---
