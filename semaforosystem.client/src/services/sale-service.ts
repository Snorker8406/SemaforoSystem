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

export interface SalesCatalogItem {
  id: number
  code: string
  name: string
  description: string | null
  active: boolean
}

// ── Query ────────────────────────────────────────────────

export interface SaleQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  siteId?: number
  clientId?: number
  employeeId?: number
  saleTypeId?: number
  saleStatusId?: number
  statusCode?: string
  accountId?: number
  from?: string
  to?: string
}

// ── Responses ────────────────────────────────────────────

export interface SaleListItem {
  saleId: number
  folio: string | null
  saleDate: string
  siteId: number
  siteName: string | null
  clientId: number | null
  clientName: string | null
  employeeId: number
  employeeName: string | null
  saleTypeId: number
  saleTypeCode: string | null
  saleTypeName: string | null
  saleStatusId: number
  saleStatusCode: string | null
  saleStatusName: string | null
  accountId: number | null
  subtotal: number
  discountTotal: number
  taxTotal: number
  total: number
  paidTotal: number
  balance: number
}

export interface SaleLineResponse {
  saleLineId: number
  lineNumber: number
  lineType: string
  productId: number | null
  productVisualDefinitionId: number | null
  productComboVisualDefinitionId: number | null
  inventoryItemDefinitionId: number | null
  sizeId: number | null
  descriptionSnapshot: string
  quantity: number
  unitPrice: number
  discountAmount: number
  taxAmount: number
  lineTotal: number
  notes: string | null
  legacySaleDetailId: number | null
  serialInventoryItemIds: number[]
}

export interface SalePaymentResponse {
  salePaymentId: number
  paymentMethodId: number
  paymentMethodCode: string | null
  paymentMethodName: string | null
  paymentDate: string
  amount: number
  reference: string | null
  comments: string | null
}

export interface SaleDetailResponse extends SaleListItem {
  externalReference: string | null
  legacySaleId: number | null
  notes: string | null
  createdAt: string
  updatedAt: string
  lines: SaleLineResponse[]
  payments: SalePaymentResponse[]
}

// ── Requests ─────────────────────────────────────────────

export interface UpdateSaleStatusRequest {
  statusCode: string
}

export interface CancelSaleRequest {
  reason?: string | null
}

export interface CreateSalePaymentRequest {
  paymentMethodId: number
  paymentDate?: string | null
  amount: number
  reference?: string | null
  comments?: string | null
}

// ── Helpers ──────────────────────────────────────────────

const BASE = '/api/sales'

function toQueryString(params: SaleQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

// ── Catalog endpoints ────────────────────────────────────

export function getSaleTypes(): Promise<SalesCatalogItem[]> {
  return apiClient.get<SalesCatalogItem[]>(`${BASE}/catalogs/types`)
}

export function getSaleStatuses(): Promise<SalesCatalogItem[]> {
  return apiClient.get<SalesCatalogItem[]>(`${BASE}/catalogs/statuses`)
}

export function getPaymentMethods(): Promise<SalesCatalogItem[]> {
  return apiClient.get<SalesCatalogItem[]>(`${BASE}/catalogs/payment-methods`)
}

// ── Sale endpoints ───────────────────────────────────────

export function getSales(
  params: SaleQueryParams = {},
): Promise<PagedResponse<SaleListItem>> {
  return apiClient.get<PagedResponse<SaleListItem>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export function getSale(id: number): Promise<SaleDetailResponse> {
  return apiClient.get<SaleDetailResponse>(`${BASE}/${id}`)
}

export function updateSaleStatus(
  id: number,
  data: UpdateSaleStatusRequest,
): Promise<void> {
  return apiClient.patch<void>(`${BASE}/${id}/status`, data)
}

export function confirmSale(id: number): Promise<SaleDetailResponse> {
  return apiClient.post<SaleDetailResponse>(`${BASE}/${id}/confirm`, {})
}

export function cancelSale(
  id: number,
  data: CancelSaleRequest = {},
): Promise<SaleDetailResponse> {
  return apiClient.post<SaleDetailResponse>(`${BASE}/${id}/cancel`, data)
}

// ── Payments ─────────────────────────────────────────────

export function getSalePayments(id: number): Promise<SalePaymentResponse[]> {
  return apiClient.get<SalePaymentResponse[]>(`${BASE}/${id}/payments`)
}

export function addSalePayment(
  id: number,
  data: CreateSalePaymentRequest,
): Promise<SalePaymentResponse> {
  return apiClient.post<SalePaymentResponse>(`${BASE}/${id}/payments`, data)
}
