import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { useEffect, useRef } from 'react';
import type { TabletClient } from '../api/TabletApi';

/** Sunucunun tablet hub'ı (ERP.Api.Realtime.TabletHub). */
const TABLET_HUB_PATH = '/hubs/tablet';

/** Art arda gelen bildirimler bu aralıkta tek yenilemeye indirgenir. */
const MIN_REFRESH_INTERVAL_MS = 1000;

/** Yeniden bağlanma beklemesi: 2 sn'den başlar, ikiye katlanır, en fazla 60 sn. */
const reconnectDelayMs = (attempt: number) => Math.min(2000 * 2 ** Math.min(attempt, 5), 60_000);

/**
 * Tabletin makinesinde sunucuda bir değişiklik olunca (ör. web'den elle adet bildirimi, arıza kapatma) `onChanged`
 * çağrılır; ekran veriyi API'den yeniden okur. Hub veri taşımaz. Bağlantı koparsa kendiliğinden yeniden kurulur;
 * o sırada istasyonun düzenli yoklaması (20 sn) yedek olarak sürer.
 */
export function useTabletLive(client: TabletClient | null, onChanged: () => void): void {
  const callback = useRef(onChanged);
  callback.current = onChanged;

  useEffect(() => {
    if (!client) {
      return;
    }

    let stopped = false;
    let retryTimer: ReturnType<typeof setTimeout> | null = null;
    let refreshTimer: ReturnType<typeof setTimeout> | null = null;
    let lastRefresh = 0;

    // İlk bildirim hemen işlenir; aralık dolmadan gelenler aralığın sonunda tek yenilemeye birleşir.
    const refresh = () => {
      const wait = lastRefresh + MIN_REFRESH_INTERVAL_MS - Date.now();
      if (wait <= 0) {
        lastRefresh = Date.now();
        callback.current();
      } else if (refreshTimer === null) {
        refreshTimer = setTimeout(() => {
          refreshTimer = null;
          lastRefresh = Date.now();
          callback.current();
        }, wait);
      }
    };

    const connection = new HubConnectionBuilder()
      .withUrl(`${client.origin}${TABLET_HUB_PATH}`, { accessTokenFactory: () => client.liveToken() })
      .withAutomaticReconnect({ nextRetryDelayInMilliseconds: context => reconnectDelayMs(context.previousRetryCount) })
      .configureLogging(LogLevel.None)
      .build();

    connection.on('Changed', refresh);
    // Kopukluk sırasında kaçan değişiklik olabilir.
    connection.onreconnected(refresh);

    // İlk bağlantı otomatik yeniden denenmez; kurulana kadar aynı bekleme düzeniyle denenir.
    const start = async (attempt: number) => {
      try {
        await connection.start();
        if (attempt > 0 && !stopped) {
          refresh();
        }
      } catch {
        if (!stopped) {
          retryTimer = setTimeout(() => void start(attempt + 1), reconnectDelayMs(attempt));
        }
      }
    };

    void start(0);

    return () => {
      stopped = true;
      if (retryTimer !== null) {
        clearTimeout(retryTimer);
      }
      if (refreshTimer !== null) {
        clearTimeout(refreshTimer);
      }
      if (connection.state !== HubConnectionState.Disconnected) {
        void connection.stop();
      }
    };
  }, [client]);
}
