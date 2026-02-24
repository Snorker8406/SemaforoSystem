import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface ProductComboDetailResponse {
  productComboDetailId: number
  productComboId: number
  productId: number | null
  productName: string | null
  embroideryId: number | null
  embroideryName: string | null
}

export interface ComboSchoolInfo {
  schoolId: number
  name: string
}

export interface ProductComboResponse {
  productComboId: number
  name: string
  description: string | null
  active: boolean | null
  createDate: string | null
  detailCount: number
  priceCount: number
  schoolCount: number
  details: ProductComboDetailResponse[]
  schools: ComboSchoolInfo[]
}

export interface ProductComboSummary {
  productComboId: number
  name: string
}

export interface CreateProductComboRequest {
  name: string
  description?: string | null
  active?: boolean | null
  schoolIds?: number[] | null
}

export type UpdateProductComboRequest = CreateProductComboRequest

export interface CreateProductComboDetailRequest {
  productId?: number | null
  embroideryId?: number | null
}

export type UpdateProductComboDetailRequest = CreateProductComboDetailRequest

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ProductComboQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
}

// ── Helpers ──────────────────────────────────────────────

const BASE = '/api/productcombos'

function toQueryString(params: Record<string, unknown>): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

// ── Combo (header) endpoints ─────────────────────────────

export async function getProductCombos(
  params: ProductComboQueryParams = {},
): Promise<PagedResponse<ProductComboResponse>> {
  return apiClient.get<PagedResponse<ProductComboResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getProductCombosLookup(): Promise<ProductComboSummary[]> {
  return apiClient.get<ProductComboSummary[]>(`${BASE}/lookup`)
}

export async function getProductCombo(id: number): Promise<ProductComboResponse> {
  return apiClient.get<ProductComboResponse>(`${BASE}/${id}`)
}

export async function createProductCombo(
  data: CreateProductComboRequest,
): Promise<ProductComboResponse> {
  return apiClient.post<ProductComboResponse>(BASE, data)
}

export async function updateProductCombo(
  id: number,
  data: UpdateProductComboRequest,
): Promise<ProductComboResponse> {
  return apiClient.put<ProductComboResponse>(`${BASE}/${id}`, data)
}

export async function deleteProductCombo(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/${id}`)
}

// ── Combo Detail (lines) endpoints ───────────────────────

export async function getComboDetails(
  comboId: number,
): Promise<ProductComboDetailResponse[]> {
  return apiClient.get<ProductComboDetailResponse[]>(`${BASE}/${comboId}/details`)
}

export async function createComboDetail(
  comboId: number,
  data: CreateProductComboDetailRequest,
): Promise<ProductComboDetailResponse> {
  return apiClient.post<ProductComboDetailResponse>(`${BASE}/${comboId}/details`, data)
}

export async function updateComboDetail(
  comboId: number,
  detailId: number,
  data: UpdateProductComboDetailRequest,
): Promise<ProductComboDetailResponse> {
  return apiClient.put<ProductComboDetailResponse>(
    `${BASE}/${comboId}/details/${detailId}`,
    data,
  )
}

export async function deleteComboDetail(
  comboId: number,
  detailId: number,
): Promise<void> {
  return apiClient.delete(`${BASE}/${comboId}/details/${detailId}`)
}
