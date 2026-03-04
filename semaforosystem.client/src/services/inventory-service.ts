import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface SiteLookup {
  siteId: number
  name: string
}

export interface CreateEntryLineRequest {
  inventoryItemDefinitionId?: number | null
  productId?: number | null
  sizeId?: number | null
  variantIds?: number[] | null
  isSerialized?: boolean | null
  quantity: number
  unitCost?: number | null
}

export interface CreateEntryRequest {
  siteId: number
  reference?: string | null
  comments?: string | null
  lines: CreateEntryLineRequest[]
}

export interface SerialItemInfo {
  inventorySerialItemId: number
  barcode: string
  serialNumber: number | null
  status: number
}

export interface InventoryTransactionLineResponse {
  inventoryTransactionLineId: number
  siteId: number
  siteName: string | null
  inventoryItemDefinitionId: number
  skuCode: string | null
  nameSnapshot: string | null
  qtyDelta: number | null
  unitCost: number | null
  unitPrice: number | null
  serialItems: SerialItemInfo[] | null
}

export interface InventoryTransactionResponse {
  inventoryTransactionId: number
  transactionType: string
  transactionDate: string
  reference: string | null
  comments: string | null
  userId: string | null
  lines: InventoryTransactionLineResponse[]
}

// ── API calls ────────────────────────────────────────────

const BASE = '/api/Inventory'

/** Fetch all sites (lightweight lookup). */
export function getSites(): Promise<SiteLookup[]> {
  return apiClient.get<SiteLookup[]>(`${BASE}/sites`)
}

/** Create an ENTRY transaction (goods receipt). */
export function createEntry(request: CreateEntryRequest): Promise<InventoryTransactionResponse> {
  return apiClient.post<InventoryTransactionResponse>(`${BASE}/entries`, request)
}

/** Get a single inventory transaction by id. */
export function getTransaction(id: number): Promise<InventoryTransactionResponse> {
  return apiClient.get<InventoryTransactionResponse>(`${BASE}/transactions/${id}`)
}
