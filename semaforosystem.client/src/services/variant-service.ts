import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface VariantResponse {
  productVariantId: number
  variantValue: string
  description: string | null
  productVariantSystemId: number
  productVariantSystemName: string | null
}

export interface VariantSystemResponse {
  productVariantId: number
  name: string
  description: string | null
  variantCount: number
  productCount: number
  variants: VariantResponse[]
}

export interface VariantSystemSummary {
  productVariantId: number
  name: string
}

export interface CreateVariantSystemRequest {
  name: string
  description?: string | null
}

export type UpdateVariantSystemRequest = CreateVariantSystemRequest

export interface CreateVariantRequest {
  variantValue: string
  description?: string | null
}

export type UpdateVariantRequest = CreateVariantRequest

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface VariantSystemQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
}

export interface VariantQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  productVariantSystemId?: number
}

// ── Helpers ──────────────────────────────────────────────

const BASE = '/api/variantsystems'

function toQueryString(params: Record<string, unknown>): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

// ── Variant System endpoints ─────────────────────────────

export async function getVariantSystems(
  params: VariantSystemQueryParams = {},
): Promise<PagedResponse<VariantSystemResponse>> {
  return apiClient.get<PagedResponse<VariantSystemResponse>>(
    `${BASE}/systems${toQueryString(params)}`,
  )
}

export async function getVariantSystemsLookup(): Promise<VariantSystemSummary[]> {
  return apiClient.get<VariantSystemSummary[]>(`${BASE}/systems/lookup`)
}

export async function getVariantSystem(id: number): Promise<VariantSystemResponse> {
  return apiClient.get<VariantSystemResponse>(`${BASE}/systems/${id}`)
}

export async function createVariantSystem(
  data: CreateVariantSystemRequest,
): Promise<VariantSystemResponse> {
  return apiClient.post<VariantSystemResponse>(`${BASE}/systems`, data)
}

export async function updateVariantSystem(
  id: number,
  data: UpdateVariantSystemRequest,
): Promise<VariantSystemResponse> {
  return apiClient.put<VariantSystemResponse>(`${BASE}/systems/${id}`, data)
}

export async function deleteVariantSystem(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/systems/${id}`)
}

// ── Variant endpoints ────────────────────────────────────

export async function getVariants(
  params: VariantQueryParams = {},
): Promise<PagedResponse<VariantResponse>> {
  return apiClient.get<PagedResponse<VariantResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getVariantsBySystem(systemId: number): Promise<VariantResponse[]> {
  return apiClient.get<VariantResponse[]>(`${BASE}/systems/${systemId}/variants`)
}

export async function getVariant(id: number): Promise<VariantResponse> {
  return apiClient.get<VariantResponse>(`${BASE}/${id}`)
}

export async function createVariant(
  systemId: number,
  data: CreateVariantRequest,
): Promise<VariantResponse> {
  return apiClient.post<VariantResponse>(`${BASE}/systems/${systemId}/variants`, data)
}

export async function updateVariant(
  id: number,
  data: UpdateVariantRequest,
): Promise<VariantResponse> {
  return apiClient.put<VariantResponse>(`${BASE}/${id}`, data)
}

export async function moveVariant(
  id: number,
  targetSystemId: number,
): Promise<VariantResponse> {
  return apiClient.put<VariantResponse>(`${BASE}/${id}/move/${targetSystemId}`)
}

export async function deleteVariant(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/${id}`)
}
