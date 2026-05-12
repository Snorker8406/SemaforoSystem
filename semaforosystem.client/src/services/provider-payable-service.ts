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

export interface PayableCatalogItem {
  id: number
  code: string
  name: string
  description: string | null
  isActive: boolean
}

// ── Query ────────────────────────────────────────────────

export interface ProviderPayableQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  providerId?: number
  siteId?: number
  providerPayableTypeId?: number
  providerPayableStatusId?: number
  typeCode?: string
  statusCode?: string
  currencyCode?: string
  documentNumber?: string
  documentDateFrom?: string
  documentDateTo?: string
  dueDateFrom?: string
  dueDateTo?: string
  onlyOpen?: boolean
  onlyOverdue?: boolean
  purchaseOrderId?: number
  purchaseReceiptId?: number
}

// ── Responses ────────────────────────────────────────────

export interface ProviderPayableListItem {
  providerPayableId: number
  providerId: number
  providerLegalName: string
  providerTradeName: string | null
  siteId: number
  siteName: string
  providerPayableTypeId: number
  typeCode: string
  typeName: string
  providerPayableStatusId: number
  statusCode: string
  statusName: string
  documentNumber: string | null
  reference: string | null
  documentDate: string
  dueDate: string | null
  isOverdue: boolean
  currencyCode: string
  subtotal: number
  discountTotal: number
  taxTotal: number
  total: number
  chargedAmount: number
  paidAmount: number
  balance: number
  purchaseOrderId: number | null
  purchaseReceiptId: number | null
  createdAt: string
  updatedAt: string
}

export interface ProviderPayableLineResponse {
  providerPayableLineId: number
  providerPayableId: number
  lineNumber: number
  purchaseOrderLineId: number | null
  purchaseReceiptLineId: number | null
  productId: number | null
  productVisualDefinitionId: number | null
  inventoryItemDefinitionId: number | null
  descriptionSnapshot: string
  quantity: number
  unitCost: number
  discountAmount: number
  taxAmount: number
  lineTotal: number
  notes: string | null
}

export interface ProviderPayableTransactionResponse {
  providerPayableTransactionId: number
  providerPayableId: number
  transactionType: string
  providerPaymentMethodId: number | null
  providerPaymentMethodCode: string | null
  providerPaymentMethodName: string | null
  transactionDate: string
  amount: number
  reference: string | null
  comments: string | null
  createdByEmployeeId: number
  createdByEmployeeName: string | null
  createdAt: string
}

export interface ProviderPayableDetailResponse extends ProviderPayableListItem {
  openedByEmployeeId: number
  openedByEmployeeName: string | null
  notes: string | null
  lines: ProviderPayableLineResponse[]
  transactions: ProviderPayableTransactionResponse[]
}

// ── Requests ─────────────────────────────────────────────

export interface CreateProviderPayableLineRequest {
  lineNumber: number
  purchaseOrderLineId?: number | null
  purchaseReceiptLineId?: number | null
  productId?: number | null
  productVisualDefinitionId?: number | null
  inventoryItemDefinitionId?: number | null
  descriptionSnapshot: string
  quantity: number
  unitCost: number
  discountAmount: number
  taxAmount: number
  lineTotal?: number | null
  notes?: string | null
}

export interface CreateProviderPayableRequest {
  providerId: number
  providerPayableTypeId: number
  providerPayableStatusId?: number | null
  siteId: number
  openedByEmployeeId: number
  purchaseOrderId?: number | null
  purchaseReceiptId?: number | null
  documentNumber?: string | null
  reference?: string | null
  documentDate?: string | null
  dueDate?: string | null
  currencyCode: string
  subtotal?: number | null
  discountTotal?: number | null
  taxTotal?: number | null
  total?: number | null
  notes?: string | null
  lines: CreateProviderPayableLineRequest[]
}

export interface UpdateProviderPayableRequest {
  providerPayableTypeId: number
  providerPayableStatusId: number
  purchaseOrderId?: number | null
  purchaseReceiptId?: number | null
  documentNumber?: string | null
  reference?: string | null
  documentDate: string
  dueDate?: string | null
  currencyCode: string
  subtotal: number
  discountTotal: number
  taxTotal: number
  total: number
  notes?: string | null
}

export interface UpdateProviderPayableStatusRequest {
  providerPayableStatusId: number
  comments?: string | null
}

export type UpdateProviderPayableLineRequest = CreateProviderPayableLineRequest

export interface CreateProviderPayableTransactionRequest {
  transactionType: string
  providerPaymentMethodId?: number | null
  transactionDate?: string | null
  amount: number
  reference?: string | null
  comments?: string | null
  createdByEmployeeId: number
}

// ── Helpers ──────────────────────────────────────────────

const BASE = '/api/provider-payables'

function toQuery(params: ProviderPayableQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

// ── Catalogs ─────────────────────────────────────────────

export function getPayableTypes() {
  return apiClient.get<PayableCatalogItem[]>(`${BASE}/catalogs/types`)
}

export function getPayableStatuses() {
  return apiClient.get<PayableCatalogItem[]>(`${BASE}/catalogs/statuses`)
}

export function getPayablePaymentMethods() {
  return apiClient.get<PayableCatalogItem[]>(`${BASE}/catalogs/payment-methods`)
}

export function getPayableTransactionTypes() {
  return apiClient.get<string[]>(`${BASE}/catalogs/transaction-types`)
}

// ── Header ───────────────────────────────────────────────

export function getProviderPayables(params: ProviderPayableQueryParams = {}) {
  return apiClient.get<PagedResponse<ProviderPayableListItem>>(`${BASE}${toQuery(params)}`)
}

export function getProviderPayable(id: number) {
  return apiClient.get<ProviderPayableDetailResponse>(`${BASE}/${id}`)
}

export function createProviderPayable(data: CreateProviderPayableRequest) {
  return apiClient.post<ProviderPayableDetailResponse>(BASE, data)
}

export function updateProviderPayable(id: number, data: UpdateProviderPayableRequest) {
  return apiClient.put<ProviderPayableDetailResponse>(`${BASE}/${id}`, data)
}

export function updateProviderPayableStatus(
  id: number,
  data: UpdateProviderPayableStatusRequest,
) {
  return apiClient.patch<void>(`${BASE}/${id}/status`, data)
}

export function deleteProviderPayable(id: number) {
  return apiClient.delete<void>(`${BASE}/${id}`)
}

// ── Lines ────────────────────────────────────────────────

export function getProviderPayableLines(id: number) {
  return apiClient.get<ProviderPayableLineResponse[]>(`${BASE}/${id}/lines`)
}

export function addProviderPayableLine(
  id: number,
  data: CreateProviderPayableLineRequest,
) {
  return apiClient.post<ProviderPayableLineResponse>(`${BASE}/${id}/lines`, data)
}

export function updateProviderPayableLine(
  id: number,
  lineId: number,
  data: UpdateProviderPayableLineRequest,
) {
  return apiClient.put<ProviderPayableLineResponse>(`${BASE}/${id}/lines/${lineId}`, data)
}

export function deleteProviderPayableLine(id: number, lineId: number) {
  return apiClient.delete<void>(`${BASE}/${id}/lines/${lineId}`)
}

// ── Transactions ─────────────────────────────────────────

export function getProviderPayableTransactions(id: number) {
  return apiClient.get<ProviderPayableTransactionResponse[]>(`${BASE}/${id}/transactions`)
}

export function addProviderPayableTransaction(
  id: number,
  data: CreateProviderPayableTransactionRequest,
) {
  return apiClient.post<ProviderPayableTransactionResponse>(
    `${BASE}/${id}/transactions`,
    data,
  )
}

export function deleteProviderPayableTransaction(id: number, txId: number) {
  return apiClient.delete<void>(`${BASE}/${id}/transactions/${txId}`)
}
