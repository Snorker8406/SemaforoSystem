import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface SizeResponse {
  sizeId: number
  sizeValue: string
  description: string | null
  sizeOrder: number | null
  sizeSystemId: number | null
  sizeSystemName: string | null
}

export interface SizeSystemResponse {
  sizeSystemId: number
  name: string
  description: string | null
  sizeCount: number
  sizes: SizeResponse[]
}

export interface SizeSystemSummary {
  sizeSystemId: number
  name: string
}

export interface CreateSizeSystemRequest {
  name: string
  description?: string | null
}

export type UpdateSizeSystemRequest = CreateSizeSystemRequest

export interface CreateSizeRequest {
  sizeValue: string
  description?: string | null
  sizeOrder?: number | null
}

export type UpdateSizeRequest = CreateSizeRequest

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface SizeSystemQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
}

export interface SizeQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  sizeSystemId?: number
}

// ── Helpers ──────────────────────────────────────────────

const BASE = '/api/sizes'

function toQueryString(params: object): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

// ── Size System endpoints ────────────────────────────────

export async function getSizeSystems(
  params: SizeSystemQueryParams = {},
): Promise<PagedResponse<SizeSystemResponse>> {
  return apiClient.get<PagedResponse<SizeSystemResponse>>(
    `${BASE}/systems${toQueryString(params)}`,
  )
}

export async function getSizeSystemsLookup(): Promise<SizeSystemSummary[]> {
  return apiClient.get<SizeSystemSummary[]>(`${BASE}/systems/lookup`)
}

export async function getSizeSystem(id: number): Promise<SizeSystemResponse> {
  return apiClient.get<SizeSystemResponse>(`${BASE}/systems/${id}`)
}

export async function createSizeSystem(
  data: CreateSizeSystemRequest,
): Promise<SizeSystemResponse> {
  return apiClient.post<SizeSystemResponse>(`${BASE}/systems`, data)
}

export async function updateSizeSystem(
  id: number,
  data: UpdateSizeSystemRequest,
): Promise<SizeSystemResponse> {
  return apiClient.put<SizeSystemResponse>(`${BASE}/systems/${id}`, data)
}

export async function deleteSizeSystem(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/systems/${id}`)
}

// ── Size endpoints ───────────────────────────────────────

export async function getSizes(
  params: SizeQueryParams = {},
): Promise<PagedResponse<SizeResponse>> {
  return apiClient.get<PagedResponse<SizeResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getSizesBySystem(systemId: number): Promise<SizeResponse[]> {
  return apiClient.get<SizeResponse[]>(`${BASE}/systems/${systemId}/sizes`)
}

export async function getSize(id: number): Promise<SizeResponse> {
  return apiClient.get<SizeResponse>(`${BASE}/${id}`)
}

export async function createSize(
  systemId: number,
  data: CreateSizeRequest,
): Promise<SizeResponse> {
  return apiClient.post<SizeResponse>(`${BASE}/systems/${systemId}/sizes`, data)
}

export async function updateSize(
  id: number,
  data: UpdateSizeRequest,
): Promise<SizeResponse> {
  return apiClient.put<SizeResponse>(`${BASE}/${id}`, data)
}

export async function moveSize(
  id: number,
  targetSystemId: number,
): Promise<SizeResponse> {
  return apiClient.put<SizeResponse>(`${BASE}/${id}/move/${targetSystemId}`)
}

export async function deleteSize(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/${id}`)
}
