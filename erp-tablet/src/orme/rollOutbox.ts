import { isRetryable } from '../api/http';
import type { KeyValueStore } from '../storage/stores';
import type { CutRollBody, OrmeApi } from './OrmeApi';

const OUTBOX_KEY = 'orme.outbox';

/** Sunucuya ulaşmamış top kaydı. `requestId` sayesinde yeniden gönderim ikinci top açmaz. */
export interface PendingRoll {
  knitOrderId: number;
  knitOrderNo: string;
  body: CutRollBody;
}

/** Wi-Fi kesilse ya da uygulama kapansa bile girilen top kaybolmaz; bağlantı gelince sırayla gönderilir. */
export class RollOutbox {
  constructor(private readonly store: KeyValueStore) {}

  async list(): Promise<PendingRoll[]> {
    const raw = await this.store.get(OUTBOX_KEY);
    if (!raw) {
      return [];
    }

    try {
      return JSON.parse(raw) as PendingRoll[];
    } catch {
      return [];
    }
  }

  async add(item: PendingRoll): Promise<void> {
    await this.save([...(await this.list()), item]);
  }

  /** Ulaşılamayan sunucuda durur; sunucunun reddettiği kayıt kuyruktan çıkar ve mesajı döner. */
  async flush(api: OrmeApi): Promise<{ sent: number; rejected: string[]; pending: number }> {
    let sent = 0;
    const rejected: string[] = [];
    for (const item of await this.list()) {
      try {
        await api.cutRoll(item.knitOrderId, item.body);
        sent += 1;
      } catch (error) {
        if (isRetryable(error)) {
          break;
        }
        rejected.push(`${item.knitOrderNo}: ${error instanceof Error ? error.message : 'Reddedildi.'}`);
      }

      await this.save((await this.list()).filter(other => other.body.requestId !== item.body.requestId));
    }

    return { sent, rejected, pending: (await this.list()).length };
  }

  private save(items: PendingRoll[]): Promise<void> {
    return items.length === 0 ? this.store.remove(OUTBOX_KEY) : this.store.set(OUTBOX_KEY, JSON.stringify(items));
  }
}
