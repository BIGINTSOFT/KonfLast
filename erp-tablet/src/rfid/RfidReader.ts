/**
 * RFID okuyucu soyutlaması. Uygulamanın geri kalanı yalnızca bu arayüzü bilir;
 * üreticiye özel kod (Android NFC, Bluetooth, USB, vendor SDK) kendi implementasyonunda kalır.
 */
export interface RfidReader {
  /** Tanılama ekranlarında gösterilecek okuyucu adı. */
  readonly name: string;

  /** Okumayı başlatır; her kart okunduğunda normalize edilmiş UID ile `onRead` çağrılır. */
  start(onRead: (uid: string) => void): Promise<void>;

  stop(): Promise<void>;
}

const DIGITS = 10;

/** Okuyucunun kart çekilince yazdığı satır; kart numarası değildir, yok sayılır. */
export const NO_CARD = 'NOCARD';

/**
 * Kart UID'sinin sistemdeki tek biçimi en fazla 10 karakterlik kart numarasıdır (harf ve rakam).
 * Yalnız rakamdan oluşan numaralarda baştaki sıfırlar elle girişte atlanabilir; 10 haneye tamamlanır
 * ("0012629368"). Harfler büyük harfe çevrilir. Geçerli bir kart numarası (1-10 harf ya da rakam)
 * değilse null döner. Sunucudaki kuralla aynıdır.
 */
export function normalizeUid(raw: string): string | null {
  const code = raw.trim().toUpperCase();
  if (code === NO_CARD || !/^[0-9A-Z]{1,10}$/.test(code)) {
    return null;
  }

  return /^[0-9]+$/.test(code) ? code.padStart(DIGITS, '0') : code;
}
