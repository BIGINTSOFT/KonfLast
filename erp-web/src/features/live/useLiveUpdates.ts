import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useEffect } from 'react'
import { baseApi } from '../../api/baseApi'
import { PRODUCTION_HUB_URL } from '../../api/config'
import { store } from '../../store'
import { useAppDispatch, useAppSelector } from '../../store/hooks'
import { liveStatusChanged } from './liveSlice'
import { allLiveTags, createThrottledCollector, reconnectDelayMs, tagsForTopics } from './liveUpdates'
import type { LiveTag } from './liveUpdates'

/** Ekranların en sık yenilenme aralığı; sunucu da bildirimleri birleştirir. */
const REFRESH_INTERVAL_MS = 2000

/**
 * Oturum açıkken üretim hub'ına bağlanır. Hub yalnızca "hangi alan değişti" der; bu kanca ilgili
 * RTK Query etiketlerini geçersiz kılar ve açık ekranlar veriyi API'den yeniden okur. İş kuralı ya da
 * veri hub üzerinden taşınmaz. Bağlantı koparsa kendiliğinden yeniden kurulur; o sırada ekranlar
 * düzenli yoklamaya (polling) döner.
 */
export function useLiveUpdates() {
  const dispatch = useAppDispatch()
  const authenticated = useAppSelector((state) => state.auth.status === 'authenticated')

  useEffect(() => {
    if (!authenticated) {
      return
    }

    let stopped = false
    let retryTimer: ReturnType<typeof setTimeout> | null = null

    const invalidate = (tags: LiveTag[]) => {
      if (tags.length > 0) {
        dispatch(baseApi.util.invalidateTags(tags))
      }
    }
    const collector = createThrottledCollector<LiveTag>(invalidate, REFRESH_INTERVAL_MS)

    const connection = new HubConnectionBuilder()
      .withUrl(PRODUCTION_HUB_URL, {
        // Kimlik token ile taşınır; çerez gönderilmez (farklı origin'de CORS kimlik bilgisi gerekmez).
        withCredentials: false,
        // Her (yeniden) bağlanmada güncel token okunur. Token burada yenilenmez: süresi dolmuşsa bağlantı
        // reddedilir, ekranların yoklama istekleri oturumu olağan yoldan yeniler ve sonraki deneme başarılı olur.
        accessTokenFactory: () => store.getState().auth.accessToken ?? '',
      })
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: (context) => reconnectDelayMs(context.previousRetryCount) })
      .configureLogging(LogLevel.Error)
      .build()

    connection.on('Changed', (message: { topics: string[] }) => collector.add(tagsForTopics(message.topics)))
    connection.onreconnecting(() => dispatch(liveStatusChanged('reconnecting')))
    connection.onreconnected(() => {
      dispatch(liveStatusChanged('connected'))
      // Kopukluk sırasında kaçan bildirimler olabilir: canlı verinin tamamı yenilenir.
      invalidate(allLiveTags)
    })
    connection.onclose(() => {
      if (!stopped) {
        dispatch(liveStatusChanged('disconnected'))
      }
    })

    // İlk bağlantı otomatik yeniden denenmez; kurulana kadar aynı bekleme düzeniyle denenir.
    const start = async (attempt: number) => {
      dispatch(liveStatusChanged('connecting'))
      try {
        await connection.start()
        if (!stopped) {
          dispatch(liveStatusChanged('connected'))
          if (attempt > 0) {
            invalidate(allLiveTags)
          }
        }
      } catch {
        if (!stopped) {
          dispatch(liveStatusChanged('disconnected'))
          retryTimer = setTimeout(() => void start(attempt + 1), reconnectDelayMs(attempt))
        }
      }
    }

    void start(0)

    return () => {
      stopped = true
      if (retryTimer !== null) {
        clearTimeout(retryTimer)
      }
      collector.cancel()
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop()
      }
      dispatch(liveStatusChanged('disconnected'))
    }
  }, [authenticated, dispatch])
}
