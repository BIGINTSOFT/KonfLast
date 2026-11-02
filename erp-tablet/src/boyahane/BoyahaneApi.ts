import { request } from '../api/http';
import { TabletClient } from '../api/TabletApi';

export type OperationBehavior = 'STANDARD' | 'DYEING' | 'COLOR_CHECK' | 'FINAL_QUALITY';
export type BatchStepStatus = 'PENDING' | 'IN_PROGRESS' | 'COMPLETED' | 'SKIPPED';
export type ColorCheckResult = 'APPROVED' | 'ADDITION' | 'REDO';
export type FinalQualityResult = 'APPROVED' | 'REJECTED' | 'REWORK';

export interface BoyahaneChemical {
  id: number;
  code: string;
  name: string;
}

export type PauseKind = 'BREAK' | 'PAUSE' | 'MACHINE_BREAKDOWN' | 'WAITING_FOR_BATCH';

export interface DueBreak {
  breakScheduleId: number;
  name: string;
  startTime: string;
  endTime: string;
  plannedMinutes: number;
  canStartNow: boolean;
  hint: string | null;
}

export interface BoyahaneReason {
  id: number;
  name: string;
}

export type BoyahaneOperatorRole = 'OPERATOR' | 'SUPERVISOR' | 'MAINTENANCE';

export interface BoyahaneContext {
  tabletId: number;
  tabletCode: string;
  serverTime: string;
  machine: { id: number; code: string; name: string } | null;
  machineStatus: 'AVAILABLE' | 'BREAKDOWN' | 'MAINTENANCE' | null;
  machineStatusReason: string | null;
  operator: { id: number; code: string; name: string; role: BoyahaneOperatorRole; loginAt: string } | null;
  pause: { kind: PauseKind; reasonName: string | null; breakName: string | null; startedAt: string } | null;
  /** Zamanı gelmiş, henüz çıkılmamış planlı mola. */
  dueBreak: DueBreak | null;
  /** Boşta sorusunun süresi; 0 ise sorulmaz. */
  idlePromptMinutes: number;
  currentBatchId: number | null;
  chemicals: BoyahaneChemical[];
  pauseReasons: BoyahaneReason[];
  breakdownReasons: BoyahaneReason[];
}

export interface BoyahaneBatchStep {
  id: number;
  sequence: number;
  operationName: string;
  behavior: OperationBehavior;
  reworkKind: ColorCheckResult | null;
  status: BatchStepStatus;
  needsMachine: boolean;
  machineCode: string | null;
}

export interface BoyahaneBatchItem {
  id: number;
  orderNo: string;
  customerName: string | null;
  itemNo: string;
  colorName: string | null;
  kilograms: number;
  meters: number | null;
  finalResult: FinalQualityResult | null;
}

export interface BoyahaneRecipeLine {
  dyeChemicalId: number;
  code: string;
  name: string;
  amount: number;
  requiredKilograms: number;
}

export interface BoyahaneBatch {
  id: number;
  batchNo: string;
  machineCode: string;
  liquorRatio: number | null;
  liquorLiters: number | null;
  recipeNo: string | null;
  totalKilograms: number;
  totalMeters: number | null;
  currentStepId: number | null;
  status: 'PLANNED' | 'ON_MACHINE' | 'COMPLETED' | 'CANCELLED';
  items: BoyahaneBatchItem[];
  recipe: BoyahaneRecipeLine[];
  steps: BoyahaneBatchStep[];
}

export interface CompleteStepBody {
  requestId: string;
  consumptions?: { dyeChemicalId: number; actualKilograms: number }[];
  colorResult?: ColorCheckResult;
  itemResults?: { itemId: number; result: FinalQualityResult; kilograms?: number; meters?: number }[];
  note?: string;
}

/** Boyahane istasyonunun sunucu çağrıları; token yalnızca Boyahane tablet uçlarında geçer. */
export class BoyahaneApi extends TabletClient {
  protected readonly tokenPath = '/boyahane/tablet-auth/token';

  getContext() {
    return this.call<BoyahaneContext>('/boyahane/tablet/context');
  }

  async heartbeat() {
    await this.call<null>('/boyahane/tablet/heartbeat', { method: 'POST', body: {} });
  }

  login(uid: string) {
    return this.call<BoyahaneContext>('/boyahane/tablet/session/login', { method: 'POST', body: { uid } });
  }

  logout() {
    return this.call<BoyahaneContext>('/boyahane/tablet/session/logout', { method: 'POST', body: {} });
  }

  pause(kind: 'BREAK' | 'PAUSE' | 'WAITING_FOR_BATCH', reasonId: number | null, authorizerUid?: string) {
    return this.call<BoyahaneContext>('/boyahane/tablet/session/pause', { method: 'POST', body: { kind, reasonId, authorizerUid } });
  }

  resume(authorizerUid?: string) {
    return this.call<BoyahaneContext>('/boyahane/tablet/session/resume', { method: 'POST', body: { authorizerUid } });
  }

  /** Oturumu açık operatörün fotoğrafı (data URI); yoksa boş. */
  async getOperatorPhoto(): Promise<string | null> {
    const photo = await this.call<{ contentType: string; dataBase64: string } | null>('/boyahane/tablet/session/photo');
    return photo ? `data:${photo.contentType};base64,${photo.dataBase64}` : null;
  }

  reportBreakdown(reasonId: number, authorizerUid: string) {
    return this.call<BoyahaneContext>('/boyahane/tablet/machine/breakdown', { method: 'POST', body: { reasonId, authorizerUid } });
  }

  reportRecovery(authorizerUid: string) {
    return this.call<BoyahaneContext>('/boyahane/tablet/machine/recover', { method: 'POST', body: { authorizerUid } });
  }

  readBatch(code: string) {
    return this.call<BoyahaneBatch>('/boyahane/tablet/batches/read', { method: 'POST', body: { code } });
  }

  getBatch(id: number) {
    return this.call<BoyahaneBatch>(`/boyahane/tablet/batches/${id}`);
  }

  startStep(batchId: number, stepId: number, requestId: string) {
    return this.call<BoyahaneBatch>(`/boyahane/tablet/batches/${batchId}/steps/${stepId}/start`, {
      method: 'POST',
      body: { requestId },
    });
  }

  completeStep(batchId: number, stepId: number, body: CompleteStepBody) {
    return this.call<BoyahaneBatch>(`/boyahane/tablet/batches/${batchId}/steps/${stepId}/complete`, {
      method: 'POST',
      body,
    });
  }
}
