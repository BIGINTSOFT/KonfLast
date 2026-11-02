import { API_BASE_URL } from '../api/config'
import { toRequestError } from '../api/envelope'
import { store } from '../store'
import { refreshSession } from './session'

function request(path: string): Promise<Response> {
  const token = store.getState().auth.accessToken
  return fetch(`${API_BASE_URL}${path}`, { headers: token ? { Authorization: `Bearer ${token}` } : {} })
}

/**
 * API'den bir dosya indirir ve tarayıcıya kaydettirir. Dosya uçları JSON zarfı döndürmediği için RTK Query
 * dışında çağrılır; oturum süresi dolmuşsa bir kez yenilenip yeniden denenir. Hata, diğer isteklerle aynı
 * biçimde (RequestError) fırlatılır.
 */
export async function downloadFile(path: string, fileName: string): Promise<void> {
  let response: Response
  try {
    response = await request(path)
    if (response.status === 401 && (await refreshSession(store.dispatch))) {
      response = await request(path)
    }
  } catch {
    throw toRequestError('FETCH_ERROR', undefined)
  }

  if (!response.ok) {
    throw toRequestError(response.status, await response.json().catch(() => undefined))
  }

  const url = URL.createObjectURL(await response.blob())
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.append(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
