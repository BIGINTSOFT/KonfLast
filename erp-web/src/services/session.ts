import type { UnknownAction } from '@reduxjs/toolkit'
import { API_BASE_URL } from '../api/config'
import { credentialsReceived, sessionCleared } from '../features/auth/authSlice'
import type { ApiEnvelope, AuthResponse } from '../types/api'
import { tokenStorage } from './tokenStorage'

type Dispatch = (action: UnknownAction) => unknown

const REFRESH_LOCK = 'erp-auth-refresh'

async function refresh(dispatch: Dispatch): Promise<boolean> {
  const refreshToken = tokenStorage.get()
  if (!refreshToken) {
    dispatch(sessionCleared())
    return false
  }

  try {
    const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    })

    if (response.ok) {
      const envelope = (await response.json()) as ApiEnvelope<AuthResponse>
      if (envelope.data) {
        tokenStorage.set(envelope.data.refreshToken, envelope.data.rememberMe)
        dispatch(credentialsReceived(envelope.data))
        return true
      }
    }

    // Sunucu token'ı reddettiyse artık işe yaramaz; geçici hatalarda (ağ, 5xx) saklanır.
    if (response.status === 401) {
      tokenStorage.clear()
    }
  } catch {
    // Ağ hatası: token saklanır, kullanıcı giriş ekranına düşer.
  }

  dispatch(sessionCleared())
  return false
}

/**
 * Saklanan refresh token ile oturumu yeniler. Refresh token tek kullanımlıktır; iki sekmenin
 * aynı token'ı aynı anda kullanması tüm oturumları kapatır. Bu yüzden yenileme sekmeler
 * arasında Web Locks ile sıraya alınır ve token kilit alındıktan sonra okunur.
 */
export function refreshSession(dispatch: Dispatch): Promise<boolean> {
  if (typeof navigator !== 'undefined' && navigator.locks) {
    return navigator.locks.request(REFRESH_LOCK, () => refresh(dispatch))
  }
  return refresh(dispatch)
}
