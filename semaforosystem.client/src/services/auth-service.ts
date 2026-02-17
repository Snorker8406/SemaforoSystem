import apiClient from '@/lib/api-client'

// ── Tipos ────────────────────────────────────────────────

export interface LoginRequest {
  email: string
  password: string
}

/** Con useCookies=true, Identity no retorna un body con token sino que setea la cookie */
export type LoginResponse = void

export interface AuthErrorResponse {
  type: string
  title: string
  status: number
  errors: Record<string, string[]>
}

export interface UserInfo {
  email: string
  isEmailConfirmed: boolean
}

// ── Endpoints ────────────────────────────────────────────

const AUTH_BASE = '/Auth'

/**
 * Inicia sesión con email y contraseña.
 * Usa cookies de sesión (Set-Cookie) para mantener la autenticación.
 */
export async function login(credentials: LoginRequest): Promise<void> {
  return apiClient.post<void>(
    `${AUTH_BASE}/login?useCookies=true`,
    credentials,
  )
}

/**
 * Registra un nuevo usuario.
 */
export async function register(credentials: LoginRequest): Promise<void> {
  return apiClient.post<void>(`${AUTH_BASE}/register`, credentials)
}

/**
 * Cierra la sesión. El servidor elimina la cookie de autenticación.
 */
export async function logout(): Promise<void> {
  return apiClient.post<void>(`${AUTH_BASE}/logout`)
}

/**
 * Obtiene la información del usuario autenticado.
 * Retorna 401 si no hay sesión válida.
 */
export async function getMe(): Promise<UserInfo> {
  return apiClient.get<UserInfo>(`${AUTH_BASE}/manage/info`)
}
