import { isRetryable } from '../api/http';
import type { KeyValueStore } from '../storage/stores';
import type { BoyahaneApi, CompleteStepBody } from './BoyahaneApi';

const OUTBOX_KEY = 'boyahane.outbox';

/** Sunucuya ulaşmamış adım bitirme. `requestId` sayesinde yeniden gönderim ikinci kez işlenmez. */
export interface PendingCompletion {
  batchId: number;
  batchNo: string;
  stepId: number;
  body: CompleteStepBody;
}

/** Wi-Fi kesilse ya da uygulama kapansa bile girilen adım sonucu kaybolmaz; bağlantı gelince sırayla gönderilir. */
export class CompletionOutbox {
  constructor(private readonly store: KeyValueStore) {}

  async list(): Promise<PendingCompletion[]> {
    const raw = await this.store.get(OUTBOX_KEY);
    if (!raw) {
      return [];
    }

    try {
      return JSON.parse(raw) as PendingCompletion[];
    } catch {
      return [];
    }
  }

  async add(item: PendingCompletion): Promise<void> {
    await this.save([...(await this.list()), item]);
  }

  /** Ulaşılamayan sunucuda durur; sunucunun reddettiği işlem kuyruktan çıkar ve mesajı döner. */
  async flush(api: BoyahaneApi): Promise<{ sent: number; rejected: string[]; pending: number }> {
    let sent = 0;
    const rejected: string[] = [];
    for (const item of await this.list()) {
      try {
        await api.completeStep(item.batchId, item.stepId, item.body);
        sent += 1;
      } catch (error) {
        if (isRetryable(error)) {
          break;
        }
        rejected.push(`${item.batchNo}: ${error instanceof Error ? error.message : 'Reddedildi.'}`);
      }

      await this.save((await this.list()).filter(other => other.body.requestId !== item.body.requestId));
    }

    return { sent, rejected, pending: (await this.list()).length };
  }

  private save(items: PendingCompletion[]): Promise<void> {
    return items.length === 0 ? this.store.remove(OUTBOX_KEY) : this.store.set(OUTBOX_KEY, JSON.stringify(items));
  }
}
