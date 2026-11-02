import { ApiError, request } from './http';
import type { RequestOptions } from './http';
import type {
  BundleRead,
  MachineStatusInfo,
  OperationProgress,
  OperatorPhoto,
  PauseType,
  TabletContext,
  TabletSession,
  TokenResponse,
} from './types';

/** Eşleştirilmiş cihazın sunucuya kendini tanıtmak için kullandığı bilgiler. */
export interface DeviceCredentials {
  origin: string;
  tabletCode: string;
  deviceKey: string;
  deviceIdentifier: string;
}

/** Cihaz anahtarı artık geçerli değil (cihaz sıfırlanmış ya da tablet pasife alınmış). */
export const INVALID_DEVICE_CREDENTIALS = 'INVALID_DEVICE_CREDENTIALS';

/** İşlem için yetkili personelin kartı okutulmalı (oturum sahibinin yetkisi yok). */
export const AUTHORIZATION_CARD_REQUIRED = 'AUTHORIZATION_CARD_REQUIRED';

/** Token süresi dolmadan bu kadar önce yenilenir. */
const TOKEN_RENEW_MARGIN_MS = 60_000;

/** İstasyon ekranının sunucudan beklediği işlemler. */
export interface StationApi {
  getContext(): Promise<TabletContext>;
  heartbeat(): Promise<void>;
  /** Makinedeki açık oturum; yoksa null. */
  getSession(): Promise<TabletSession | null>;
  /** Oturumu açık operatörün fotoğrafı (görüntü olarak gösterilebilir data URI); fotoğrafı yoksa null. */
  getOperatorPhoto(): Promise<string | null>;
  /** Okutulan personel kartıyla bu makinede oturum açar. */
  login(uid: string, readId: string): Promise<TabletSession>;
  logout(): Promise<void>;
  /** Planlı molaya çıkma, ara verme (neden zorunlu) ya da demet bekleme. */
  /** `authorizerUid`: oturum sahibinin yetkisi yoksa onaylayan yetkilinin okutulan kart numarası. */
  pause(type: PauseType, reasonId: number | null, authorizerUid?: string | null): Promise<TabletSession>;
  /** Duraklamadan çalışmaya dönüş. */
  resume(authorizerUid?: string | null): Promise<TabletSession>;
  readBundle(uid: string, readId: string): Promise<BundleRead>;
  /** Operasyonu, makinedeki açık oturumun operatörü adına başlatır. */
  startOperation(bundleOperationId: number, requestId: string): Promise<OperationProgress>;
  reportOperation(
    bundleOperationId: number,
    requestId: string,
    producedQuantity: number,
    rejectedQuantity: number,
  ): Promise<OperationProgress>;
  getCurrentOperation(): Promise<OperationProgress | null>;
  /** Arıza bildirir; neden ana verideki arıza nedenlerinden seçilir (tanımlı neden yoksa null). */
  reportBreakdown(reasonId: number | null, authorizerUid?: string | null): Promise<MachineStatusInfo>;
  /** Arızayı kapatır; bakım operatörünün ya da bant şefinin okutulan kart numarası zorunludur. */
  reportRecovery(authorizerUid: string): Promise<MachineStatusInfo>;
}

/**
 * Tablet token'ı ile yapılan çağrılar. Token bellekte tutulur, süresi dolmadan yenilenir; sunucu 401
 * dönerse bir kez yenilenip istek tekrarlanır. Token yalnızca alındığı projenin uçlarında geçer.
 */
export abstract class TabletClient {
  protected abstract readonly tokenPath: string;
  private token: { value: string; expiresAt: number } | null = null;
  private tokenRequest: Promise<string> | null = null;

  constructor(
    private readonly credentials: DeviceCredentials,
    private readonly now: () => number = Date.now,
  ) {}

  protected async call<T>(path: string, options: RequestOptions = {}): Promise<T> {
    try {
      return await request<T>(this.credentials.origin, path, { ...options, token: await this.accessToken() });
    } catch (error) {
      if (!(error instanceof ApiError) || error.status !== 401 || error.code === INVALID_DEVICE_CREDENTIALS) {
        throw error;
      }
    }

    // Token sunucuda geçersiz sayıldı (süre, anahtar değişimi): bir kez yenileyip tekrar dene.
    this.token = null;
    return request<T>(this.credentials.origin, path, { ...options, token: await this.accessToken() });
  }

  /** Eşzamanlı çağrılar tek bir token isteğini paylaşır. */
  private accessToken(): Promise<string> {
    if (this.token && this.token.expiresAt - TOKEN_RENEW_MARGIN_MS > this.now()) {
      return Promise.resolve(this.token.value);
    }

    this.tokenRequest ??= this.requestToken().finally(() => {
      this.tokenRequest = null;
    });
    return this.tokenRequest;
  }

  private async requestToken(): Promise<string> {
    const { origin, tabletCode, deviceKey, deviceIdentifier } = this.credentials;
    const response = await request<TokenResponse>(origin, this.tokenPath, {
      method: 'POST',
      body: { tabletCode, deviceKey, deviceIdentifier },
    });

    this.token = { value: response.accessToken, expiresAt: Date.parse(response.accessTokenExpiresAt) };
    return response.accessToken;
  }
}

/** Konfeksiyon istasyonunun çağrıları. */
export class TabletApi extends TabletClient implements StationApi {
  protected readonly tokenPath = '/tablet-auth/token';

  getContext() {
    return this.call<TabletContext>('/tablet/context');
  }

  async heartbeat() {
    await this.call<null>('/tablet/heartbeat', { method: 'POST', body: {} });
  }

  getSession() {
    return this.call<TabletSession | null>('/production/session');
  }

  async getOperatorPhoto() {
    const photo = await this.call<OperatorPhoto | null>('/production/session/photo');
    return photo ? `data:${photo.contentType};base64,${photo.dataBase64}` : null;
  }

  login(uid: string, readId: string) {
    return this.call<TabletSession>('/production/session/login', { method: 'POST', body: { uid, readId } });
  }

  async logout() {
    await this.call<null>('/production/session/logout', { method: 'POST', body: {} });
  }

  pause(type: PauseType, reasonId: number | null, authorizerUid: string | null = null) {
    return this.call<TabletSession>('/production/session/pause', {
      method: 'POST',
      body: { type, reasonId, authorizerUid: authorizerUid ?? undefined },
    });
  }

  resume(authorizerUid: string | null = null) {
    return this.call<TabletSession>('/production/session/resume', { method: 'POST', body: { authorizerUid } });
  }

  readBundle(uid: string, readId: string) {
    return this.call<BundleRead>('/rfid/bundle/read', { method: 'POST', body: { uid, readId } });
  }

  startOperation(bundleOperationId: number, requestId: string) {
    return this.call<OperationProgress>(`/production/bundle-operations/${bundleOperationId}/start`, {
      method: 'POST',
      body: { requestId },
    });
  }

  reportOperation(bundleOperationId: number, requestId: string, producedQuantity: number, rejectedQuantity: number) {
    return this.call<OperationProgress>(`/production/bundle-operations/${bundleOperationId}/report`, {
      method: 'POST',
      body: { requestId, producedQuantity, rejectedQuantity },
    });
  }

  getCurrentOperation() {
    return this.call<OperationProgress | null>('/production/current');
  }

  reportBreakdown(reasonId: number | null, authorizerUid: string | null = null) {
    return this.call<MachineStatusInfo>('/tablet/machine/breakdown', {
      method: 'POST',
      body: { reasonId, authorizerUid: authorizerUid ?? undefined },
    });
  }

  reportRecovery(authorizerUid: string) {
    return this.call<MachineStatusInfo>('/tablet/machine/recover', { method: 'POST', body: { authorizerUid } });
  }
}
