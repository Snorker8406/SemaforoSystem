const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || ''

type RequestOptions = Omit<RequestInit, 'body'> & {
  body?: unknown
}

/**
 * Cliente HTTP base para las llamadas al servidor.
 * Maneja serialización JSON, headers y errores de forma centralizada.
 */
async function request<T>(endpoint: string, options: RequestOptions = {}): Promise<T> {
  const { body, headers, ...rest } = options

  const config: RequestInit = {
    ...rest,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...headers,
    },
  }

  if (body) {
    config.body = JSON.stringify(body)
  }

  const response = await fetch(`${API_BASE_URL}${endpoint}`, config)

  if (!response.ok) {
    const errorBody = await response.text()
    let parsed: unknown
    try {
      parsed = JSON.parse(errorBody)
    } catch {
      parsed = errorBody
    }
    throw new ApiError(response.status, response.statusText, parsed)
  }

  // Algunas respuestas pueden no tener body (204 No Content)
  const text = await response.text()
  if (!text) return undefined as T

  return JSON.parse(text) as T
}

/**
 * Error personalizado para respuestas HTTP no exitosas.
 */
export class ApiError extends Error {
  status: number
  statusText: string
  body: unknown

  constructor(status: number, statusText: string, body: unknown) {
    super(`HTTP ${status}: ${statusText}`)
    this.name = 'ApiError'
    this.status = status
    this.statusText = statusText
    this.body = body
  }
}

/**
 * Métodos HTTP de conveniencia.
 */
const apiClient = {
  get: <T>(endpoint: string, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'GET' }),

  post: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'POST', body }),

  put: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'PUT', body }),

  patch: <T>(endpoint: string, body?: unknown, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'PATCH', body }),

  delete: <T>(endpoint: string, options?: RequestOptions) =>
    request<T>(endpoint, { ...options, method: 'DELETE' }),
}

export default apiClient
