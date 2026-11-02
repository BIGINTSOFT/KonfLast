import { isRetryable } from '../api/http';
import type { OperationProgress } from '../api/types';
import type { KeyValueStore } from '../storage/stores';

const OUTBOX_KEY = 'outbox.reports';

/** Sunucuya ulaşmamış bir adet bildirimi. `requestId` sayesinde yeniden gönderim ikinci kez işlenmez. */
export interface PendingReport {
  requestId: string;
  bundleOperationId: number;
  bundleNo: string;
  operationName: string;
  producedQuantity: number;
  rejectedQuantity: number;
  /** Bildirimin tablette girildiği an (yalnızca bilgi amaçlı; üretim zamanı sunucu saatidir). */
  createdAt: string;
}

export interface FlushResult {
  sent: { report: PendingReport; progress: OperationProgress }[];
  /** Sunucunun kalıcı olarak reddettiği bildirimler; kuyruktan çıkarılır. */
  rejected: { report: PendingReport; error: unknown }[];
  /** Hâlâ gönderilmeyi bekleyen bildirim sayısı. */
  pending: number;
}

/**
 * Adet bildirimlerinin kalıcı gönderim kuyruğu. Wi-Fi kesilse ya da uygulama kapansa bile girilen adet
 * kaybolmaz; bağlantı gelince girildiği sırayla gönderilir.
 */
export class ReportOutbox {
  constructor(private readonly store: KeyValueStore) {}

  async list(): Promise<PendingReport[]> {
    const raw = await this.store.get(OUTBOX_KEY);
    if (!raw) {
      return [];
    }

    try {
      return JSON.parse(raw) as PendingReport[];
    } catch {
      return [];
    }
  }

  async add(report: PendingReport): Promise<void> {
    const reports = await this.list();
    if (!reports.some(r => r.requestId === report.requestId)) {
      await this.save([...reports, report]);
    }
  }

  /**
   * Bekleyen bildirimleri sırayla gönderir. Ulaşılamayan sunucuda durur (sıra korunur); sunucunun
   * reddettiği bildirim kuyruktan çıkarılır ve sonuçta bildirilir.
   */
  async flush(send: (report: PendingReport) => Promise<OperationProgress>): Promise<FlushResult> {
    const result: FlushResult = { sent: [], rejected: [], pending: 0 };

    for (const report of await this.list()) {
      try {
        const progress = await send(report);
        result.sent.push({ report, progress });
      } catch (error) {
        if (isRetryable(error)) {
          break;
        }
        result.rejected.push({ report, error });
      }

      await this.remove(report.requestId);
    }

    result.pending = (await this.list()).length;
    return result;
  }

  private async remove(requestId: string): Promise<void> {
    await this.save((await this.list()).filter(r => r.requestId !== requestId));
  }

  private save(reports: PendingReport[]): Promise<void> {
    return reports.length === 0 ? this.store.remove(OUTBOX_KEY) : this.store.set(OUTBOX_KEY, JSON.stringify(reports));
  }
}
