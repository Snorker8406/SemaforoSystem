# Product Images Development Guide (VS Code Copilot + .NET Core)

> Purpose: This document defines **how to store, relate, and query product images** with support for:
> - Images at **product level** (generic)
> - Images at **visual-combination level** (**ignores size**; same photo for multiple sizes)
> - Images at **item-definition level** (size-specific or configuration-specific override)
> - Binary storage in PostgreSQL via **BYTEA**, with optional future migration to object storage (`storage_key`/`url`)

Recommended repo path: `.github/copilot-instructions.images.md`

---

## 1) Key Concepts

### 1.1 Why we don’t link images directly to single variants
Products can have **dynamic combinations of N variants** (from different variant systems). A single variant value (e.g., `TINTO`) is **not enough** to uniquely identify the correct photo when multiple variant systems exist (e.g., `Color=TINTO` + `Model=LOYOLA`).

### 1.2 Why `inventory_item_definitions` alone is not enough for images
`inventory_item_definitions` represents the **inventory identity** (product + size + set of variants). But images often **do not change by size** (e.g., the same vest in S/M/L). Attaching images only to `inventory_item_definitions` forces duplication per size.

### 1.3 The solution: a “Visual Definition” level (ignores size)
We introduce a stable entity for visuals:

- **Visual Definition** = `product_id + set of variants (NO size)`

This lets one image cover “Product + variants” across sizes.

---

## 2) Tables & Meaning

### 2.1 `product_images`
Stores image binary + metadata.

**Core columns**
- `product_image_id` (PK)
- `image_bytes` (bytea, NOT NULL)
- `content_type` (varchar) — `image/jpeg`, `image/png`, `image/webp`
- `image_role` (varchar) — `ORIGINAL` | `THUMB` | `DETAIL`
- `width_px` (int, nullable)
- `height_px` (int, nullable)
- `file_size_bytes` (int, nullable)
- `sha256` (char(64), nullable) — optional dedup
- `file_name` (varchar, nullable)
- `alt_text` (varchar, nullable)
- `created_at` (timestamp)

**Future-ready (optional)**
- `storage_key` (varchar, nullable)
- `url` (varchar, nullable)

### 2.2 `product_visual_definitions`
Represents the **visual identity** of a configuration (size ignored).

**Core columns**
- `product_visual_definition_id` (PK)
- `product_id` (FK -> products)
- `variants_hash` (char(64)) — hash of sorted variant IDs (e.g., sha256("12,33,81"))
- `created_at` (timestamp)

**Uniqueness**
- unique `(product_id, variants_hash)`

### 2.3 `product_visual_definition_variants`
Links a visual definition to dynamic variants.

- PK: (`product_visual_definition_id`, `product_variant_id`)

### 2.4 `inventory_item_definitions` (image-related note)
Inventory identity includes size. Recommended extra column:

- `product_visual_definition_id` (nullable FK -> product_visual_definitions)

### 2.5 `product_image_targets`
Links an image to exactly one scope:

- `PRODUCT` — generic product images
- `VISUAL_DEFINITION` — product + variants images (size ignored)
- `ITEM_DEFINITION` — size/config override

Exactly one target FK must be set depending on `target_type`:
- `product_id` for `PRODUCT`
- `product_visual_definition_id` for `VISUAL_DEFINITION`
- `inventory_item_definition_id` for `ITEM_DEFINITION`

Ordering fields:
- `sort_order` (int)
- `is_primary` (bool)

---

## 3) Retrieval Rules (Most Important)

### 3.1 Fallback order when UI knows `inventory_item_definition_id`
1) `ITEM_DEFINITION`
2) `VISUAL_DEFINITION` (via `inventory_item_definitions.product_visual_definition_id`)
3) `PRODUCT`

Within each scope:
- order by `is_primary DESC`, then `sort_order ASC`

### 3.2 When UI knows only `product_id`
Return `PRODUCT` targets for that product.

---

## 4) Role Strategy (`image_role`)

- `THUMB`: list/grid
- `ORIGINAL`: detail page
- `DETAIL`: optional extras

Rule: list endpoints should not return `image_bytes`.

---

## 5) Get-or-create Visual Definition (service algorithm)

Inputs: `product_id`, `variant_ids[]`

1) dedupe + sort `variant_ids`
2) compute `variants_hash = sha256("id1,id2,...")`
3) find `product_visual_definitions` by `(product_id, variants_hash)`
4) if not found: create it + insert rows into `product_visual_definition_variants`

---

## 6) Do / Don’t

### DO
- ✅ Use `VISUAL_DEFINITION` when images are the same across sizes
- ✅ Use `ITEM_DEFINITION` only for true size-specific overrides
- ✅ Keep exactly one `is_primary=true` per target scope

### DON’T
- ❌ Don’t link images to a single variant when combinations exist
- ❌ Don’t duplicate the same image per size unnecessarily
