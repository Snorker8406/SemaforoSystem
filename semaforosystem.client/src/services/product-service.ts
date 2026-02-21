import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface CategoryInfo {
  categoryId: number
  name: string
}

export interface VariantSystemInfo {
  productVariantId: number
  name: string
}

export interface BrandLookup {
  brandId: number
  name: string | null
}

export interface ProductResponse {
  productId: number
  name: string | null
  barcode: string | null
  description: string | null
  model: string | null
  comments: string | null
  serialCount: number | null
  serialize: boolean | null
  createDate: string | null
  brandId: number | null
  brandName: string | null
  categories: CategoryInfo[]
  schoolCount: number
  hasPicture: boolean
  pictureCount: number
  stockTotal: number
  latestCost: number | null
  latestPrice: number | null
  variantSystems: VariantSystemInfo[]
}

export interface CreateProductRequest {
  name?: string | null
  barcode?: string | null
  description?: string | null
  model?: string | null
  comments?: string | null
  serialCount?: number | null
  serialize?: boolean | null
  brandId?: number | null
  categoryIds: number[]
}

export type UpdateProductRequest = CreateProductRequest

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ProductQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  brandId?: number
  categoryId?: number
  hasSchools?: boolean
  model?: string
  serialize?: boolean
  hasStock?: boolean
}

// ── API endpoints ────────────────────────────────────────

const BASE = '/api/products'

function toQueryString(params: ProductQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

export async function getProducts(
  params: ProductQueryParams = {},
): Promise<PagedResponse<ProductResponse>> {
  return apiClient.get<PagedResponse<ProductResponse>>(
    `${BASE}${toQueryString(params)}`,
  )
}

export async function getProduct(id: number): Promise<ProductResponse> {
  return apiClient.get<ProductResponse>(`${BASE}/${id}`)
}

export async function createProduct(
  data: CreateProductRequest,
): Promise<ProductResponse> {
  return apiClient.post<ProductResponse>(BASE, data)
}

export async function updateProduct(
  id: number,
  data: UpdateProductRequest,
): Promise<ProductResponse> {
  return apiClient.put<ProductResponse>(`${BASE}/${id}`, data)
}

export async function deleteProduct(id: number): Promise<void> {
  return apiClient.delete(`${BASE}/${id}`)
}

export async function getBrands(): Promise<BrandLookup[]> {
  return apiClient.get<BrandLookup[]>(`${BASE}/brands`)
}

export async function getCategories(): Promise<CategoryInfo[]> {
  return apiClient.get<CategoryInfo[]>(`${BASE}/categories`)
}
