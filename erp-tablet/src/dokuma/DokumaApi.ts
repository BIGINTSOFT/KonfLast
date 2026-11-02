import { TabletClient } from '../api/TabletApi';

export type DokumaStopCategory = 'PAUSE' | 'STOP' | 'BREAKDOWN' | 'PLANNED';
export type DokumaPauseKind = 'BREAK' | 'PAUSE' | 'WAITING_FOR_BEAM';
export type DokumaOperatorRole = 'WEAVER' | 'SUPERVISOR' | 'MAINTENANCE';

export interface DokumaDueBreak {
  breakScheduleId: number;
  name: string;
  startTime: string;
  endTime: string;
  plannedMinutes: number;
  canStartNow: boolean;
  hint: string | null;
}

export interface DokumaLoom {
  loomId: number;
  loomCode: string;
  loomName: string;
  beamId: number | null;
  beamNo: string | null;
  orderNo: string | null;
  customerName: string | null;
  articleCode: string | null;
  articleName: string | null;
  expectedFabricMeters: number | null;
  wovenMeters: number | null;
  rollCount: number;
  openStopCategory: DokumaStopCategory | null;
  openStopReason: string | null;
  openStopSince: string | null;
}

export interface DokumaStopReason {
  id: number;
  name: string;
  category: DokumaStopCategory;
}

export interface DokumaContext {
  tabletId: number;
  tabletCode: string;
  serverTime: string;
  /** Tabletin bağlı olduğu tezgâh; boşsa salon tabletidir. */
  loomId: number | null;
  operator: { id: number; code: string; name: string; role: DokumaOperatorRole; loginAt: string } | null;
  pause: { kind: DokumaPauseKind; reasonName: string | null; breakName: string | null; startedAt: string } | null;
  /** Zamanı gelmiş, henüz çıkılmamış planlı mola. */
  dueBreak: DokumaDueBreak | null;
  /** Boşta sorusunun süresi; 0 ise sorulmaz. */
  idlePromptMinutes: number;
  looms: DokumaLoom[];
  /** Operatörün "Ara ver" nedenleri. */
  pauseReasons: DokumaStopReason[];
  /** Tezgâh duruş ve arıza nedenleri. */
  stopReasons: DokumaStopReason[];
}

export interface CutRollBody {
  requestId: string;
  cutOn: string;
  meters: number;
  kilograms?: number;
  notes?: string;
}

/** Dokuma istasyonunun sunucu çağrıları; token yalnızca Dokuma tablet uçlarında geçer. */
export class DokumaApi extends TabletClient {
  protected readonly tokenPath = '/dokuma/tablet-auth/token';

  getContext() {
    return this.call<DokumaContext>('/dokuma/tablet/context');
  }

  async heartbeat() {
    await this.call<null>('/dokuma/tablet/heartbeat', { method: 'POST', body: {} });
  }

  login(uid: string) {
    return this.call<DokumaContext>('/dokuma/tablet/session/login', { method: 'POST', body: { uid } });
  }

  logout() {
    return this.call<DokumaContext>('/dokuma/tablet/session/logout', { method: 'POST', body: {} });
  }

  /** Oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş. */
  async getSessionPhoto(): Promise<string | null> {
    const photo = await this.call<{ contentType: string; dataBase64: string } | null>('/dokuma/tablet/session/photo');
    return photo ? `data:${photo.contentType};base64,${photo.dataBase64}` : null;
  }

  /** Mola serbesttir; ara yetkili kartı ister. */
  pause(kind: DokumaPauseKind, reasonId: number | null, authorizerUid?: string) {
    return this.call<DokumaContext>('/dokuma/tablet/session/pause', { method: 'POST', body: { kind, reasonId, authorizerUid } });
  }

  /** Moladan dönüş serbesttir; aradan dönüş yetkili kartı ister. */
  resume(authorizerUid?: string) {
    return this.call<DokumaContext>('/dokuma/tablet/session/resume', { method: 'POST', body: { authorizerUid } });
  }

  cutRoll(beamId: number, body: CutRollBody) {
    return this.call<{ rollNo: string; meters: number }>(`/dokuma/tablet/beams/${beamId}/rolls`, { method: 'POST', body });
  }

  /** Arıza yetkili kartı ister; diğer duruşları oturumdaki operatör bildirir. */
  startStop(loomId: number, reasonId: number, authorizerUid?: string) {
    return this.call<DokumaContext>(`/dokuma/tablet/looms/${loomId}/stop`, { method: 'POST', body: { reasonId, authorizerUid } });
  }

  /** Arıza yetkili kartıyla kapanır; diğer duruşları oturumdaki personel kapatır. */
  endStop(loomId: number, authorizerUid?: string) {
    return this.call<DokumaContext>(`/dokuma/tablet/looms/${loomId}/resume`, { method: 'POST', body: { authorizerUid } });
  }
}
