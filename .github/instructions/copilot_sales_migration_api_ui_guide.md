# Copilot Guide - Sales Migration, .NET API, UI, Inventory Ledger, Accounts

> Canonical guide for GitHub Copilot and developers working on the new sales module in the PostgreSQL destination database.
>
> This document explains how to understand and use:
> - `sales`
> - `sales_lines`
> - `sales_types`
> - `sale_statuses`
> - `sale_payments`
> - `sale_line_serial_items`
> - integration with `inventory_*`
> - integration with `accounts`
> - migration from legacy `dbo.Ventas` and `dbo.Venta_Detalles`

This guide must be used for:
- migration scripts
- .NET API controllers/services
- DTO design
- EF Core mappings
- React / frontend UI flows
- inventory integration
- reporting logic

---

## 1) Core architectural decision

Legacy sales removed inventory records when a sale was created.  
That behavior is invalid in the destination system.

### New rule
Sales must never delete inventory rows.

Instead:
- `sales` is the commercial document
- `sales_lines` are the commercial lines
- actual stock effect must be registered through the inventory ledger
- accounts receivable / layaway obligations are handled through `accounts`

So the new system separates:
1. commercial document
2. financial collection
3. inventory movement

---

## 2) Main tables and intended meaning

### 2.1 `sales`
Header of the sale.

Represents:
- who sold
- where it was sold
- to which client (optional)
- what type of sale it is
- current sale status
- sale totals snapshot
- optional link to an account (`account_id`) when the sale is tied to credit / layaway

### 2.2 `sales_lines`
Commercial detail lines of the sale.

Each line may represent:
- a product
- a product visual definition
- a product combo visual definition
- a free-text/manual line
- a service
- an embroidery-related charge

### 2.3 `sales_types`
Catalog of sale business types.

Examples:
- direct sale
- on account
- layaway delivery
- mixed
- legacy migration

### 2.4 `sale_statuses`
Catalog of sale document statuses.

Examples:
- draft
- completed
- canceled
- void

### 2.5 `sale_payments`
Payments directly captured on the sale.

This is for:
- immediate payments
- mixed payments
- partial cash/card/transfer collection at the point of sale

### 2.6 `sale_line_serial_items`
Join table for serialized pieces sold on a specific line.

This is only used when exact serialized inventory items are known and must be traced.

---

## 3) Commercial vs inventory vs financial separation

### 3.1 Sales are commercial documents
A sale stores:
- the commercial header
- the sold lines
- the prices used
- the payment(s) captured

### 3.2 Inventory effect is ledger-based
When a sale is confirmed, inventory must be affected through:
- `inventory_transactions`
- `inventory_transaction_lines`
- optionally serialized inventory linkage

Do not directly reduce a quantity field in a stock table.

### 3.3 Financial debt is account-based
If a customer still owes money:
- create or use `accounts`
- register debt/collections through `account_transactions`

Do not force all sales to behave like accounts receivable.

---

## 4) Sales line types

### 4.1 Supported line semantics
`sales_lines.line_type` is the explicit semantic of the line.

Allowed values:
- `PRODUCT`
- `COMBO`
- `FREE_TEXT`
- `SERVICE`
- `EMBROIDERY`

### 4.2 Meaning of each line type

#### `PRODUCT`
Used when a line represents a real catalog/inventory product.

It may reference:
- `inventory_item_definition_id`
- `product_id`
- `product_visual_definition_id`
- `size_id`

#### `COMBO`
Used when the sold line represents a commercial combo.

It may reference:
- `product_combo_visual_definition_id`

Backend may later explode the combo into inventory components when generating inventory ledger output.

#### `FREE_TEXT`
Used for manual lines not linked to catalog or stock.

Examples:
- custom garment text
- miscellaneous sale
- manual note item
- quick counter sale
- ad-hoc concept

Rules:
- product-related FKs may be null
- `description_snapshot` is mandatory
- no inventory movement should be generated

#### `SERVICE`
Used for service-only lines.

Examples:
- adjustment
- repair
- tailoring
- packaging

No inventory movement unless explicitly modeled otherwise.

#### `EMBROIDERY`
Used for embroidery charge lines when needed commercially as a separate sale line.

It may or may not affect inventory depending on business rules, but usually it is a commercial/service charge.

---

## 5) Free-text / manual sales are valid and first-class

The destination model must support many valid sales where:
- there is no product record
- there is no inventory definition
- the cashier only captures a text description and a price

This is the purpose of `line_type = 'FREE_TEXT'`.

### Copilot rule
Do not force all sale lines to reference a product or inventory record.

The system must allow:
- nullable foreign keys on `sales_lines`
- required `description_snapshot`
- proper totals even without inventory linkage

---

## 6) Snapshot rule

### 6.1 Historical immutability
Sale lines must store snapshots.

Do not reconstruct historical sales from current catalog data.

### 6.2 Required snapshots
At minimum, a sale line should preserve:
- `description_snapshot`
- `unit_price`
- `discount_amount`
- `tax_amount`
- `line_total`

Optional but recommended:
- `size_id`
- chosen product references
- legacy source ids during migration

### 6.3 Why this matters
Prices, names, combos, schools, variants, and commercial descriptions may change later.
Historical sales must remain stable.

---

## 7) Integration with inventory ledger

### 7.1 Sale confirmation rule
When a sale becomes operationally valid (normally `COMPLETED`), the backend must create inventory ledger rows if the sale line affects inventory.

### 7.2 Inventory generation rules
For each sale line:
- if it is `FREE_TEXT` -> no inventory movement
- if it is `SERVICE` -> normally no inventory movement
- if it is inventory-backed -> create negative inventory movement
- if it is serialized -> create exact serialized linkage

### 7.3 Source linkage
`inventory_transaction_lines.sale_line_id` should link the inventory ledger line back to the source sale line.

This is the canonical linkage for traceability.

### 7.4 No destructive stock mutation
Never implement sale confirmation by deleting stock records.
Never recreate the legacy behavior.

---

## 8) Integration with accounts

### 8.1 Optional relation
`sales.account_id` is optional.

This supports:
- direct paid sale without debt
- sale partially sent to credit account
- layaway-related sale
- account settlement-related sale

### 8.2 Recommended patterns

#### direct sale
- create `sales`
- create `sales_lines`
- create `sale_payments`
- generate inventory ledger
- no `accounts` needed

#### sale on account
- create `sales`
- create `sales_lines`
- create or link `accounts`
- create account charge/payment logic through `account_transactions`
- generate inventory ledger if inventory leaves store

#### layaway delivery / conversion
- create `sales`
- link to layaway account if applicable
- register payments as needed
- generate final inventory ledger movement when goods are actually delivered

### 8.3 Avoid duplication
Do not use `sale_payments` and `account_transactions` as interchangeable duplicates.

General interpretation:
- `sale_payments` = money received as part of sale capture
- `account_transactions` = financial ledger of the receivable/payable obligation

---

## 9) Migration from legacy `dbo.Ventas` and `dbo.Venta_Detalles`

### 9.1 Legacy interpretation
Legacy sales:
- stored sale header/detail
- removed inventory on sale
- used a much less normalized detail model

### 9.2 New migration rule
When migrating legacy sales:
1. create `sales`
2. create `sales_lines`
3. map payments if they exist or can be inferred
4. do not recreate legacy deletion behavior
5. if historical inventory movement needs traceability, create corresponding inventory ledger entries only when migration strategy requires it

### 9.3 Legacy identifiers
Preserve old IDs where useful:
- `sales.legacy_sale_id`
- `sales_lines.legacy_sale_detail_id`

These fields are for migration traceability only.

### 9.4 Legacy data quality
Legacy data may contain:
- incomplete client linkage
- details without exact product linkage
- manual descriptions
- historic items no longer in catalog

The destination model must accept these scenarios using:
- nullable FKs
- snapshot descriptions
- free-text line support

---

## 10) Recommended .NET entity interpretation

### 10.1 Main entities
Copilot should generate entities roughly like:

- `Sale`
- `SaleType`
- `SaleStatus`
- `SaleLine`
- `SalePayment`
- `SaleLineSerialItem`
- `PaymentMethod`

### 10.2 Navigation expectations
`Sale` should have:
- many `SaleLines`
- many `SalePayments`
- optional `Account`
- optional `Client`
- required `Site`
- required `Employee`
- required `SaleType`
- required `SaleStatus`

`SaleLine` should have optional references to:
- `Product`
- `ProductVisualDefinition`
- `ProductComboVisualDefinition`
- `InventoryItemDefinition`
- `Size`

### 10.3 Foreign key type alignment
Copilot must always align FK CLR types to principal key CLR types exactly.

Examples:
- `sales.sale_id` -> `long`
- `sales_lines.sale_id` -> `long`
- `sales_lines.product_visual_definition_id` -> `long?` if principal key is `bigint`
- `sales_lines.product_combo_visual_definition_id` -> `long?` if principal key is `bigint`
- `sales_lines.inventory_item_definition_id` -> `long?` if principal key is `bigint`

Never generate mismatched `int?`/`long?` combinations.

---

## 11) API design rules

### 11.1 Recommended controllers
Suggested controllers:
- `SalesController`
- `SalePaymentsController` (optional if separated)
- catalog endpoints for statuses/types/payment methods

### 11.2 Recommended endpoints

#### sales
- `GET /api/sales`
- `GET /api/sales/{id}`
- `POST /api/sales`
- `PUT /api/sales/{id}`
- `PATCH /api/sales/{id}/status`
- `POST /api/sales/{id}/confirm`
- `POST /api/sales/{id}/cancel`

#### payments
- `GET /api/sales/{id}/payments`
- `POST /api/sales/{id}/payments`

#### catalogs
- `GET /api/sales/catalogs/types`
- `GET /api/sales/catalogs/statuses`
- `GET /api/sales/catalogs/payment-methods`

### 11.3 Confirm endpoint behavior
`POST /api/sales/{id}/confirm` should:
1. validate sale status
2. validate lines
3. validate serialized assignment if required
4. persist or ensure payment rows if applicable
5. generate inventory ledger output for inventory-backed lines
6. finalize commercial state

### 11.4 Cancel endpoint behavior
Canceling a sale must not delete rows.

Recommended behavior:
- change status to `CANCELED`
- if inventory was already moved, generate compensating inventory ledger entries
- if account entries were generated, create compensating financial logic according to business rules

---

## 12) Service layer rules

### 12.1 Controllers must stay thin
Controllers should not contain complex inventory/accounting logic.

Use service classes such as:
- `SaleService`
- `SaleConfirmationService`
- `SalePricingService`
- `SaleInventoryService`
- `SaleAccountIntegrationService`

### 12.2 Sale creation flow
Recommended flow:
1. validate DTO
2. resolve catalogs and references
3. create sale header
4. create lines
5. create payment rows if included
6. optionally return calculated totals
7. do not generate inventory ledger until confirmation, unless your business flow explicitly confirms immediately

### 12.3 Pricing validation
Backend must validate:
- quantities
- unit prices
- discounts
- totals

Do not trust UI totals blindly.

---

## 13) DTO guidance

### 13.1 Sale list DTO
Should include:
- `saleId`
- `saleDate`
- `folio`
- `siteName`
- `clientName`
- `employeeName`
- `saleTypeCode`
- `saleStatusCode`
- `subtotal`
- `discountTotal`
- `taxTotal`
- `total`

### 13.2 Sale detail DTO
Should include:
- header fields
- line collection
- payments collection
- account relation summary if any
- serial item references if any

### 13.3 Create sale DTO
Should support:
- header data
- line array
- optional payment array
- optional account linkage
- optional legacy ids in migration scenarios

### 13.4 Create sale line DTO
Should support nullable references plus snapshot:
- `lineType`
- `productId`
- `productVisualDefinitionId`
- `productComboVisualDefinitionId`
- `inventoryItemDefinitionId`
- `sizeId`
- `descriptionSnapshot`
- `quantity`
- `unitPrice`
- `discountAmount`
- `taxAmount`
- `lineTotal`

### 13.5 Create payment DTO
Should include:
- `paymentMethodId`
- `paymentDate`
- `amount`
- `reference`
- `comments`

---

## 14) UI design rules

### 14.1 Main sale screen
The sale screen should support:
- selecting client (optional)
- selecting site / seller
- adding line items
- mixing catalog lines with free-text lines
- capturing partial or multiple payments
- showing totals clearly

### 14.2 Line editor behavior
The line editor must allow:
- product/catalog selection
- combo selection
- manual text line entry
- service line entry
- optional serialized assignment if needed

### 14.3 Free-text line UX
There must be an easy UI path for:
- manual description
- quantity
- unit price
- discount
- total

Do not force the user to create a product first.

### 14.4 Serialized item UX
When a line affects serialized inventory:
- UI should request the exact serialized item(s)
- selected serials should be stored through `sale_line_serial_items`

### 14.5 Confirm vs save draft
UI should distinguish:
- saving draft
- confirming sale

Only confirmation should normally trigger inventory ledger creation.

### 14.6 Cancellation UX
Cancellation should:
- show reason
- warn about compensating inventory/account actions
- never imply physical deletion

---

## 15) Reporting rules

### 15.1 Sales reports
Reports should use `sales` + `sales_lines`.

### 15.2 Inventory reports
Inventory reports should use inventory ledger tables, not sales lines directly as stock truth.

### 15.3 Financial collection reports
Collection / debt reports should use `accounts` and `account_transactions`, not only `sale_payments`.

### 15.4 Manual line reporting
Reports must include free-text/manual lines.
Do not assume every sold line belongs to catalog.

---

## 16) Important validations

### 16.1 Header validations
Validate:
- site exists
- employee exists
- sale type exists
- sale status exists
- client exists if provided
- account exists if provided

### 16.2 Line validations
Validate:
- `quantity > 0`
- `unit_price >= 0`
- `discount_amount >= 0`
- `tax_amount >= 0`
- `line_total >= 0`
- `description_snapshot` is required

### 16.3 Free-text line validation
If line type is `FREE_TEXT`, then:
- description is mandatory
- all product FKs may be null
- no inventory movement should be generated

### 16.4 Serialized validation
If a line requires serialized items:
- assigned serialized count must match quantity
- serialized items must belong to valid inventory state
- serialized items must not already be sold/reserved improperly

---

## 17) Things Copilot must NOT do

- Do not recreate legacy behavior of deleting stock on sale
- Do not force all lines to have a product FK
- Do not calculate historical lines from current catalog data
- Do not treat `sale_payments` as the same thing as account ledger
- Do not generate mismatched foreign key CLR types
- Do not hard-delete sales during cancellation flows
- Do not bypass inventory ledger when inventory-backed lines are confirmed

---

## 18) Canonical interpretation

The new sales module must be treated as:

- normalized
- compatible with inventory ledger
- compatible with accounts receivable / layaway
- capable of handling serialized and non-serialized sales
- capable of handling manual/free-text sales
- historically stable through snapshots
- suitable for migration from legacy data

This is the required interpretation for all generated code related to sales.

---
