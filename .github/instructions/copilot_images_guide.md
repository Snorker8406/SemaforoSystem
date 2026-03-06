# Product Images Development Guide (VS Code Copilot + .NET Core)

> Purpose: This document explains **how to store, relate, and query product images** with support for:
> - Images at **product level** (generic)
> - Images at **configuration level** (specific to `inventory_item_definitions`, i.e., product + size + dynamic variants)
> - Binary storage in PostgreSQL via **BYTEA**, with optional future migration to object storage (`storage_key`/`url`)
>
> Copy this file into your repo (recommended path): `.github/copilot-instructions.images.md`  
> Or merge it into your main Copilot instructions file.

---

## 1) Design Summary

### 1.1 Why we don’t link images directly to variants
Products can have **dynamic combinations of N variants** (from different variant systems).  
A single variant value (e.g., `TINTO`) is **not enough** to uniquely identify the correct photo when multiple variant systems exist (e.g., `Color=TINTO` + `Model=LOYOLA`).

Therefore, images attach to either:
- a **Product** (applies to all configurations), or
- an **Inventory Item Definition** (`inventory_item_definitions`) which represents a stable configuration:  
  `product_id + optional size_id + set of product_variant_id`

---

## 2) Tables & Meaning

### 2.1 `product_images`
Stores the actual image binary + metadata.

**Core columns**
- `product_image_id` (bigserial, PK)
- `image_bytes` (bytea, NOT NULL) — binary image data
- `content_type` (varchar(100), NOT NULL) — `image/jpeg`, `image/png`, `image/webp`
- `image_role` (varchar(20), NOT NULL, default `ORIGINAL`) — `ORIGINAL` | `THUMB` | `DETAIL` (extensible)
- `width_px` (int, nullable)
- `height_px` (int, nullable)
- `file_size_bytes` (int, nullable)
- `sha256` (char(64), nullable) — optional dedup
- `file_name` (varchar(255), nullable)
- `alt_text` (varchar(200), nullable)
- `created_at` (timestamp, default now)

**Future-ready (optional)**
- `storage_key` (varchar(300), nullable)
- `url` (varchar(500), nullable)

> Guideline: Even if we store bytes in DB now, keep `storage_key` available for a later migration to S3/MinIO/GCS/CDN.

---

### 2.2 `product_image_targets`
Links an image to **exactly one** target type:
- `PRODUCT` (generic image for a product)
- `ITEM_DEFINITION` (image for a specific configuration)

**Core columns**
- `product_image_target_id` (bigserial, PK)
- `product_image_id` (FK -> product_images, ON DELETE CASCADE)
- `target_type` (varchar(30), NOT NULL) — `PRODUCT` | `ITEM_DEFINITION`
- `product_id` (int, nullable FK -> products)
- `inventory_item_definition_id` (int, nullable FK -> inventory_item_definitions)
- `sort_order` (int, default 0)
- `is_primary` (bool, default false)
- `created_at` (timestamp, default now)

**Constraint**
- If `target_type='PRODUCT'`: `product_id NOT NULL` and `inventory_item_definition_id IS NULL`
- If `target_type='ITEM_DEFINITION'`: `inventory_item_definition_id NOT NULL` and `product_id IS NULL`

**Uniqueness expectations**
- Prevent duplicate links per target:
  - same `product_image_id` cannot be linked twice to the same `product_id`
  - same `product_image_id` cannot be linked twice to the same `inventory_item_definition_id`
- Only one primary image per target:
  - one `is_primary=true` per product target
  - one `is_primary=true` per item_definition target

---

## 3) Roles & UI Consumption

### 3.1 `image_role` strategy
Use roles to avoid loading full images where thumbnails are needed:

- `THUMB`: small (e.g., 300–500px) for tables/grids
- `ORIGINAL`: optimized full image (e.g., max width 1600px)
- `DETAIL`: optional extra (zoom, close-ups, etc.)

**Rule of thumb**
- Catalog/list UI requests `THUMB` only
- Product detail UI requests `ORIGINAL` and/or `DETAIL`

---

## 4) Retrieval Rules (Important)

### 4.1 When you have `inventory_item_definition_id` (selected configuration)
Always fetch images with this fallback order:

1) Configuration-specific images:
   - targets where `target_type='ITEM_DEFINITION'` and `inventory_item_definition_id = X`
2) If none found → fallback to product-level images:
   - targets where `target_type='PRODUCT'` and `product_id = item_definition.product_id`

**Primary image preference**
- Prefer `is_primary=true` within the chosen scope
- Then order by `sort_order`

### 4.2 When you have only `product_id` (no configuration selected)
Fetch product-level images:
- `target_type='PRODUCT' AND product_id = X`
Order by:
- `is_primary DESC`, then `sort_order ASC`

---

## 5) Recommended API Patterns (.NET Core)

### 5.1 Upload flow (binary in DB)
**Inputs**
- file bytes
- `content_type`, optional `file_name`, optional `alt_text`
- one of:
  - target: `{ target_type: "PRODUCT", product_id }`
  - target: `{ target_type: "ITEM_DEFINITION", inventory_item_definition_id }`
- optional: `image_role` (default `ORIGINAL`), `sort_order`, `is_primary`

**Steps**
1) Insert row in `product_images` with bytes + metadata
2) Insert row in `product_image_targets` to attach it to the correct scope
3) If `is_primary=true`, enforce uniqueness (database constraint + app handling)

### 5.2 Delete flow
Deleting an image should:
- delete `product_images` (cascade deletes targets), OR
- delete target only (detach), leaving the image if it is shared across targets

**Guideline**
- If the system allows one image shared by many targets, support “detach” separately:
  - `DELETE /targets/{id}`
  - `DELETE /images/{id}` for full removal

### 5.3 Endpoint suggestions
- `POST   /api/images` (upload image bytes + metadata + target)
- `DELETE /api/images/{productImageId}` (delete image and its targets)
- `DELETE /api/images/targets/{productImageTargetId}` (detach)
- `GET    /api/products/{productId}/images?role=THUMB`
- `GET    /api/item-definitions/{id}/images?role=THUMB` (with fallback)

---

## 6) Query Guidelines (EF Core)

### 6.1 Avoid loading `image_bytes` unless needed
For list pages, return metadata and a lightweight representation:
- If you must deliver bytes, do it from a dedicated endpoint:
  - `GET /api/images/{id}/content`
- In EF, project without selecting `image_bytes` for list endpoints.

**DTO suggestion**
- `ProductImageDto { id, role, contentType, width, height, isPrimary, sortOrder }`

### 6.2 Example logic (pseudocode) for item_definition images with fallback
1) Query targets for `ITEM_DEFINITION`
2) If none:
   - fetch product_id from `inventory_item_definitions`
   - query targets for `PRODUCT`

**Sorting**
- `OrderByDescending(t => t.IsPrimary).ThenBy(t => t.SortOrder)`

---

## 7) Performance & Storage Guidance

### 7.1 Volume expectations
~3,000 images total is feasible in Postgres `bytea`, especially if optimized.

### 7.2 Best practices
- Store compressed images (WebP/JPEG) and cap resolution (e.g., 1600px)
- Generate thumbnails (`THUMB`) for catalog usage
- Use `sha256` (optional) to deduplicate uploads
- Consider later migration to object storage; keep `storage_key` fields.

---

## 8) Consistency Rules (Do/Don’t)

### DO
- ✅ Attach configuration-specific images to `inventory_item_definitions`
- ✅ Use `PRODUCT` images as fallback when configuration images are missing
- ✅ Maintain exactly one primary image per target
- ✅ Use `image_role=THUMB` for list screens

### DON’T
- ❌ Don’t link images to a single variant value for multi-system combinations
- ❌ Don’t return `image_bytes` in bulk list endpoints
- ❌ Don’t allow multiple primaries for the same target scope

---

## 9) Example: "CHALECO TINTO LOYOLA"

- `products`: CHALECO (`product_id = P1`)
- `inventory_item_definitions`: configuration (`inventory_item_definition_id = D1`)
  - points to `product_id=P1`
  - includes variants: `TINTO` (Color system) + `LOYOLA` (other system)

Images:
- Generic CHALECO photo → `product_image_targets.target_type='PRODUCT', product_id=P1`
- Specific TINTO+LOYOLA photo → `product_image_targets.target_type='ITEM_DEFINITION', inventory_item_definition_id=D1`

Retrieval:
- UI selected D1 → return D1 images, else fallback to P1 images.

---
