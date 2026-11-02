import { useAppSelector } from '../../store/hooks'

/** Canlı bağlantı varken seyrek bir güvenlik yoklaması yeter (kaçan bildirime karşı). */
const CONNECTED_POLL_MS = 120_000

/**
 * Canlı izlenen sorgular için yoklama aralığı: bağlantı varken veriyi bildirimler yeniler; bağlantı
 * yokken ekran <paramref name="fallbackMs"/> aralığıyla kendini yenilemeye devam eder.
 */
export function useLivePolling(fallbackMs = 30_000): number {
  const connected = useAppSelector((state) => state.live.status === 'connected')
  return connected ? Math.max(CONNECTED_POLL_MS, fallbackMs) : fallbackMs
}
