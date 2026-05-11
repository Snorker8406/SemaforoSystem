import apiClient from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

export interface ProviderResponse {
  providerId: number
  providerStatusId: number
  providerStatusCode: string
  providerStatusName: string
  legalName: string
  tradeName: string | null
  taxId: string | null
  website: string | null
  notes: string | null
  createdAt: string
  updatedAt: string
  contactsCount: number
  addressesCount: number
  bankAccountsCount: number
}

export interface ProviderContactResponse {
  providerContactId: number
  providerId: number
  name: string
  role: string | null
  email: string | null
  phone: string | null
  cellphone: string | null
  whatsapp: boolean
  isPrimary: boolean
  notes: string | null
  createdAt: string
}

export interface ProviderAddressResponse {
  providerAddressId: number
  providerId: number
  addressType: string
  addressLine: string
  city: string | null
  state: string | null
  postalCode: string | null
  country: string | null
  isPrimary: boolean
  createdAt: string
}

export interface ProviderBankAccountResponse {
  providerBankAccountId: number
  providerId: number
  bankName: string
  accountHolder: string | null
  accountNumber: string | null
  clabe: string | null
  currencyCode: string
  isPrimary: boolean
  notes: string | null
  createdAt: string
}

export interface ProviderDetailResponse extends ProviderResponse {
  contacts: ProviderContactResponse[]
  addresses: ProviderAddressResponse[]
  bankAccounts: ProviderBankAccountResponse[]
}

export interface ProviderStatusOption {
  providerStatusId: number
  code: string
  name: string
  description: string | null
  isActive: boolean
}

export interface CreateProviderRequest {
  providerStatusId: number
  legalName: string
  tradeName?: string | null
  taxId?: string | null
  website?: string | null
  notes?: string | null
}
export type UpdateProviderRequest = CreateProviderRequest

export interface CreateProviderContactRequest {
  name: string
  role?: string | null
  email?: string | null
  phone?: string | null
  cellphone?: string | null
  whatsapp: boolean
  isPrimary: boolean
  notes?: string | null
}
export type UpdateProviderContactRequest = CreateProviderContactRequest

export interface CreateProviderAddressRequest {
  addressType: string
  addressLine: string
  city?: string | null
  state?: string | null
  postalCode?: string | null
  country?: string | null
  isPrimary: boolean
}
export type UpdateProviderAddressRequest = CreateProviderAddressRequest

export interface CreateProviderBankAccountRequest {
  bankName: string
  accountHolder?: string | null
  accountNumber?: string | null
  clabe?: string | null
  currencyCode: string
  isPrimary: boolean
  notes?: string | null
}
export type UpdateProviderBankAccountRequest = CreateProviderBankAccountRequest

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface ProviderQueryParams {
  page?: number
  pageSize?: number
  sortBy?: string
  sortDescending?: boolean
  search?: string
  providerStatusId?: number
  statusCode?: string
  taxId?: string
}

// ── Endpoints ────────────────────────────────────────────

const BASE = '/api/providers'

function toQuery(params: ProviderQueryParams): string {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== '',
  )
  if (entries.length === 0) return ''
  return '?' + new URLSearchParams(entries.map(([k, v]) => [k, String(v)])).toString()
}

export async function getProviders(params: ProviderQueryParams = {}) {
  return apiClient.get<PagedResponse<ProviderResponse>>(`${BASE}${toQuery(params)}`)
}

export async function getProvider(id: number) {
  return apiClient.get<ProviderResponse>(`${BASE}/${id}`)
}

export async function getProviderDetail(id: number) {
  return apiClient.get<ProviderDetailResponse>(`${BASE}/${id}/detail`)
}

export async function createProvider(data: CreateProviderRequest) {
  return apiClient.post<ProviderResponse>(BASE, data)
}

export async function updateProvider(id: number, data: UpdateProviderRequest) {
  return apiClient.put<ProviderResponse>(`${BASE}/${id}`, data)
}

export async function updateProviderStatus(id: number, providerStatusId: number) {
  return apiClient.patch<void>(`${BASE}/${id}/status`, { providerStatusId })
}

export async function deleteProvider(id: number) {
  return apiClient.delete<void>(`${BASE}/${id}`)
}

export async function getProviderStatuses() {
  return apiClient.get<ProviderStatusOption[]>(`${BASE}/statuses`)
}

// ── Contacts ─────────────────────────────────────────────

export async function listProviderContacts(providerId: number) {
  return apiClient.get<ProviderContactResponse[]>(`${BASE}/${providerId}/contacts`)
}
export async function createProviderContact(providerId: number, data: CreateProviderContactRequest) {
  return apiClient.post<ProviderContactResponse>(`${BASE}/${providerId}/contacts`, data)
}
export async function updateProviderContact(providerId: number, contactId: number, data: UpdateProviderContactRequest) {
  return apiClient.put<ProviderContactResponse>(`${BASE}/${providerId}/contacts/${contactId}`, data)
}
export async function deleteProviderContact(providerId: number, contactId: number) {
  return apiClient.delete<void>(`${BASE}/${providerId}/contacts/${contactId}`)
}

// ── Addresses ────────────────────────────────────────────

export async function listProviderAddresses(providerId: number) {
  return apiClient.get<ProviderAddressResponse[]>(`${BASE}/${providerId}/addresses`)
}
export async function createProviderAddress(providerId: number, data: CreateProviderAddressRequest) {
  return apiClient.post<ProviderAddressResponse>(`${BASE}/${providerId}/addresses`, data)
}
export async function updateProviderAddress(providerId: number, addressId: number, data: UpdateProviderAddressRequest) {
  return apiClient.put<ProviderAddressResponse>(`${BASE}/${providerId}/addresses/${addressId}`, data)
}
export async function deleteProviderAddress(providerId: number, addressId: number) {
  return apiClient.delete<void>(`${BASE}/${providerId}/addresses/${addressId}`)
}

// ── Bank accounts ────────────────────────────────────────

export async function listProviderBankAccounts(providerId: number) {
  return apiClient.get<ProviderBankAccountResponse[]>(`${BASE}/${providerId}/bank-accounts`)
}
export async function createProviderBankAccount(providerId: number, data: CreateProviderBankAccountRequest) {
  return apiClient.post<ProviderBankAccountResponse>(`${BASE}/${providerId}/bank-accounts`, data)
}
export async function updateProviderBankAccount(providerId: number, bankId: number, data: UpdateProviderBankAccountRequest) {
  return apiClient.put<ProviderBankAccountResponse>(`${BASE}/${providerId}/bank-accounts/${bankId}`, data)
}
export async function deleteProviderBankAccount(providerId: number, bankId: number) {
  return apiClient.delete<void>(`${BASE}/${providerId}/bank-accounts/${bankId}`)
}
