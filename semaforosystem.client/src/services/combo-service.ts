import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface ComboSchoolInfo {
  schoolId: number
  name: string
}

export interface ComponentVariantInfo {
  systemName: string
  variantValue: string
}

export interface ComponentResponse {
  productComboComponentId: number
  productComboVisualDefinitionId: number
  componentType: string
  productId: number | null
  productName: string | null
  productVisualDefinitionId: number | null
  visualDefinitionProductName: string | null
  visualDefinitionVariants: ComponentVariantInfo[]
  embroideryId: number | null
  embroideryName: string | null
  quantity: number
  placement: string | null
  isRequired: boolean
  extraPrice: number | null
  sortOrder: number
}

export interface VisualDefinitionResponse {
  productComboVisualDefinitionId: number
  productComboId: number
  name: string
  description: string | null
  fixedPriceAmount: number | null
  discountType: string
  discountValue: number | null
  priceListId: number | null
  priceListName: string | null
  isActive: boolean
  sortOrder: number
  createdAt: string
  updatedAt: string | null
  hasImage: boolean
  imageId: number | null
  componentCount: number
  components: ComponentResponse[]
  schools: ComboSchoolInfo[]
}

export interface ProductComboResponse {
  productComboId: number
  name: string
  description: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
  visualDefinitionCount: number
  schoolCount: number
  visualDefinitions: VisualDefinitionResponse[]
}

export interface ProductComboSummary {
  productComboId: number
  name: string
}

export interface CreateProductComboRequest {
  name: string
  description?: string | null
  isActive?: boolean | null
}

export type UpdateProductComboRequest = CreateProductComboRequest

export interface CreateVisualDefinitionRequest {
  name: string
  description?: string | null
  fixedPriceAmount?: number | null
  discountType?: string | null
  discountValue?: number | null
  priceListId?: number | null
  isActive?: boolean | null
  sortOrder?: number | null
  schoolIds?: number[] | null
}

export type UpdateVisualDefinitionRequest = CreateVisualDefinitionRequest

export interface CreateComponentRequest {
  componentType: string
  productId?: number | null
  productVisualDefinitionId?: number | null
  embroideryId?: number | null
  quantity?: number | null
  placement?: string | null
  isRequired?: boolean | null
  extraPrice?: number | null
  sortOrder?: number | null
}

export type UpdateComponentRequest = CreateComponentRequest

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
  isActive?: boolean
  schoolId?: number
}

// ── Helpers ──────────────────────────────────────────────

const BASE = '/api/productcombos'

function toQueryString(params: object): string {
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

// ── Visual Definition endpoints ──────────────────────────

export async function getComboVisualDefinitions(
  comboId: number,
): Promise<VisualDefinitionResponse[]> {
  return apiClient.get<VisualDefinitionResponse[]>(`${BASE}/${comboId}/visual-definitions`)
}

export async function createComboVisualDefinition(
  comboId: number,
  data: CreateVisualDefinitionRequest,
): Promise<VisualDefinitionResponse> {
  return apiClient.post<VisualDefinitionResponse>(`${BASE}/${comboId}/visual-definitions`, data)
}

export async function updateComboVisualDefinition(
  comboId: number,
  vdId: number,
  data: UpdateVisualDefinitionRequest,
): Promise<VisualDefinitionResponse> {
  return apiClient.put<VisualDefinitionResponse>(
    `${BASE}/${comboId}/visual-definitions/${vdId}`,
    data,
  )
}

export async function deleteComboVisualDefinition(
  comboId: number,
  vdId: number,
): Promise<void> {
  return apiClient.delete(`${BASE}/${comboId}/visual-definitions/${vdId}`)
}

// ── Component endpoints ──────────────────────────────────

export async function getComboComponents(
  comboId: number,
  vdId: number,
): Promise<ComponentResponse[]> {
  return apiClient.get<ComponentResponse[]>(
    `${BASE}/${comboId}/visual-definitions/${vdId}/components`,
  )
}

export async function createComboComponent(
  comboId: number,
  vdId: number,
  data: CreateComponentRequest,
): Promise<ComponentResponse> {
  return apiClient.post<ComponentResponse>(
    `${BASE}/${comboId}/visual-definitions/${vdId}/components`,
    data,
  )
}

export async function updateComboComponent(
  comboId: number,
  vdId: number,
  compId: number,
  data: UpdateComponentRequest,
): Promise<ComponentResponse> {
  return apiClient.put<ComponentResponse>(
    `${BASE}/${comboId}/visual-definitions/${vdId}/components/${compId}`,
    data,
  )
}

export async function deleteComboComponent(
  comboId: number,
  vdId: number,
  compId: number,
): Promise<void> {
  return apiClient.delete(
    `${BASE}/${comboId}/visual-definitions/${vdId}/components/${compId}`,
  )
}

// ── Image endpoints ──────────────────────────────────────

export function getComboImageUrl(imageId: number): string {
  return `${BASE}/images/${imageId}`
}

// ── School sync endpoint ─────────────────────────────────

export async function syncComboVisualDefinitionSchools(
  comboId: number,
  vdId: number,
  schoolIds: number[],
): Promise<ComboSchoolInfo[]> {
  return apiClient.put<ComboSchoolInfo[]>(
    `${BASE}/${comboId}/visual-definitions/${vdId}/schools`,
    schoolIds,
  )
}
