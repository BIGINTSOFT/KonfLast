import { REQUEST_TIMEOUT_MS } from './config';

/** Sunucunun isteği reddetmesi: iş kuralı, doğrulama ya da yetki hatası. Mesaj kullanıcıya gösterilebilir. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  /** Sunucu tarafı geçici hata: aynı istek daha sonra yeniden denenebilir. */
  get isTransient(): boolean {
    return this.status >= 500;
  }
}

/** Sunucuya ulaşılamadı (Wi-Fi kesintisi, zaman aşımı). Fabrikada olağan bir durumdur. */
export class NetworkError extends Error {
  constructor() {
    super('Sunucuya ulaşılamıyor.');
    this.name = 'NetworkError';
  }
}

/** İstek yeniden denenirse sonuç değişebilir mi (ağ kesintisi ya da sunucu tarafı geçici hata). */
export function isRetryable(error: unknown): boolean {
  return error instanceof NetworkError || (error instanceof ApiError && error.isTransient);
}

interface Envelope<T> {
  success: boolean;
  data: T;
  error: { code: string; message: string } | null;
}

export interface RequestOptions {
  method?: 'GET' | 'POST';
  body?: unknown;
  token?: string;
}

/**
 * API çağrısı yapar ve yanıt zarfını açar. Başarısızsa <see cref="ApiError"/>, sunucuya ulaşılamazsa
 * <see cref="NetworkError"/> fırlatır.
 */
export async function request<T>(origin: string, path: string, options: RequestOptions = {}): Promise<T> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  let response: Response;
  try {
    response = await fetch(`${origin}/api/v1${path}`, {
      method: options.method ?? 'GET',
      headers: {
        Accept: 'application/json',
        ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
      },
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      signal: controller.signal,
    });
  } catch {
    throw new NetworkError();
  } finally {
    clearTimeout(timer);
  }

  let envelope: Envelope<T> | null = null;
  try {
    envelope = (await response.json()) as Envelope<T>;
  } catch {
    // Zarf yok: araya giren bir proxy ya da beklenmeyen sunucu yanıtı.
  }

  if (response.ok && envelope?.success) {
    return envelope.data;
  }

  throw new ApiError(
    response.status,
    envelope?.error?.code ?? 'UNEXPECTED_RESPONSE',
    envelope?.error?.message ?? `Sunucu beklenmeyen bir yanıt verdi (${response.status}).`,
  );
}
