# Product Combos Guide (VS Code Copilot + .NET Core)

> Purpose: This document defines how **Product Combos** work in the destination PostgreSQL model.
> Combos are **sellable groupings** of items (products / product visual definitions / embroideries) and **do not have inventory**.
> They support:
> - Multiple components with quantities
> - Optional combo-level images
> - Optional combo-level fixed price or discount
> - Price fallback to “sum of component prices” when combo pricing is empty
>
> Recommended repo path: `.github/copilot-instructions.combos.md`

---

## 1) Conceptual Model (What a Combo is)

### 1.1 Combos are NOT inventory items
Combos are not tracked in inventory and never create `inventory_item_definitions` or ledger movements.

- ✅ You can sell a combo
- ✅ You can compute a combo price
- ✅ You can show combo images
- ❌ You cannot “stock” a combo as a single unit

Inventory remains at the component level (`inventory_item_definitions`, ledger).

### 1.2 Combo behaves like a “virtual product”
A `product_combo` is like `products`, but virtual:
- represents a named offering/group
- can have multiple “combo visual definitions”
- does not appear in inventory as a stockable item

---

## 2) Tables & Meaning

## 2.1 `product_combos`
Top-level combo definition.

**Columns (conceptual)**
- `product_combo_id` (PK)
- `name` (varchar)
- `description` (varchar/text)
- `is_active` (bool)
- timestamps

**Meaning**
- A “container” for one or more offerings/versions of a combo.

---

## 2.2 `product_combo_visual_definitions`
Equivalent to `product_visual_definitions` but for combos.

**Columns (conceptual)**
- `product_combo_visual_definition_id` (PK)
- `product_combo_id` (FK -> product_combos)
- `name`, `description`
- `sort_order`, `is_active`

### Pricing fields (combo-level)
- `fixed_price_amount` (numeric, nullable)
- `discount_type` (`NONE` | `FIXED` | `PERCENT`)
- `discount_value` (numeric, nullable)
- `price_list_id` (FK -> price_lists, nullable)

**Meaning**
- Each combo can have multiple “versions” (e.g., different variants, different bundles).
- Pricing may be overridden here, otherwise computed from components.

---

## 2.3 `product_combo_components`
Defines the component items inside a `product_combo_visual_definition`.

**Columns (conceptual)**
- `product_combo_component_id` (PK)
- `product_combo_visual_definition_id` (FK)
- `component_type` (`PRODUCT` | `PRODUCT_VISUAL_DEFINITION` | `EMBROIDERY`)
- One-of target FKs:
  - `product_id` (FK -> products) if `PRODUCT`
  - `product_visual_definition_id` (FK -> product_visual_definitions) if `PRODUCT_VISUAL_DEFINITION`
  - `embroidery_id` (FK -> embroideries) if `EMBROIDERY`
- `quantity` (int > 0)
- `sort_order` (int)

### Embroidery-specific columns
Only when `component_type='EMBROIDERY'`:
- `placement` (varchar(50), required)
- `is_required` (bool, default true)
- `extra_price` (numeric, nullable)

**Meaning**
- A combo can include:
  - a base product reference (generic)
  - a specific product visual definition (e.g., “Chamarra ROJA LOYOLA”)
  - one or multiple embroidery designs (ponchados)

---

## 2.4 `product_combo_images` and `product_combo_image_targets`
Combo-specific images stored as binary in DB.

### `product_combo_images`
Stores bytes + metadata:
- `image_bytes` (bytea)
- `content_type`
- `image_role` (`ORIGINAL` | `THUMB` | `DETAIL`)
- optional dimensions/hash

### `product_combo_image_targets`
Attaches images to a combo visual definition:
- `product_combo_visual_definition_id`
- `product_combo_image_id`
- `sort_order`
- `is_primary`

---

## 3) Pricing Rules (Most Important)

### 3.1 Effective combo price: resolution order
Given a `product_combo_visual_definition_id` and a `price_list_id` (or default list):

1) If `fixed_price_amount` is not null:
   - Effective combo price = `fixed_price_amount`

2) Else compute `sum_components`:
   - sum each component’s price × quantity
   - add embroidery extra if configured (see 3.3)
   - then apply discount if configured:
     - if `discount_type='FIXED'`: `final = max(0, sum_components - discount_value)`
     - if `discount_type='PERCENT'`: `final = max(0, sum_components * (1 - discount_value/100))`
     - if `discount_type='NONE'`: `final = sum_components`

### 3.2 How to price each component type
#### A) PRODUCT_VISUAL_DEFINITION
- Pricing generally requires a chosen size.
- UI/backend must resolve an `inventory_item_definition_id` for the selected size:
  - Use `inventory_item_definitions` where:
    - `product_visual_definition_id = component.product_visual_definition_id`
    - `size_id = selected size`
- Then resolve price via the standard pricing engine:
  - `get_effective_price(price_list_id, inventory_item_definition_id)`

#### B) PRODUCT
- If product has sizes/variants, `PRODUCT` alone is not enough to price precisely.
- Prefer using `PRODUCT_VISUAL_DEFINITION` components whenever size/variants matter.
- If you do use `PRODUCT`:
  - use product-level price fallback (`price_product_entries`)
  - OR require the UI to pick a configuration (best practice)

#### C) EMBROIDERY
Two options:
- If `extra_price` is set on the component:
  - use that as the embroidery charge
- else fallback to the embroidery’s own price:
  - `embroideries.price` (nullable; treat null as 0)

**Quantity**
- Multiply embroidery charge by component quantity if needed.

### 3.3 Recommended “Embroidery pricing” behavior
- Use `extra_price` for combo-specific override (nullable).
- If `extra_price` is null, use `embroideries.price`.
- If `embroideries.price` is null, treat as 0.

---

## 4) UI Guidance

### 4.1 How users select sizes
Because combos can include size-linked items, the UI should:
- either force a single size selection applied to all apparel components
- or allow selecting size per component (more complex)

**Recommendation**
- Start simple:
  - a single “selected size” used to resolve item_definition for every `PRODUCT_VISUAL_DEFINITION` component
- Later, if needed:
  - per-component size selection

### 4.2 Display strategy
- Show combo as one sellable item with:
  - a primary THUMB image (from combo images)
  - computed price (fixed/discount/sum)
- Show components as a breakdown in the detail view

---

## 5) API Patterns (Recommended)

### Read endpoints

### Additional school-scoped endpoints (optional)
- `GET /api/combos/by-school/{schoolId}` (list combo visual definitions for a school)
- `POST /api/combos/visual-definitions/{id}/schools` (assign schools)
- `DELETE /api/combos/visual-definitions/{id}/schools/{schoolId}` (remove school)


- `GET /api/combos` (list combos + primary THUMB image + computed price preview)
- `GET /api/combos/{comboId}` (combo detail + visual defs + components)
- `GET /api/combos/visual-definitions/{id}/price?priceListId=..&sizeId=..` (computed effective price)

### Write endpoints
- `POST /api/combos` (create combo)
- `POST /api/combos/{comboId}/visual-definitions`
- `POST /api/combos/visual-definitions/{id}/components`
- `POST /api/combos/visual-definitions/{id}/images`

---

## 6) Controller/Service Implementation Notes (.NET Core)

### 6.1 Always compute price in a service layer
Controllers should be thin. Use:
- `ComboPricingService.ComputeEffectiveComboPriceAsync(...)`

### 6.2 Price computation must be deterministic
Given:
- combo_visual_definition_id
- price_list_id
- selected size(s)
The computed price must be reproducible and saved into the sale line item at checkout.

### 6.3 Store the final price on sale
Even though prices can change later, the sale must store:
- unit price used for the combo
- optional breakdown for audit (optional)

---

## 7) Do / Don’t Checklist

### DO
- ✅ Use `PRODUCT_VISUAL_DEFINITION` components for size/variant-sensitive items
- ✅ Keep combos inventory-free
- ✅ Provide combo images at combo-visual level
- ✅ Use fixed price or discount fields only as overrides
- ✅ Save final computed price to the sale record

### DON’T
- ❌ Don’t create inventory movements for combos
- ❌ Don’t rely on `PRODUCT` components for items that require size selection (unless you accept imprecision)
- ❌ Don’t return image bytes in list endpoints; use dedicated content endpoint if needed

---

## 8) Example

Combo: “Conjunto Deportivo + Bordado”
- Components:
  - `PRODUCT_VISUAL_DEFINITION`: Chamarra deportiva
  - `PRODUCT_VISUAL_DEFINITION`: Pantalón deportivo
  - `EMBROIDERY`: Logo (placement='PECHO_IZQ')

Pricing:
- If `fixed_price_amount` is set: use it
- Else:
  - sum prices of chamara + pants at selected size
  - add embroidery charge (extra_price or embroidery.price)
  - apply discount if configured


---

## 2.5 `product_combo_visual_definition_schools` (School targeting for combos)

**Intent:** Allow combo visual definitions to be **scoped/filtered by School**, similar to how `product_visual_definitions` relate to schools.

This is useful when:
- A combo is an “official package” for a school (e.g., uniform set for **BASILIO VADILLO**)
- You want fast UI filtering: “show me combos for School X”
- You want explicit governance (not only derived from components)

**Shape (conceptual)**
- PK: (`product_combo_visual_definition_id`, `school_id`)
- Columns:
  - `product_combo_visual_definition_id` (FK → `product_combo_visual_definitions`)
  - `school_id` (FK → `schools`)
  - `is_active` (bool, default true)
  - `created_at` (timestamp)

**Behavior**
- Many-to-many: one combo visual definition can target many schools; one school can have many combos.
- If a combo already includes school-specific items (e.g., components that include a SCHOOL variant), you may still use this table for **explicit tagging** and faster queries.



---

## 4.3 School filtering in UI

If the UI needs “Combos for a school”:
- Query combos via `product_combo_visual_definition_schools` with `school_id = X`
- Then load:
  - combo visual definition details
  - components
  - primary THUMB image
  - computed price (fixed/discount/sum)

**Note:** You can also *derive* a school from components (by inspecting SCHOOL variants or `embroideries.school_id`), but that is slower and less explicit. Prefer the explicit relationship when you need fast filtering.

