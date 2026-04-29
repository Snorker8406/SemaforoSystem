import apiClient from '@/lib/api-client'

// ── Shared ───────────────────────────────────────────────

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

// ── Catalogs ─────────────────────────────────────────────

export interface AccountCatalogItem {
  id: number
  code: string
  name: string
  description: string | null
  isActive: boolean
}

// ── Query ────────────────────────────────────────────────

export interface AccountQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  clientId?: number
  siteId?: number
  accountTypeId?: number
  accountStatusId?: number
  typeCode?: string
  statusCode?: string
  from?: string
  to?: string
  overdue?: boolean
}

// ── Responses ────────────────────────────────────────────

export interface AccountListItem {
  accountId: number
  clientId: number
  clientName: string | null
  siteId: number
  siteName: string | null
  accountTypeId: number
  accountTypeCode: string | null
  accountTypeName: string | null
  accountStatusId: number
  accountStatusCode: string | null
  accountStatusName: string | null
  openingDate: string
  dueDate: string | null
  currencyCode: string
  reference: string | null
  totalCharged: number
  totalPaid: number
  balance: number
}

export interface AccountItemResponse {
  accountItemId: number
  inventoryItemDefinitionId: number | null
  productId: number | null
  productVisualDefinitionId: number | null
  descriptionSnapshot: string
  quantity: number
  unitPrice: number
  discountAmount: number
  lineTotal: number
  createdAt: string
  serialInventoryItemIds: number[]
}

export interface AccountTransactionResponse {
  accountTransactionId: number
  accountId: number
  transactionType: string
  transactionDate: string
  amount: number
  paymentMethod: string | null
  reference: string | null
  comments: string | null
  createdByEmployeeId: number
  createdByEmployeeName: string | null
  createdAt: string
}

export interface AccountInstallmentResponse {
  accountInstallmentId: number
  installmentNumber: number
  dueDate: string
  expectedAmount: number
  paidAmount: number
  isPaid: boolean
  paidDate: string | null
  notes: string | null
}

export interface CreditAccountResponse {
  creditDays: number | null
  graceUntil: string | null
  originalDueDate: string | null
  lastExtensionDate: string | null
  extendedDueDate: string | null
  creditLimitSnapshot: number | null
  requiresGuarantor: boolean
}

export interface LayawayAccountResponse {
  depositAmount: number
  expectedArrivalDate: string | null
  readyDate: string | null
  deliveryDate: string | null
  isReady: boolean
  expirationDate: string | null
  cancellationPolicyNotes: string | null
}

export interface AccountDetailResponse extends AccountListItem {
  openedByEmployeeId: number
  openedByEmployeeName: string | null
  closedDate: string | null
  canceledDate: string | null
  notes: string | null
  createdAt: string
  updatedAt: string
  items: AccountItemResponse[]
  transactions: AccountTransactionResponse[]
  installments: AccountInstallmentResponse[]
  credit: CreditAccountResponse | null
  layaway: LayawayAccountResponse | null
}

// ── Requests ─────────────────────────────────────────────

export interface CreateAccountItemRequest {
  inventoryItemDefinitionId?: number | null
  productId?: number | null
  productVisualDefinitionId?: number | null
  descriptionSnapshot: string
  quantity: number
  unitPrice: number
  discountAmount: number
  lineTotal?: number | null
  serialInventoryItemIds?: number[] | null
}

export interface CreateCreditAccountRequest {
  creditDays?: number | null
  graceUntil?: string | null
  originalDueDate?: string | null
  extendedDueDate?: string | null
  creditLimitSnapshot?: number | null
  requiresGuarantor?: boolean
}

export interface CreateLayawayAccountRequest {
  depositAmount: number
  expectedArrivalDate?: string | null
  readyDate?: string | null
  deliveryDate?: string | null
  isReady?: boolean
  expirationDate?: string | null
  cancellationPolicyNotes?: string | null
}

export interface CreateInstallmentRequest {
  installmentNumber: number
  dueDate: string
  expectedAmount: number
  notes?: string | null
}

export interface CreateAccountRequest {
  clientId: number
  siteId: number
  openedByEmployeeId: number
  accountTypeId?: number | null
  accountTypeCode?: string | null
  accountStatusId?: number | null
  accountStatusCode?: string | null
  openingDate?: string | null
  dueDate?: string | null
  currencyCode?: string | null
  reference?: string | null
  notes?: string | null
  items: CreateAccountItemRequest[]
  credit?: CreateCreditAccountRequest | null
  layaway?: CreateLayawayAccountRequest | null
  installments?: CreateInstallmentRequest[] | null
  generateInitialCharge?: boolean
  initialPaymentAmount?: number | null
  initialPaymentMethod?: string | null
}

export interface UpdateAccountRequest {
  accountStatusId?: number | null
  accountStatusCode?: string | null
  dueDate?: string | null
  currencyCode?: string | null
  reference?: string | null
  notes?: string | null
}

export interface UpdateAccountStatusRequest {
  statusCode: string
}

export interface CreateAccountTransactionRequest {
  transactionType: string
  transactionDate?: string | null
  amount: number
  paymentMethod?: string | null
  reference?: string | null
  comments?: string | null
  createdByEmployeeId: number
}

export interface CancelAccountRequest {
  reason?: string | null
  canceledByEmployeeId: number
}

// ── Helpers ───────────────────────────────────────────────

const BASE = '/api/accounts'

function toQueryString(params: AccountQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

// ── Catalog endpoints ─────────────────────────────────────

export function getAccountTypes(): Promise<AccountCatalogItem[]> {
  return apiClient.get<AccountCatalogItem[]>(`${BASE}/catalogs/types`)
}

export function getAccountStatuses(): Promise<AccountCatalogItem[]> {
  return apiClient.get<AccountCatalogItem[]>(`${BASE}/catalogs/statuses`)
}

export function getTransactionTypes(): Promise<string[]> {
  return apiClient.get<string[]>(`${BASE}/catalogs/transaction-types`)
}

// ── Account endpoints ─────────────────────────────────────

export function getAccounts(
  params: AccountQueryParams = {},
): Promise<PagedResponse<AccountListItem>> {
  return apiClient.get<PagedResponse<AccountListItem>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export function getAccount(id: number): Promise<AccountDetailResponse> {
  return apiClient.get<AccountDetailResponse>(`${BASE}/${id}`)
}

export function createAccount(
  data: CreateAccountRequest,
): Promise<AccountDetailResponse> {
  return apiClient.post<AccountDetailResponse>(BASE, data)
}

export function updateAccount(
  id: number,
  data: UpdateAccountRequest,
): Promise<AccountDetailResponse> {
  return apiClient.put<AccountDetailResponse>(`${BASE}/${id}`, data)
}

export function updateAccountStatus(
  id: number,
  data: UpdateAccountStatusRequest,
): Promise<void> {
  return apiClient.patch<void>(`${BASE}/${id}/status`, data)
}

export function cancelAccount(
  id: number,
  data: CancelAccountRequest,
): Promise<AccountDetailResponse> {
  return apiClient.post<AccountDetailResponse>(`${BASE}/${id}/cancel`, data)
}

// ── Item endpoints ────────────────────────────────────────

export function getAccountItems(id: number): Promise<AccountItemResponse[]> {
  return apiClient.get<AccountItemResponse[]>(`${BASE}/${id}/items`)
}

export function addAccountItem(
  id: number,
  data: CreateAccountItemRequest,
): Promise<AccountItemResponse> {
  return apiClient.post<AccountItemResponse>(`${BASE}/${id}/items`, data)
}

export function deleteAccountItem(
  accountId: number,
  itemId: number,
): Promise<void> {
  return apiClient.delete<void>(`${BASE}/${accountId}/items/${itemId}`)
}

// ── Transaction endpoints ─────────────────────────────────

export function getAccountTransactions(
  id: number,
): Promise<AccountTransactionResponse[]> {
  return apiClient.get<AccountTransactionResponse[]>(`${BASE}/${id}/transactions`)
}

export function addAccountTransaction(
  id: number,
  data: CreateAccountTransactionRequest,
): Promise<AccountTransactionResponse> {
  return apiClient.post<AccountTransactionResponse>(
    `${BASE}/${id}/transactions`,
    data,
  )
}

// ── Installment endpoints ─────────────────────────────────

export function getAccountInstallments(
  id: number,
): Promise<AccountInstallmentResponse[]> {
  return apiClient.get<AccountInstallmentResponse[]>(
    `${BASE}/${id}/installments`,
  )
}

export function addAccountInstallment(
  id: number,
  data: CreateInstallmentRequest,
): Promise<AccountInstallmentResponse> {
  return apiClient.post<AccountInstallmentResponse>(
    `${BASE}/${id}/installments`,
    data,
  )
}

// ── Layaway endpoints ─────────────────────────────────────

export function markLayawayReady(id: number): Promise<void> {
  return apiClient.patch<void>(`${BASE}/${id}/layaway/ready`, {})
}

export function deliverLayaway(id: number): Promise<void> {
  return apiClient.patch<void>(`${BASE}/${id}/layaway/deliver`, {})
}
