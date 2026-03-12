# Embroidery Integration Guide (VS Code Copilot + .NET Core + Migration)

> Purpose: This document defines how **embroidery designs (ponchados)** integrate into the destination PostgreSQL model:
> - A **School** can act as a **dynamic Variant** (so it becomes part of the item identity)
> - A **Visual Definition** (product + variants, size ignored) can require **one or more embroideries**
> - Inventory and pricing continue to work through `inventory_item_definitions` and the ledger/price engine
>
> Recommended repo path (development): `.github/copilot-instructions.embroidery.md`  
> Recommended repo path (migration): `docs/migration/embroidery-mapping.md` (or merge into MAPEO_Migracion_v2.md)

---

## 1) Business Meaning

### 1.1 What is an `embroidery`?
An `embroidery` row represents a **digitized punch design** (e.g., DST/EMB files), typically associated with a school logo.
It is a reusable design asset that can be applied to products at defined placements (e.g., left chest, back, sleeve).

### 1.2 What does “CHALECO ROJO BASILIO VADILLO” mean?
- **CHALECO**: base product (`products`)
- **ROJO**: variant (color system) → `product_variants`
- **BASILIO VADILLO**: school name meaning the item is **embroidered for that school**
  - We model this school as a **Variant** too (system `SCHOOL`)
- The combination `{ROJO, BASILIO VADILLO}` becomes a `product_visual_definition`
- That `product_visual_definition` links to one or more `embroideries`
- Inventory is tracked per `inventory_item_definition` (same visual definition but **with size**)

---

## 2) Key Design Principle

### 2.1 Embroidery must be part of the item identity when it changes the SKU
An embroidered version is not just a service; it creates a different “sellable/stockable” item.
Therefore, the school (and optionally other embroidery-related variants) must be included in the **variant set** that defines the item.

We do **not** create “third products” like “Chaleco Bordado”.
We create:
- `product_visual_definitions` representing the configuration (variants set)
- `inventory_item_definitions` for sizes and inventory
- `product_visual_definition_embroideries` to declare required punch designs

---

## 3) Tables Introduced

## 3.1 `school_variant_links`
**Intent:** Make `schools` participate in the dynamic variant system without duplicating school data.

**Behavior:**
- One `school_id` maps to exactly one `product_variant_id`
- One `product_variant_id` can map to at most one school

**Columns (conceptual)**
- `school_id` (PK, FK -> schools)
- `product_variant_id` (FK -> product_variants, UNIQUE)
- `created_at`

**Constraint/Rule**
- The linked `product_variant_id` **must belong to** `product_variant_system` named `SCHOOL`.
- This is enforced by a DB trigger (`fn_validate_school_variant_system`).

**Why**
- School becomes a “Variant value” and can be included in the variant set for:
  - `product_visual_definitions`
  - `inventory_item_definitions`

---

## 3.2 `product_visual_definition_embroideries`
**Intent:** Declare that a visual configuration requires one or more embroidery designs (ponchados).

**Columns (conceptual)**
- `product_visual_definition_id` (FK -> product_visual_definitions)
- `embroidery_id` (FK -> embroideries)
- `placement` (varchar(50)) — e.g., `PECHO_IZQ`, `ESPALDA`, `MANGA`
- `is_required` (bool, default true)
- `extra_price` (numeric(12,2), nullable)
- `notes` (varchar(200), nullable)
- `created_at`

**Primary key**
- (`product_visual_definition_id`, `embroidery_id`, `placement`)

**Why**
- Supports multiple embroideries for the same item (logo + name + patch)
- Supports consistent placement metadata
- Allows optional extra charge per visual configuration

---

## 4) How This Connects To Inventory

### 4.1 Inventory identity remains `inventory_item_definitions`
- Inventory is tracked by `inventory_item_definition_id` (size-linked).
- Each `inventory_item_definition` belongs to a `product_visual_definition_id` (size ignored) plus `size_id`.

Therefore:
- “CHALECO ROJO BASILIO VADILLO” (visual definition) has sizes:
  - S, M, L → each is an `inventory_item_definition`
- Stock ledger records movements per `inventory_item_definition_id`.

### 4.2 No override-by-size for embroidery (current decision)
We intentionally **do not** create `inventory_item_definition_embroideries`.
Embroidery requirements are managed at the visual level, since designs typically do not change by size.

---

## 5) How This Connects To Pricing

### 5.1 Price scopes (same as project pricing guide)
Effective price is resolved by:
1) ITEM_DEFINITION
2) VISUAL_DEFINITION
3) PRODUCT

Embroidery-related price behavior options:
- If embroidery affects price *uniformly across sizes*:
  - store that price at **VISUAL_DEFINITION** scope
  - or store `extra_price` in `product_visual_definition_embroideries` and add it in pricing calculation
- If price differs by size:
  - store price at **ITEM_DEFINITION** scope

**Recommendation**
- Use **VISUAL_DEFINITION pricing** for embroidered school-based items if price is the same across sizes.
- Use **ITEM_DEFINITION pricing** when size strongly affects cost/price (your project often links price to size).

---

## 6) How This Connects To Images

### 6.1 Images should usually attach to `VISUAL_DEFINITION`
For embroidered items, the photo is typically:
- same across sizes
- different across schools/colors

Therefore:
- attach embroidered item images to `product_image_targets.target_type='VISUAL_DEFINITION'`
- fallback still works:
  - ITEM_DEFINITION → VISUAL_DEFINITION → PRODUCT

---

## 7) Development Guidance (.NET Core)

### 7.1 Get-or-create School Variant
When a school is selected:
1) Resolve `product_variant_id` using `school_variant_links`
2) Ensure that the variant is included in the variant set used to resolve/create:
   - `product_visual_definitions`
   - `inventory_item_definitions`

### 7.2 Create a visual definition for embroidered item
Given:
- `product_id` (CHALECO)
- variant IDs: {ROJO, (SCHOOL) BASILIO_VADILLO, ...}

Steps:
1) Normalize variant IDs (dedupe + sort)
2) Compute `variants_hash`
3) Get-or-create `product_visual_definitions`
4) Insert required embroideries into `product_visual_definition_embroideries`

### 7.3 Validations (recommended)
- Do not allow a school-embroidered configuration without at least one required embroidery record.
- `placement` should be validated against allowed values (enum/table) in application code.

---

## 8) Migration Guidance (Legacy → Destination)

### 8.1 Migrating schools and creating SCHOOL variants
1) Migrate `schools` (by name)
2) Ensure `product_variant_system` contains a system named `SCHOOL`
3) For each school:
   - create a `product_variants` row with:
     - `product_variant_system_id` = SCHOOL system id
     - `variant_value` = school name
   - create `school_variant_links(school_id, product_variant_id)`

### 8.2 Migrating embroideries
- Migrate `embroideries` as assets:
  - binary DST/EMB files (bytea) and metadata
  - ensure `embroidery_id` mapping legacy→dest is tracked

### 8.3 Migrating embroidered products
If legacy indicates “product is embroidered for a school”:
1) include the school variant in the visual definition variant set
2) create `product_visual_definition_embroideries` rows linking:
   - that visual definition
   - the embroidery design(s) for the school
   - placement(s)

---

## 9) Do / Don’t Checklist

### DO
- ✅ Model School as a Variant via `school_variant_links` (system `SCHOOL`)
- ✅ Attach embroidery requirements at `product_visual_definitions` level
- ✅ Use inventory ledger on `inventory_item_definitions`
- ✅ Use images at `VISUAL_DEFINITION` for school-specific embroidered items
- ✅ Keep pricing consistent with scope rules (ITEM_DEF → VISUAL_DEF → PRODUCT)

### DON’T
- ❌ Don’t create separate “products” for embroidered versions
- ❌ Don’t attach embroidery only to `products` if it changes the SKU identity
- ❌ Don’t bypass the SCHOOL variant system (it breaks identity consistency)

---

## 10) Example: “CHALECO ROJO BASILIO VADILLO”

1) Resolve school variant:
- `school_id` = BASILIO VADILLO
- `product_variant_id` from `school_variant_links` (system `SCHOOL`)

2) Build visual definition variant set:
- `ROJO` (color system)
- `BASILIO VADILLO` (SCHOOL system)

3) Get-or-create `product_visual_definitions`:
- `product_id = CHALECO`
- `variants_hash = sha256("ROJO_ID,SCHOOL_BASILIO_ID")`

4) Attach embroidery designs:
- insert into `product_visual_definition_embroideries`:
  - (visual_def_id, embroidery_id, placement='PECHO_IZQ', required=true)

5) Create `inventory_item_definitions` per size:
- S, M, L → each references the same `product_visual_definition_id`

6) Inventory:
- ledger movements track each size as a distinct `inventory_item_definition_id`
