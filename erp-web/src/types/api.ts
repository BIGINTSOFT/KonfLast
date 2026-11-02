/** Backend'in tüm uçlarda döndüğü standart yanıt zarfı. */
export interface ApiEnvelope<T> {
  success: boolean
  data: T | null
  error: ApiError | null
  correlationId: string
}

export interface ApiError {
  code: string
  message: string
  details?: Record<string, string[]> | null
}

/** İstemci tarafında tek tipe indirgenmiş hata (API hatası veya ağ hatası). */
export interface RequestError {
  status: number | 'NETWORK_ERROR'
  code: string
  message: string
  details?: Record<string, string[]>
  correlationId?: string
}

export interface CurrentUser {
  id: number
  userName: string
  fullName: string
  email: string | null
  roles: string[]
  permissions: string[]
}

export interface AuthResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: CurrentUser
  /** "Beni hatırla" ile açılmış kalıcı oturum. */
  rememberMe: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface User {
  id: number
  userName: string
  fullName: string
  email: string | null
  isActive: boolean
  isLockedOut: boolean
  lastLoginAt: string | null
  createdAt: string
  roles: string[]
}

export interface Permission {
  id: number
  code: string
  module: string
  description: string
  /** Yetkinin ait olduğu proje; çekirdek yetkide null. */
  moduleKey: string | null
}

export type AuditAction = 'CREATED' | 'UPDATED' | 'DELETED'

export interface AuditLog {
  id: number
  entityName: string
  entityId: string
  action: AuditAction
  oldValues: Record<string, unknown> | null
  newValues: Record<string, unknown> | null
  userId: number | null
  userName: string
  ipAddress: string | null
  deviceInfo: string | null
  correlationId: string | null
  createdAt: string
}

export interface Role {
  id: number
  code: string
  name: string
  isSystem: boolean
  /** Rolün ait olduğu proje; bütün projelerde geçerliyse null. */
  moduleKey: string | null
  permissions: string[]
}
