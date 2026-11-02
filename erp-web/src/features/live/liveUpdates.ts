/** Sunucunun duyurduğu değişiklik alanları (ProductionHub "Changed" mesajı). */
export type LiveTopic = 'MACHINES' | 'SESSIONS' | 'PRODUCTION' | 'QUALITY' | 'PLANNING' | 'STOCK'

export type LiveStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected'

/**
 * Yalnızca okunan, canlı izlenen verilerin etiketleri. Düzenleme formlarının dayandığı etiketler
 * (sipariş, bant, ana veri) bilinçli olarak yer almaz: başkasının yaptığı bir değişiklik, kullanıcının
 * o an doldurduğu formu yeniden yükleyip girdisini silmemelidir.
 */
export type LiveTag = 'LiveDashboard' | 'ProductionTracking' | 'MachineStatus' | 'WorkSession' | 'Bundle' | 'Quality' | 'Stock' | 'Planning'

const tagsByTopic: Record<LiveTopic, LiveTag[]> = {
  MACHINES: ['LiveDashboard', 'ProductionTracking', 'MachineStatus'],
  SESSIONS: ['LiveDashboard', 'ProductionTracking', 'WorkSession'],
  PRODUCTION: ['LiveDashboard', 'ProductionTracking', 'Bundle'],
  QUALITY: ['LiveDashboard', 'Quality'],
  PLANNING: ['LiveDashboard', 'Planning'],
  STOCK: ['Stock'],
}

export const allLiveTags: LiveTag[] = [...new Set(Object.values(tagsByTopic).flat())]

/** Değişen alanların yenilenmesi gereken önbellek etiketleri (tekrarsız). Bilinmeyen alan yok sayılır. */
export function tagsForTopics(topics: readonly string[]): LiveTag[] {
  return [...new Set(topics.flatMap((topic) => tagsByTopic[topic as LiveTopic] ?? []))]
}

/**
 * Sık gelen bildirimleri seyreltir: ilki hemen işlenir, sonraki `intervalMs` içinde gelenler birleştirilip
 * sürenin sonunda tek seferde işlenir. Yoğun üretimde ekranın saniyede defalarca yenilenmesini önler.
 */
export function createThrottledCollector<T>(flush: (items: T[]) => void, intervalMs: number) {
  const pending = new Set<T>()
  let timer: ReturnType<typeof setTimeout> | null = null

  const open = () => {
    timer = setTimeout(() => {
      timer = null
      if (pending.size > 0) {
        const items = [...pending]
        pending.clear()
        flush(items)
        open()
      }
    }, intervalMs)
  }

  return {
    add(items: readonly T[]) {
      if (timer === null) {
        flush([...new Set(items)])
        open()
      } else {
        items.forEach((item) => pending.add(item))
      }
    },
    cancel() {
      if (timer !== null) {
        clearTimeout(timer)
        timer = null
      }
      pending.clear()
    },
  }
}

/** Yeniden bağlanma bekleme süresi: 1 sn'den başlar, her denemede ikiye katlanır, en fazla 30 sn. */
export function reconnectDelayMs(previousRetryCount: number): number {
  return Math.min(1000 * 2 ** Math.min(previousRetryCount, 5), 30_000)
}

export const liveStatusPresentation: Record<LiveStatus, { label: string; icon: string; tone: string }> = {
  connected: { label: 'Canlı', icon: '●', tone: 'ok' },
  connecting: { label: 'Bağlanıyor…', icon: '◌', tone: 'neutral' },
  reconnecting: { label: 'Yeniden bağlanıyor…', icon: '◌', tone: 'warning' },
  disconnected: { label: 'Canlı bağlantı yok', icon: '○', tone: 'danger' },
}
