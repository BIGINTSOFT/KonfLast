import { NO_CARD, normalizeUid } from './RfidReader';
import type { RfidReader } from './RfidReader';

/**
 * Klavye gibi davranan okuyucular (USB / Bluetooth HID): kart okununca kart numarasını yazar ve Enter gönderir.
 * Satırı Android tarafı toplar; WedgeInput `feed` ile buraya aktarır.
 */
export class KeyboardWedgeReader implements RfidReader {
  readonly name = 'Klavye emülasyonlu okuyucu (USB / Bluetooth HID)';

  private onRead: ((uid: string) => void) | null = null;

  /** @param onInvalid Okunan satır geçerli bir kart numarası değilse çağrılır (bozuk okuma). */
  /** @param normalize Okunan satırı geçerli koda çevirir; varsayılanı kart numarasıdır. */
  constructor(
    private readonly onInvalid: (raw: string) => void = () => {},
    private readonly normalize: (raw: string) => string | null = normalizeUid,
  ) {}

  async start(onRead: (uid: string) => void) {
    this.onRead = onRead;
  }

  async stop() {
    this.onRead = null;
  }

  /** Okuyucunun yazdığı bir satırı işler. */
  feed(raw: string): void {
    // Okuyucu kart çekilince "NOCARD" yazar; okuma değildir, hata da gösterilmez.
    if (!this.onRead || raw.trim().length === 0 || raw.trim().toUpperCase() === NO_CARD) {
      return;
    }

    const uid = this.normalize(raw);
    if (uid) {
      this.onRead(uid);
    } else {
      this.onInvalid(raw);
    }
  }
}
