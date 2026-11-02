import type { ApiEnvelope, RequestError } from '../types/api'

const NETWORK_MESSAGE = 'Sunucuya ulaşılamıyor. Bağlantınızı kontrol edin.'
const UNKNOWN_MESSAGE = 'Beklenmeyen bir hata oluştu.'

function isEnvelope(body: unknown): body is ApiEnvelope<unknown> {
  return typeof body === 'object' && body !== null && 'success' in body && 'correlationId' in body
}

/** HTTP durum kodu ve gövdeden istemcinin kullandığı tek tip hatayı üretir. */
export function toRequestError(status: number | string, body: unknown): RequestError {
  if (typeof status !== 'number') {
    return { status: 'NETWORK_ERROR', code: 'NETWORK_ERROR', message: NETWORK_MESSAGE }
  }

  if (isEnvelope(body) && body.error) {
    return {
      status,
      code: body.error.code,
      message: body.error.message,
      details: body.error.details ?? undefined,
      correlationId: body.correlationId,
    }
  }

  return { status, code: 'UNKNOWN_ERROR', message: UNKNOWN_MESSAGE }
}

export function isRequestError(error: unknown): error is RequestError {
  return typeof error === 'object' && error !== null && 'code' in error && 'message' in error && 'status' in error
}

/** API'nin belirli bir form alanı için döndüğü doğrulama mesajları. */
export function fieldErrors(error: unknown, field: string): string[] | undefined {
  return isRequestError(error) ? error.details?.[field] : undefined
}
