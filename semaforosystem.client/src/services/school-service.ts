import apiClient from '@/lib/api-client'
import { ApiError } from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface SchoolResponse {
  schoolId: number
  schoolLevelId: number
  schoolLevelName: string
  createDate: string | null
  name: string
  address: string
  ciudad: string | null
  state: string | null
  phoneNumber: string | null
  principalInfo: string | null
  email: string | null
  description: string | null
  hasLogo: boolean
  hasPhoto: boolean
}

export interface CreateSchoolRequest {
  schoolLevelId: number
  name: string
  address: string
  ciudad?: string | null
  state?: string | null
  phoneNumber?: string | null
  principalInfo?: string | null
  email?: string | null
  description?: string | null
}

export type UpdateSchoolRequest = CreateSchoolRequest

export interface SchoolLevel {
  schoolLevelId: number
  name: string
  description: string | null
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

export interface SchoolQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  schoolLevelId?: number
  ciudad?: string
  state?: string
}

export interface SchoolStats {
  totalSchools: number
  byLevel: { level: string; count: number }[]
  byState: { state: string; count: number }[]
}

// ── API endpoints ────────────────────────────────────────

const BASE = '/api/schools'

function toQueryString(params: SchoolQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

export async function getSchools(
  params: SchoolQueryParams = {},
): Promise<PagedResponse<SchoolResponse>> {
  return apiClient.get<PagedResponse<SchoolResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getSchool(id: number): Promise<SchoolResponse> {
  return apiClient.get<SchoolResponse>(`${BASE}/${id}`)
}

export async function createSchool(
  data: CreateSchoolRequest,
): Promise<SchoolResponse> {
  return apiClient.post<SchoolResponse>(BASE, data)
}

export async function updateSchool(
  id: number,
  data: UpdateSchoolRequest,
): Promise<SchoolResponse> {
  return apiClient.put<SchoolResponse>(`${BASE}/${id}`, data)
}

export async function deleteSchool(id: number): Promise<void> {
  return apiClient.delete<void>(`${BASE}/${id}`)
}

export interface SchoolLookupItem {
  schoolId: number
  name: string
}

export async function getSchoolsLookup(): Promise<SchoolLookupItem[]> {
  return apiClient.get<SchoolLookupItem[]>(`${BASE}/lookup`)
}

export async function getSchoolLevels(): Promise<SchoolLevel[]> {
  return apiClient.get<SchoolLevel[]>(`${BASE}/levels`)
}

export async function getSchoolStats(): Promise<SchoolStats> {
  return apiClient.get<SchoolStats>(`${BASE}/stats`)
}

export async function checkSchoolNameExists(
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

// ── Image upload / delete ────────────────────────────────

async function uploadImage(
  schoolId: number,
  type: 'logo' | 'photo',
  file: File,
): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${BASE}/${schoolId}/${type}`, {
    method: 'PUT',
    credentials: 'include',
    body: formData,
  })

  if (!response.ok) {
    const text = await response.text()
    let parsed: unknown
    try {
      parsed = JSON.parse(text)
    } catch {
      parsed = text
    }
    throw new ApiError(response.status, response.statusText, parsed)
  }
}

export async function uploadSchoolLogo(schoolId: number, file: File): Promise<void> {
  return uploadImage(schoolId, 'logo', file)
}

export async function uploadSchoolPhoto(schoolId: number, file: File): Promise<void> {
  return uploadImage(schoolId, 'photo', file)
}

export async function deleteSchoolLogo(schoolId: number): Promise<void> {
  return apiClient.delete<void>(`${BASE}/${schoolId}/logo`)
}

export async function deleteSchoolPhoto(schoolId: number): Promise<void> {
  return apiClient.delete<void>(`${BASE}/${schoolId}/photo`)
}

export function getSchoolLogoUrl(schoolId: number): string {
  return `${BASE}/${schoolId}/logo`
}

export function getSchoolPhotoUrl(schoolId: number): string {
  return `${BASE}/${schoolId}/photo`
}
