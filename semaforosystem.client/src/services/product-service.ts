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
  sizeSystemId: number | null
  sizeSystemName: string | null
  categories: CategoryInfo[]
  schoolCount: number
  hasPicture: boolean
  stockTotal: number
  latestCost: number | null
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
  sizeSystemId?: number | null
  categoryIds: number[]
  variantSystemIds: number[]
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

// ── Product Picture URL ──────────────────────────────────

export function getProductPictureUrl(productId: number): string {
  return `${BASE}/${productId}/picture`
}

// ── Product Schools ──────────────────────────────────────

export interface ProductSchoolInfo {
  schoolId: number
  name: string
  schoolLevelId: number
  schoolLevelName: string
  productVisualDefinitionId: number
}

export async function getProductSchools(
  productId: number,
): Promise<ProductSchoolInfo[]> {
  return apiClient.get<ProductSchoolInfo[]>(`${BASE}/${productId}/schools`)
}

// ── Item Definitions (variant-derived items) ─────────────

export interface ItemDefinitionVariantInfo {
  productVariantId: number
  variantValue: string
  systemName: string | null
}

export interface ItemDefinitionSummary {
  inventoryItemDefinitionId: number
  skuCode: string
  nameSnapshot: string | null
  isSerialized: boolean
  isActive: boolean
  sizeId: number | null
  sizeValue: string | null
  hasImage: boolean
  variants: ItemDefinitionVariantInfo[]
}

export async function getProductItemDefinitions(
  productId: number,
): Promise<ItemDefinitionSummary[]> {
  return apiClient.get<ItemDefinitionSummary[]>(
    `${BASE}/${productId}/item-definitions`,
  )
}

export function getItemDefinitionImageUrl(itemDefinitionId: number): string {
  return `/api/images/by-item-definition/${itemDefinitionId}/primary`
}

export function getVisualDefinitionImageUrl(visualDefinitionId: number): string {
  return `/api/images/by-visual-definition/${visualDefinitionId}/primary`
}

// ── Visual Definitions (grouped by variant combination) ──

export interface VisualDefinitionItem {
  inventoryItemDefinitionId: number
  skuCode: string
  nameSnapshot: string | null
  isSerialized: boolean
  isActive: boolean
  sizeId: number | null
  sizeValue: string | null
  sizeOrder: number | null
  // Effective price (resolved via ITEM_DEF → VISUAL_DEF → PRODUCT)
  priceAmount: number | null
  priceKind: string | null
  priceScope: string | null
  basePriceAmount: number | null
  promoName: string | null
  // Inventory balance (aggregated across all sites)
  stockTotal: number
  stockBySite?: VisualDefinitionItemSiteStock[]
}

export interface VisualDefinitionItemSiteStock {
  siteId: number
  siteName: string
  onHand: number
  reserved: number
  available: number
}

export interface VisualDefinitionSchoolInfo {
  schoolId: number
  name: string
  schoolLevelName: string
}

export interface VisualDefinitionEmbroideryInfo {
  embroideryId: number
  name: string
  placement: string
  isRequired: boolean
}

export interface VisualDefinitionGroup {
  productVisualDefinitionId: number
  variantsHash: string
  variants: ItemDefinitionVariantInfo[]
  hasImage: boolean
  items: VisualDefinitionItem[]
  schools: VisualDefinitionSchoolInfo[]
  embroideries: VisualDefinitionEmbroideryInfo[]
}

export async function getProductVisualDefinitions(
  productId: number,
): Promise<VisualDefinitionGroup[]> {
  return apiClient.get<VisualDefinitionGroup[]>(
    `${BASE}/${productId}/visual-definitions`,
  )
}
