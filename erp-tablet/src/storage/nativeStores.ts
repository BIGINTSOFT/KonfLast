import AsyncStorage from '@react-native-async-storage/async-storage';
import * as Keychain from 'react-native-keychain';
import type { ProjectKey } from '../device/project';
import type { KeyValueStore, SecretStore } from './stores';

export const deviceStore: KeyValueStore = {
  get: key => AsyncStorage.getItem(key),
  set: (key, value) => AsyncStorage.setItem(key, value),
  remove: key => AsyncStorage.removeItem(key),
};

const KEYCHAIN_SERVICE = 'erp-tablet-device-key';
const KEYCHAIN_USER = 'device';

function keychainStore(service: string): SecretStore {
  return {
    async get() {
      const entry = await Keychain.getGenericPassword({ service });
      return entry ? entry.password : null;
    },
    async set(secret) {
      await Keychain.setGenericPassword(KEYCHAIN_USER, secret, { service });
    },
    async remove() {
      await Keychain.resetGenericPassword({ service });
    },
  };
}

/**
 * Cihaz anahtarı Android Keystore ile şifrelenerek saklanır; uygulama verisi yedeklerine girmez.
 * Her projenin anahtarı ayrıdır: projeler arası geçişte eşleştirme kaybolmaz.
 */
export const deviceKeyStoreFor = (project: ProjectKey): SecretStore => keychainStore(`${KEYCHAIN_SERVICE}.${project}`);

/** Proje başına anahtardan önceki tek anahtar; yalnızca taşıma için okunur. */
export const legacyDeviceKeyStore: SecretStore = keychainStore(KEYCHAIN_SERVICE);
