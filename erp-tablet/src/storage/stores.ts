/** Cihazda kalıcı, şifresiz anahtar-değer deposu (ayarlar, gönderilmeyi bekleyen bildirimler). */
export interface KeyValueStore {
  get(key: string): Promise<string | null>;
  set(key: string, value: string): Promise<void>;
  remove(key: string): Promise<void>;
}

/** Cihaz anahtarı gibi gizli değerlerin saklandığı depo (Android Keystore ile şifrelenir). */
export interface SecretStore {
  get(): Promise<string | null>;
  set(secret: string): Promise<void>;
  remove(): Promise<void>;
}
