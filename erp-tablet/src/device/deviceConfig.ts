import type { DeviceCredentials } from '../api/TabletApi';
import type { KeyValueStore, SecretStore } from '../storage/stores';
import { newUuid } from '../util/uuid';
import type { ProjectKey } from './project';

const LEGACY_CONFIG_KEY = 'device.config';
const configKey = (project: ProjectKey) => `device.config.${project}`;
const IDENTIFIER_KEY = 'device.identifier';

/** Eşleştirme sonrası cihazda saklanan ayarlar (cihaz anahtarı ayrıca güvenli depoda durur). */
export interface DeviceConfig {
  origin: string;
  tabletCode: string;
  /** Eşleştirilen makine ya da kontrol noktası; eski kayıtlarda boştur. */
  stationCode?: string;
  stationName?: string;
}

export interface EnrolledDevice {
  config: DeviceConfig;
  credentials: DeviceCredentials;
}

/**
 * Bu kurulumun kimliği: ilk açılışta üretilir ve saklanır. Sunucu aynı fiziksel cihazın iki tablet
 * kaydına bağlanmasını bu kimlikle engeller. Uygulama silinip yeniden kurulursa değişir; o durumda
 * tabletin cihazı yönetim ekranından sıfırlanıp yeniden eşleştirilir.
 */
export async function getDeviceIdentifier(store: KeyValueStore): Promise<string> {
  const existing = await store.get(IDENTIFIER_KEY);
  if (existing) {
    return existing;
  }

  const identifier = `android-${newUuid()}`;
  await store.set(IDENTIFIER_KEY, identifier);
  return identifier;
}

/** Sunucu adresini tek biçime getirir: şeması olan, sonunda "/" olmayan adres. Geçersizse null. */
export function normalizeOrigin(raw: string): string | null {
  const trimmed = raw.trim();
  if (trimmed.length === 0) {
    return null;
  }

  const origin = (/^https?:\/\//i.test(trimmed) ? trimmed : `http://${trimmed}`).replace(/\/+$/, '');
  const port = /:(\d+)$/.exec(origin)?.[1];
  if (port && Number(port) > 65535) {
    return null;
  }

  return /^https?:\/\/[^\s/?#]+$/i.test(origin) ? origin : null;
}

/**
 * Projenin eşleştirmesi. Sunucu adresi sistem ayarlarından gelir: adres değişince yeniden eşleştirme gerekmez.
 */
export async function loadEnrolledDevice(
  store: KeyValueStore,
  secrets: SecretStore,
  project: ProjectKey,
  origin?: string,
): Promise<EnrolledDevice | null> {
  const [raw, deviceKey] = await Promise.all([store.get(configKey(project)), secrets.get()]);
  if (!raw || !deviceKey) {
    return null;
  }

  let stored: DeviceConfig;
  try {
    stored = JSON.parse(raw) as DeviceConfig;
  } catch {
    return null;
  }

  const config = origin ? { ...stored, origin } : stored;
  const deviceIdentifier = await getDeviceIdentifier(store);
  return {
    config,
    credentials: { origin: config.origin, tabletCode: config.tabletCode, deviceKey, deviceIdentifier },
  };
}

export async function saveEnrolledDevice(
  store: KeyValueStore,
  secrets: SecretStore,
  project: ProjectKey,
  config: DeviceConfig,
  deviceKey: string,
): Promise<EnrolledDevice> {
  // Önce anahtar: ayar yazılıp anahtar yazılamazsa cihaz "eşleşmiş ama anahtarsız" kalmasın.
  await secrets.set(deviceKey);
  await store.set(configKey(project), JSON.stringify(config));

  const deviceIdentifier = await getDeviceIdentifier(store);
  return { config, credentials: { origin: config.origin, tabletCode: config.tabletCode, deviceKey, deviceIdentifier } };
}

/**
 * Tek eşleştirme saklayan eski sürümden geçiş: mevcut eşleştirme, tabletin açık olan projesine taşınır.
 */
export async function migrateLegacyDevice(
  store: KeyValueStore,
  legacySecrets: SecretStore,
  secrets: SecretStore,
  project: ProjectKey,
): Promise<void> {
  const [raw, deviceKey] = await Promise.all([store.get(LEGACY_CONFIG_KEY), legacySecrets.get()]);
  if (raw && deviceKey) {
    await secrets.set(deviceKey);
    await store.set(configKey(project), raw);
  }

  if (raw || deviceKey) {
    await store.remove(LEGACY_CONFIG_KEY);
    await legacySecrets.remove();
  }
}
