import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface SchoolProductItem {
  productId: number
  name: string | null
  serialCount: number | null
  categoryName: string
  schoolNames: string[]
  schoolCount: number
  schoolCommonProduct: boolean
}

export interface SchoolWithProducts {
  schoolId: number
  name: string
  schoolLevelName: string
  address: string
  ciudad: string | null
  state: string | null
  productCount: number
  products: SchoolProductItem[]
}

// ── API endpoints ────────────────────────────────────────

const BASE = '/api/supplyprocess'

export async function getSchoolsWithProducts(
  minSchoolCount: number,
): Promise<SchoolWithProducts[]> {
  return apiClient.get<SchoolWithProducts[]>(
    `${BASE}/schools-with-products?minSchoolCount=${minSchoolCount}`,
  )
}

export async function getSchoolCommonProducts(
  minSchoolCount: number,
): Promise<SchoolProductItem[]> {
  return apiClient.get<SchoolProductItem[]>(
    `${BASE}/school-common-products?minSchoolCount=${minSchoolCount}`,
  )
}
