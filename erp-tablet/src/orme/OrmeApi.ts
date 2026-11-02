import { TabletClient } from '../api/TabletApi';

export type OrmeStopCategory = 'PAUSE' | 'STOP' | 'BREAKDOWN' | 'PLANNED';
export type OrmePauseKind = 'BREAK' | 'PAUSE' | 'WAITING_FOR_ORDER';
export type OrmeOperatorRole = 'KNITTER' | 'SUPERVISOR' | 'MAINTENANCE';

export interface OrmeDueBreak {
  breakScheduleId: number;
  name: string;
  startTime: string;
  endTime: string;
  plannedMinutes: number;
  canStartNow: boolean;
  hint: string | null;
}

export interface OrmeMachine {
  machineId: number;
  machineCode: string;
  machineName: string;
  knitOrderId: number | null;
  knitOrderNo: string | null;
  orderNo: string | null;
  customerName: string | null;
  fabricCode: string | null;
  fabricName: string | null;
  plannedKilograms: number | null;
  /** Emrin tüm makinelerde örülen toplamı. */
  knittedKilograms: number | null;
  /** Plandan kalan. */
  remainingKilograms: number | null;
  /** Emrin bu makinede çıkan topları. */
  rollCount: number;
  openStopCategory: OrmeStopCategory | null;
  openStopReason: string | null;
  openStopSince: string | null;
}

export interface OrmeStopReason {
  id: number;
  name: string;
  category: OrmeStopCategory;
}

export interface OrmeContext {
  tabletId: number;
  tabletCode: string;
  serverTime: string;
  /** Tabletin bağlı olduğu makine; boşsa salon tabletidir. */
  machineId: number | null;
  operator: { id: number; code: string; name: string; role: OrmeOperatorRole; loginAt: string } | null;
  pause: { kind: OrmePauseKind; reasonName: string | null; breakName: string | null; startedAt: string } | null;
  /** Zamanı gelmiş, henüz çıkılmamış planlı mola. */
  dueBreak: OrmeDueBreak | null;
  /** Boşta sorusunun süresi; 0 ise sorulmaz. */
  idlePromptMinutes: number;
  machines: OrmeMachine[];
  /** Operatörün "Ara ver" nedenleri. */
  pauseReasons: OrmeStopReason[];
  /** Makine duruş ve arıza nedenleri. */
  stopReasons: OrmeStopReason[];
}

export interface CutRollBody {
  requestId: string;
  machineId: number;
  cutOn: string;
  /** Makine başında tartılan kilo; zorunlu. */
  kilograms: number;
  /** Ölçüldüyse metre. */
  meters?: number;
  notes?: string;
}

/** Örme istasyonunun sunucu çağrıları; token yalnızca Örme tablet uçlarında geçer. */
export class OrmeApi extends TabletClient {
  protected readonly tokenPath = '/orme/tablet-auth/token';

  getContext() {
    return this.call<OrmeContext>('/orme/tablet/context');
  }

  async heartbeat() {
    await this.call<null>('/orme/tablet/heartbeat', { method: 'POST', body: {} });
  }

  login(uid: string) {
    return this.call<OrmeContext>('/orme/tablet/session/login', { method: 'POST', body: { uid } });
  }

  logout() {
    return this.call<OrmeContext>('/orme/tablet/session/logout', { method: 'POST', body: {} });
  }

  /** Oturumu açık operatörün fotoğrafı; oturum ya da fotoğraf yoksa boş. */
  async getSessionPhoto(): Promise<string | null> {
    const photo = await this.call<{ contentType: string; dataBase64: string } | null>('/orme/tablet/session/photo');
    return photo ? `data:${photo.contentType};base64,${photo.dataBase64}` : null;
  }

  /** Mola serbesttir; ara yetkili kartı ister. */
  pause(kind: OrmePauseKind, reasonId: number | null, authorizerUid?: string) {
    return this.call<OrmeContext>('/orme/tablet/session/pause', { method: 'POST', body: { kind, reasonId, authorizerUid } });
  }

  /** Moladan dönüş serbesttir; aradan dönüş yetkili kartı ister. */
  resume(authorizerUid?: string) {
    return this.call<OrmeContext>('/orme/tablet/session/resume', { method: 'POST', body: { authorizerUid } });
  }

  cutRoll(knitOrderId: number, body: CutRollBody) {
    return this.call<{ rollNo: string; kilograms: number }>(`/orme/tablet/knit-orders/${knitOrderId}/rolls`, { method: 'POST', body });
  }

  /** Arıza yetkili kartı ister; diğer duruşları oturumdaki operatör bildirir. */
  startStop(machineId: number, reasonId: number, authorizerUid?: string) {
    return this.call<OrmeContext>(`/orme/tablet/machines/${machineId}/stop`, { method: 'POST', body: { reasonId, authorizerUid } });
  }

  /** Arıza yetkili kartıyla kapanır; diğer duruşları oturumdaki personel kapatır. */
  endStop(machineId: number, authorizerUid?: string) {
    return this.call<OrmeContext>(`/orme/tablet/machines/${machineId}/resume`, { method: 'POST', body: { authorizerUid } });
  }
}
