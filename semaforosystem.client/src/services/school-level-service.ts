import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface SchoolLevelResponse {
  schoolLevelId: number
  name: string
  description: string | null
  schoolCount: number
}

export interface CreateSchoolLevelRequest {
  name: string
  description?: string | null
}

export interface UpdateSchoolLevelRequest {
  name: string
  description?: string | null
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface SchoolLevelQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
}

// ── API endpoints ────────────────────────────────────────

const BASE = '/api/schoollevels'

function toQueryString(params: SchoolLevelQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

export async function getSchoolLevels(
  params: SchoolLevelQueryParams = {},
): Promise<PagedResponse<SchoolLevelResponse>> {
  return apiClient.get<PagedResponse<SchoolLevelResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getSchoolLevel(id: number): Promise<SchoolLevelResponse> {
  return apiClient.get<SchoolLevelResponse>(`${BASE}/${id}`)
}

export async function createSchoolLevel(
  data: CreateSchoolLevelRequest,
): Promise<SchoolLevelResponse> {
  return apiClient.post<SchoolLevelResponse>(BASE, data)
}

export async function updateSchoolLevel(
  id: number,
  data: UpdateSchoolLevelRequest,
): Promise<SchoolLevelResponse> {
  return apiClient.put<SchoolLevelResponse>(`${BASE}/${id}`, data)
}

export async function deleteSchoolLevel(id: number): Promise<void> {
  return apiClient.delete<void>(`${BASE}/${id}`)
}

export async function checkSchoolLevelNameExists(
  name: string,
  excludeId?: number,
): Promise<boolean> {
  const params = new URLSearchParams({ name })
  if (excludeId) params.set('excludeId', String(excludeId))
  const result = await apiClient.get<{ exists: boolean }>(
    `${BASE}/exists?${params.toString()}`,
  )
  return result.exists
}
