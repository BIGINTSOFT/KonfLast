import { request } from '../api/http';
import type { ProjectKey } from './project';

/** Tablet sistem ayarlarının parolası; sunucu eşleştirmede kendi ayarındaki parolayla ayrıca doğrular. */
export const SETTINGS_PASSWORD = '271123';

/** Tabletin eşleştirilebileceği istasyon: bir makine ya da makinesiz tablet kaydı (kontrol noktası). */
export interface PairingStation {
  machineId: number | null;
  tabletId: number | null;
  code: string;
  name: string;
  detail: string | null;
  tabletCode: string | null;
  isPaired: boolean;
  isThisDevice: boolean;
}

export interface PairResponse {
  tabletId: number;
  tabletCode: string;
  deviceKey: string;
  stationCode: string;
  stationName: string;
}

/** Projenin eşleştirme uçları; projenin tablet istasyonu yoksa boştur. */
const pairingPaths: Record<ProjectKey, string | null> = {
  Konfeksiyon: '/tablet-auth/pairing',
  Boyahane: '/boyahane/tablet-auth/pairing',
  Dokuma: '/dokuma/tablet-auth/pairing',
  Orme: '/orme/tablet-auth/pairing',
};

export const supportsPairing = (project: ProjectKey): boolean => pairingPaths[project] !== null;

export function fetchStations(
  origin: string,
  project: ProjectKey,
  password: string,
  deviceIdentifier: string,
): Promise<PairingStation[]> {
  return request<PairingStation[]>(origin, `${pairingPaths[project]}/stations`, {
    method: 'POST',
    body: { password, deviceIdentifier },
  });
}

export function pairStation(
  origin: string,
  project: ProjectKey,
  password: string,
  deviceIdentifier: string,
  station: PairingStation,
  takeOver: boolean,
): Promise<PairResponse> {
  return request<PairResponse>(origin, `${pairingPaths[project]}/pair`, {
    method: 'POST',
    body: { password, deviceIdentifier, machineId: station.machineId, tabletId: station.tabletId, takeOver },
  });
}
