import { DEFAULT_API_ORIGIN, REQUEST_TIMEOUT_MS } from './config';

export type ServerStatus = 'checking' | 'online' | 'database-down' | 'offline';

/**
 * Sunucuya ve veritabanına erişilebildiğini /health ucundan kontrol eder.
 * Wi-Fi kesintisi olağan bir durumdur; hata fırlatmaz, 'offline' döner.
 */
export async function checkServer(origin: string = DEFAULT_API_ORIGIN): Promise<ServerStatus> {
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);

  try {
    const response = await fetch(`${origin}/health`, { signal: controller.signal });
    const body = (await response.json()) as { checks?: Record<string, string> };
    return body.checks?.database === 'Healthy' ? 'online' : 'database-down';
  } catch {
    return 'offline';
  } finally {
    clearTimeout(timer);
  }
}
