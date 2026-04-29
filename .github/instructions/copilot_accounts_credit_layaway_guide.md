---
applyTo: "**/*.{cs,sql,ts,tsx}"
---

# Accounts / Credit / Layaway Guide (.NET Core / API / UI / Migration / Copilot)

> Purpose: This document explains how GitHub Copilot and developers must understand and use the new account schema in PostgreSQL for:
> - customer credit accounts
> - layaway / apartados
> - account items
> - financial transactions
> - installments
> - account-related files
> - migration from legacy `dbo.Cuentas_Cobrar`

This guide is the canonical interpretation for backend, frontend, migration scripts, and DTOs.

## Model Alignment (Current Codebase)

Use the current entity model in `SemaforoSystem.Server/Models` as source of truth:
- `accounts.account_id` -> PostgreSQL `bigint` -> C# `long` (`Account.AccountId`)
- `account_items.account_id` -> `long`
- `account_transactions.account_id` -> `long`
- `account_installments.account_id` -> `long`
- `credit_accounts.account_id` -> `long`
- `layaway_accounts.account_id` -> `long`
- `files.account_id` -> `long?`
- `accounts.opened_by_employee_id` is the canonical FK for the opening employee

Legacy/removed scope for new code:
- Do not generate or depend on `account_payments`; use `account_transactions` as ledger source for payments.

---

## 1) Core business meaning

The old legacy table `dbo.Cuentas_Cobrar` mixed too many responsibilities in one place:
- account header
- client snapshot data
- financial balance
- layaway operational dates
- payments / deposits
- due logic
- ready/delivery state

In the new schema, these concerns are separated.

### New model
The destination schema now uses:
- `account_types`
- `account_statuses`
- `accounts`
- `account_items`
- `account_transactions`
- `account_installments`
- `credit_accounts`
- `layaway_accounts`
- `account_item_serial_items`

This design is normalized and should replace the previous simplistic `accounts/account_*` interpretation.

---

## 2) Main tables and responsibilities

### 2.1 `accounts`
This is the header/master record of the commercial obligation.

Represents:
- one customer account
- one account type
- one current account status
- one originating site
- one employee who opened it

Important:
- `accounts` is not the place to persist derived balances
- do not store redundant customer name/phone snapshots here unless explicitly required later

### 2.2 `account_types`
Catalog of account business types.

Current intended values:
- `CREDIT`
- `LAYAWAY`

### 2.3 `account_statuses`
Catalog of generic account statuses.

Current intended values:
- `OPEN`
- `PAID`
- `CANCELED`
- `OVERDUE`
- `CLOSED`

## 2.4 `account_items`
Commercial lines of the account:
- garments
- products
- definitions
- reserved pieces
- item snapshots with price and quantity

This is what the customer is paying for.

### Foreign key type alignment rule
All foreign keys in `account_items` must use the exact same database and .NET type as the referenced primary key.

Important current case:
- `account_items.product_visual_definition_id` must match `product_visual_definitions.product_visual_definition_id`
- in the current destination schema that means:
  - PostgreSQL: `bigint`
  - .NET / EF Core: `long?`

Copilot must **not** generate `int? ProductVisualDefinitionId` if the principal key is `long`.

General rule:
- PostgreSQL `integer` -> C# `int`
- PostgreSQL `bigint` -> C# `long`

This rule applies to all `accounts` and `account_*` foreign keys.

### 2.5 `account_transactions`
Financial ledger of the account:
- charges
- payments
- discounts
- interest
- adjustments
- cancellations

This is the financial truth source.

### 2.6 `account_installments`
Expected payment schedule for credit accounts.

Used when the business wants:
- weekly payments
- monthly payments
- installment plan visibility

### 2.7 `credit_accounts`
Type-specific extension for credit accounts.

### 2.8 `layaway_accounts`
Type-specific extension for layaway / apartados.

### 2.9 `account_item_serial_items`
Used only when a specific serialized inventory piece is reserved under an account item.

---

## 3) Accounting convention

### 3.1 Financial source of truth
The canonical balance must come from `account_transactions`.

### 3.2 Recommended sign convention
Copilot-generated code must assume:

- `CHARGE` = positive amount
- `PAYMENT` = negative amount
- `DISCOUNT` = negative amount
- `INTEREST` = positive amount
- `ADJUSTMENT` = positive or negative
- `CANCELLATION` = usually compensating negative entry or reversal logic

### 3.3 Balance formula
Balance is:

`SUM(account_transactions.amount)`

Interpretation:
- `balance > 0` => customer still owes money
- `balance = 0` => account is settled
- `balance < 0` => overpayment or inconsistent state that must be reviewed

### 3.4 Do not persist redundant balance columns
Do not add columns like:
- `remaining_balance`
- `paid_total`
- `pending_amount`

unless they are explicit cache fields with controlled regeneration.
The default rule is: compute from transactions.

---

## 4) Item total convention

### 4.1 Commercial total
The commercial total of the account comes from:

`SUM(account_items.line_total)`

### 4.2 Snapshot requirement
`account_items.description_snapshot`, `unit_price`, `discount_amount`, and `line_total` are historical snapshots.

Do not recalculate historical lines from live product price tables after the account is created.

---

## 5) Credit vs Layaway

### 5.1 Credit account
A credit account is a customer debt with optional schedule.

Typical structure:
- `accounts` header
- `account_items`
- one initial `CHARGE`
- many `PAYMENT`s
- optional `account_installments`
- optional row in `credit_accounts`

### 5.2 Layaway account
A layaway account is a reservation of garments/articles while the customer is paying in parts.

Typical structure:
- `accounts` header
- `account_items`
- one row in `layaway_accounts`
- one initial `CHARGE`
- one or more `PAYMENT`s

### 5.3 Layaway operational fields
Layaway operational state belongs in `layaway_accounts`, not in `accounts`.

Examples:
- expected arrival date
- ready date
- delivery date
- whether it is ready
- expiration date

---

## 6) Migration rules from legacy `dbo.Cuentas_Cobrar`

### 6.0 Legacy source databases (SEMAFORO + SEMAFOROX)
The legacy data for accounts is split across **two SQL Server databases with identical schema**:
- `SEMAFORO` — current / most recent data
- `SEMAFOROX` — historical data

Both databases contain the same legacy tables (`dbo.Cuentas_Cobrar`, `dbo.Abonos`, etc.). To obtain the full legacy dataset for migration, queries MUST read from both databases via `UNION ALL`.

Important schema difference:
- Column `dbo.Cuentas_Cobrar.Congelada` exists **only in `SEMAFORO`**, NOT in `SEMAFOROX`.
- When building the UNION, project `CAST(NULL AS bit) AS Congelada` (or an equivalent constant) on the `SEMAFOROX` side to keep column lists aligned.

Rules for Copilot-generated migration code / SQL:
- Always read legacy accounts as the UNION of `[SEMAFORO].[dbo].[Cuentas_Cobrar]` and `[SEMAFOROX].[dbo].[Cuentas_Cobrar]`.
- Always read legacy payments as the UNION of `[SEMAFORO].[dbo].[Abonos]` and `[SEMAFOROX].[dbo].[Abonos]`.
- Prefer `UNION ALL` over `UNION` to avoid losing rows and to preserve performance (duplicates across databases are not expected; if they occur, they must be handled explicitly).
- Add a `source_db` discriminator column (`'SEMAFORO'` / `'SEMAFOROX'`) to preserve provenance and to disambiguate potential `ID` collisions across databases.
- Never assume the legacy `ID` is globally unique across both databases; when generating new PostgreSQL keys, keep `(source_db, legacy_id)` as the natural key for idempotent migration.

Reference query template for `Cuentas_Cobrar` (aligned columns, `Congelada` projected as NULL from SEMAFOROX):

```sql
SELECT
    'SEMAFORO' AS source_db,
    [ID], [Cliente_ID], [Sucursal_Apertura_ID],
    [Fecha_Apertura], [Fecha_Liquidacion], [Fecha_Vencimiento],
    [Fecha_Cancelacion], [Fecha_Reactivacion],
    [Prorroga], [Dias_Credito], [Credito], [Apartado],
    [Incompleto], [Pedido_Especial], [Total], [Saldo],
    [Empleado_Apertura_ID], [Observaciones], [Apartado_Codigo_Barras],
    [Escuela_ID], [Concepto_ID], [Bordar],
    [Congelada]
FROM [SEMAFORO].[dbo].[Cuentas_Cobrar]
UNION ALL
SELECT
    'SEMAFOROX' AS source_db,
    [ID], [Cliente_ID], [Sucursal_Apertura_ID],
    [Fecha_Apertura], [Fecha_Liquidacion], [Fecha_Vencimiento],
    [Fecha_Cancelacion], [Fecha_Reactivacion],
    [Prorroga], [Dias_Credito], [Credito], [Apartado],
    [Incompleto], [Pedido_Especial], [Total], [Saldo],
    [Empleado_Apertura_ID], [Observaciones], [Apartado_Codigo_Barras],
    [Escuela_ID], [Concepto_ID], [Bordar],
    CAST(NULL AS bit) AS [Congelada]
FROM [SEMAFOROX].[dbo].[Cuentas_Cobrar];
```

The same UNION ALL pattern applies to `dbo.Abonos` (legacy payments) and to any other legacy table split between `SEMAFORO` and `SEMAFOROX`.

### 6.1 Legacy table interpretation
The legacy table mixed:
- account header
- financial status
- deposit info
- ready/delivery dates
- due logic
- client presentation data

### 6.2 Migration strategy
When migrating from legacy:
1. create `accounts`
2. create `account_items` if item detail exists or can be reconstructed
3. create one initial `CHARGE`
4. create payment transactions from historical payments if available
5. create `credit_accounts` or `layaway_accounts` according to the business meaning

### 6.3 Legacy layaway mapping
Legacy fields such as:
- deposit / anticipo
- ready / listo
- arrival date / fecha llegada
- delivery date / fecha entrega

must map into:
- `layaway_accounts`
- and/or account transactions if they represent actual money movement

### 6.4 Legacy balance fields
Legacy stored balance-like values must not be copied as authoritative final truth if transaction history exists.

Preferred order of trust:
1. account transactions
2. derived balance
3. legacy summary fields only as fallback for incomplete migration

---

## 7) API design rules

### 7.1 Main account endpoints
Recommended endpoints:
- `GET /api/accounts`
- `GET /api/accounts/{id}`
- `POST /api/accounts`
- `PUT /api/accounts/{id}`
- `PATCH /api/accounts/{id}/status`
- `GET /api/accounts/{id}/transactions`
- `POST /api/accounts/{id}/transactions`
- `GET /api/accounts/{id}/items`
- `POST /api/accounts/{id}/items`

### 7.2 Credit-specific endpoints
Optional:
- `GET /api/accounts/{id}/installments`
- `POST /api/accounts/{id}/installments`
- `PATCH /api/accounts/{id}/installments/{installmentId}`

### 7.3 Layaway-specific endpoints
Optional:
- `PATCH /api/accounts/{id}/layaway/ready`
- `PATCH /api/accounts/{id}/layaway/deliver`
- `PATCH /api/accounts/{id}/layaway/arrival`

### 7.4 File attachment endpoints
If files are linked to accounts:
- `GET /api/accounts/{id}/files`
- `POST /api/accounts/{id}/files`

The `files.account_id` relation must remain aligned with the new `accounts.account_id` type.

---

## 8) Backend behavior rules

### 8.1 Create account flow
When creating an account:
1. validate client
2. validate site
3. validate opening employee
4. validate account type
5. create `accounts`
6. create `account_items`
7. create type-specific row if needed:
   - `credit_accounts`
   - `layaway_accounts`
8. create initial `CHARGE` transaction for the total amount

### 8.2 Initial charge is mandatory
Every account that represents debt or layaway commitment should have an initial `CHARGE`.

This makes the ledger explicit and avoids hidden totals.

### 8.3 Payment flow
A payment must:
1. insert a `PAYMENT` transaction
2. optionally update installment paid amount if installments are used
3. optionally update account status if resulting balance reaches zero

### 8.4 Status update rules
Status must not be changed arbitrarily.

Recommended rules:
- balance > 0 and due date passed -> possibly `OVERDUE`
- balance = 0 -> `PAID`
- manually canceled -> `CANCELED`

### 8.5 Do not trust UI totals
The backend must calculate or validate:
- account item totals
- resulting charge amount
- remaining balance

UI values are advisory, not authoritative.

---

## 9) Frontend behavior rules

### 9.1 Account list
The account list should display:
- account id / reference
- client name
- account type
- account status
- opening date
- due date
- total charge
- total paid
- balance

### 9.2 Totals in UI
The UI may display totals, but should receive authoritative values from the API whenever possible.

### 9.3 Account detail screen
The account detail should have clear sections:
- header
- items
- transactions
- installments
- files
- type-specific section:
  - credit data
  - layaway data

### 9.4 Layaway UI
Layaway accounts should show:
- reserved garments/items
- deposit
- balance
- expected arrival date
- ready state
- delivery date

### 9.5 Credit UI
Credit accounts should show:
- financed items
- total debt
- payment history
- installments schedule
- overdue indicator

---

## 10) DTO guidance

### 10.1 Account list DTO
Should include:
- `accountId`
- `clientId`
- `clientName`
- `accountTypeCode`
- `accountTypeName`
- `accountStatusCode`
- `accountStatusName`
- `openingDate`
- `dueDate`
- `totalCharged`
- `totalPaid`
- `balance`

### 10.2 Account detail DTO
Should include:
- header fields
- type-specific extension
- items
- transactions
- installments
- attached files

### 10.3 Transaction create DTO
Should include:
- `transactionType`
- `transactionDate`
- `amount`
- `paymentMethod`
- `reference`
- `comments`

### 10.4 Item create DTO
Should include:
- product linkage ids if available
- description snapshot
- quantity
- unit price
- discount amount

---

## 11) Validation rules

### 11.1 Accounts
Validate:
- client exists
- employee exists
- site exists
- account type exists
- account status exists

### 11.2 Items
Validate:
- quantity > 0
- unit price >= 0
- discount >= 0
- line total >= 0
- description snapshot is required

### 11.3 Transactions
Validate:
- transaction type is allowed
- amount is not zero unless explicitly supported
- payment method only for real payments if business requires it

### 11.4 Installments
Validate:
- installment number unique per account
- expected amount > 0
- paid amount >= 0

---

## 12) Recommended EF Core interpretation

### 12.1 Entity boundaries
Copilot should generate entities roughly grouped as:

- `Account`
- `AccountType`
- `AccountStatus`
- `AccountItem`
- `AccountTransaction`
- `AccountInstallment`
- `CreditAccount`
- `LayawayAccount`
- `AccountItemSerialItem`

### 12.2 Navigation rules
`Account` should have:
- many `AccountItems`
- many `AccountTransactions`
- many `AccountInstallments`
- optional `CreditAccount`
- optional `LayawayAccount`

### 12.3 Foreign key CLR type consistency
When Copilot generates EF Core entities, FK CLR types must match the principal key CLR types exactly.

Example:
- if `ProductVisualDefinition.ProductVisualDefinitionId` is `long`
- then `AccountItem.ProductVisualDefinitionId` must be `long?`

Do not generate `int?` in that case.

### 12.4 Balance projection
If the API needs list performance, use projections or SQL views to expose:
- total charged
- total paid
- balance

Do not solve this by denormalizing too early.

---

## 13) Type-specific business rules

### 13.1 Credit account rules
- usually has `due_date`
- may have installments
- may become `OVERDUE`
- may use `credit_accounts` fields like extension dates and terms

### 13.2 Layaway account rules
- usually starts with deposit
- may reserve serialized or non-serialized items
- may use arrival / ready / delivery operational dates
- should use `layaway_accounts` for operational state

---

## 14) Serialized reservation rules

### 14.1 When to use `account_item_serial_items`
Only use it when the account reserves exact serialized pieces.

### 14.2 When not to use it
Do not use it for quantity-only reservations when the exact serial unit does not matter.

### 14.3 Integrity rule
One serialized inventory item should not be linked ambiguously to multiple account items at the same time.

The unique constraint on `inventory_serial_item_id` supports this.

---

## 15) Files linked to accounts

### 15.1 Important schema note
The `files` table may reference `accounts` through `files.account_id`.

Because the new `accounts.account_id` uses `bigint`, the `files.account_id` column must remain aligned with that type.

### 15.2 Copilot rule
Any migration or entity code that touches account files must assume:
- `files.account_id` should be `bigint`
- the FK to `accounts(account_id)` must exist
- deleting an account should normally detach files with `SET NULL`, not delete files automatically, unless business rules say otherwise

---

## 16) Things Copilot must NOT do

- Do not reintroduce a monolithic table equivalent to legacy `Cuentas_Cobrar`
- Do not generate foreign keys with mismatched CLR/database types relative to their principal keys
- Do not store client name and phone redundantly in `accounts`
- Do not persist a balance column as business truth
- Do not compute historical item prices from live product price tables
- Do not hardcode status logic in the UI only
- Do not skip the initial `CHARGE` transaction
- Do not place layaway-specific fields in generic `accounts`

---

## 17) Recommended query strategy

### 17.1 List screen
Use projections that aggregate:
- item total
- payments total
- balance
- client name
- account type/status

### 17.2 Detail screen
Load:
- account header
- items
- transactions
- installments
- credit/layaway extension
- files

### 17.3 Performance
Prefer:
- projection DTO queries
- grouped SQL queries
- indexed filters by:
  - `client_id`
  - `account_status_id`
  - `account_type_id`
  - `opening_date`
  - `due_date`

---

## 18) Final architectural interpretation

This account schema must be treated as:
- normalized
- ledger-driven
- suitable for both credit and layaway
- historically stable
- API-friendly
- UI-friendly

This is the default interpretation for all generated code related to:
- customer accounts
- payments
- layaways
- credit
- account reporting
- account migration
- account files

---
