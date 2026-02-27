import apiClient from '@/lib/api-client'
import { ApiError } from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface EmbroideryResponse {
  embroideryId: number
  schoolId: number | null
  schoolName: string | null
  name: string
  description: string | null
  stiches: string | null
  colorSecuence: string | null
  price: number | null
  createDate: string | null
  hasEmbFile: boolean
  hasDstFile: boolean
  hasImage: boolean
  imageDesignBase64: string | null
}

export interface EmbroideryLookup {
  embroideryId: number
  name: string
  schoolId: number | null
}

export interface EmbroideryQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  schoolId?: number
}

export interface CreateEmbroideryRequest {
  schoolId?: number | null
  name: string
  description?: string | null
  stiches?: string | null
  colorSecuence?: string | null
  price?: number | null
}

export interface UpdateEmbroideryRequest {
  schoolId?: number | null
  name: string
  description?: string | null
  stiches?: string | null
  colorSecuence?: string | null
  price?: number | null
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

// ── API endpoints ────────────────────────────────────────

const BASE = '/api/embroideries'

function toQueryString(params: EmbroideryQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

export async function getEmbroideries(
  params: EmbroideryQueryParams = {},
): Promise<PagedResponse<EmbroideryResponse>> {
  return apiClient.get<PagedResponse<EmbroideryResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getEmbroidery(id: number): Promise<EmbroideryResponse> {
  return apiClient.get<EmbroideryResponse>(`${BASE}/${id}`)
}

export async function getEmbroideriesLookup(
  schoolId?: number,
): Promise<EmbroideryLookup[]> {
  const qs = schoolId != null ? `?schoolId=${schoolId}` : ''
  return apiClient.get<EmbroideryLookup[]>(`${BASE}/lookup${qs}`)
}

export async function createEmbroidery(
  data: CreateEmbroideryRequest,
): Promise<EmbroideryResponse> {
  return apiClient.post<EmbroideryResponse>(BASE, data)
}

export async function updateEmbroidery(
  id: number,
  data: UpdateEmbroideryRequest,
): Promise<EmbroideryResponse> {
  return apiClient.put<EmbroideryResponse>(`${BASE}/${id}`, data)
}

export async function deleteEmbroidery(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/${id}`)
}

// ── Image upload ─────────────────────────────────────────

export async function uploadEmbroideryImage(
  id: number,
  file: File,
): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${BASE}/${id}/image`, {
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

/** URL for displaying the embroidery image (for img src) */
export function getEmbroideryImageUrl(id: number): string {
  return `${BASE}/${id}/image`
}

/** URL for displaying the image design (for img src) */
export function getEmbroideryImageDesignUrl(id: number): string {
  return `${BASE}/${id}/image-design`
}

export async function uploadEmbroideryImageDesign(
  id: number,
  file: File,
): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${BASE}/${id}/image-design`, {
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
