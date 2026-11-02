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

/**
 * Kart UID'sinin sistemdeki tek biçimi okuyucunun yazdığı 10 haneli kart numarasıdır ("0012629368").
 * Baştaki sıfırlar elle girişte atlanabilir; 10 haneye tamamlanır. Geçerli bir kart numarası
 * (1-10 rakam) değilse null döner. Sunucudaki kuralla aynıdır.
 */
export function normalizeUid(raw: string): string | null {
  const digits = raw.trim();
  return /^[0-9]{1,10}$/.test(digits) ? digits.padStart(DIGITS, '0') : null;
}
