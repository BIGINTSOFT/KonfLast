import { ApiError, isRetryable } from '../api/http';
import { AUTHORIZATION_CARD_REQUIRED, INVALID_DEVICE_CREDENTIALS } from '../api/TabletApi';
import type { StationApi } from '../api/TabletApi';
import type {
  BundleRead,
  BundleStep,
  MachineStatus,
  OperationProgress,
  PauseType,
  PerformancePeriod,
  TabletContext,
  TabletPerformance,
  TabletSession,
} from '../api/types';
import type { ReportOutbox } from '../production/outbox';
import { createReadDebouncer } from '../rfid/debounce';
import { newUuid } from '../util/uuid';

export type StationPhase = 'loading' | 'needs-operator' | 'needs-bundle' | 'working';

export interface StationMessage {
  kind: 'error' | 'info' | 'success';
  text: string;
}

/** Yetkili kartı okutularak onaylanan işlemler. */
export type AuthorizedAction = 'pause' | 'resume' | 'breakdown' | 'recover';

/** Yetkili kartının okutulması beklenen işlem. */
export interface PendingAuthorization {
  action: AuthorizedAction;
  /** Seçilmiş ara / arıza nedeni (arıza kapatmada yoktur). */
  reasonId: number | null;
}

/**
 * Okutulan demetin operasyonları operatöre gösteriliyor: rotanın tamamı listelenir, bu makinede
 * başlatılabilen adımlardan (`options`) biri seçilip başlatılır. Başlatılabilen adım yoksa liste yalnızca bilgidir.
 */
export interface OperationChoice {
  bundle: BundleRead;
  options: BundleStep[];
}

export interface StationState {
  /** Sunucudan ilk durum alındı mı (bağlam + makinede süren iş). */
  synced: boolean;
  online: boolean;
  /** Cihaz anahtarı artık geçerli değil: tabletin yeniden eşleştirilmesi gerekiyor. */
  needsEnrollment: boolean;
  busy: boolean;
  context: TabletContext | null;
  operator: { id: number; name: string } | null;
  /** Oturumu açık operatörün fotoğrafı (data URI); yoksa ya da henüz gelmediyse null. */
  operatorPhoto: string | null;
  /** Operatörün sunucudaki oturumu: durumu (çalışıyor / molada / ara ...), süren duraklaması ve zamanı gelen molası. */
  session: TabletSession | null;
  /** Bu makinede süren operasyon. */
  job: OperationProgress | null;
  /** Süren işin sunucudan alındığı an (cihaz saati, ms): geri sayım sunucunun verdiği süreye bundan sonra geçeni ekler. */
  jobSyncedAt: number | null;
  /** Süren işin demet bilgisi (model / renk / beden); uygulama yeniden açıldıysa boş olabilir. */
  bundle: BundleRead | null;
  pendingReports: number;
  message: StationMessage | null;
  /** Doluysa sıradaki kart okutması personel / demet kartı değil, bu işlemin yetkili onayıdır. */
  authorization: PendingAuthorization | null;
  /** Doluysa operatörden başlatacağı operasyonu seçmesi beklenir. */
  operationChoice: OperationChoice | null;
  /**
   * Operatörün boşta görünmeye başladığı an (cihaz saati, ms): oturumu açık ve çalışıyor durumda, ama süren işi
   * yok. Boşta değilse null. Ekran, süre dolunca ne yaptığını sorar.
   */
  idleSince: number | null;
}

const initialState: StationState = {
  synced: false,
  online: true,
  needsEnrollment: false,
  busy: false,
  context: null,
  operator: null,
  operatorPhoto: null,
  session: null,
  job: null,
  jobSyncedAt: null,
  bundle: null,
  pendingReports: 0,
  message: null,
  authorization: null,
  operationChoice: null,
  idleSince: null,
};

export function phaseOf(state: StationState): StationPhase {
  if (!state.synced) {
    return 'loading';
  }
  if (state.job) {
    return 'working';
  }
  return state.operator ? 'needs-bundle' : 'needs-operator';
}

const BREAKDOWN_BLOCK = 'Makine arızalı. Arıza kapatılmadan üretim yapılamaz.';

export interface StationOptions {
  /** Aynı kartın tekrar okumalarının yutulduğu süre. */
  debounceMs?: number;
  newId?: () => string;
  now?: () => number;
}

/**
 * İstasyon ekranının akışı: personel kartı (oturum) → demet kartı → operasyon → adet bildirimi.
 *
 * Burada iş kuralı yoktur: kart, demet, operasyon ve adet kontrollerinin hepsini sunucu yapar; tablet
 * yalnızca hangi adımda olduğunu izler ve sunucunun yanıtını (ya da hata mesajını) gösterir.
 */
export class StationController {
  private state: StationState = initialState;
  private readonly listeners = new Set<() => void>();
  private readonly acceptRead: (uid: string) => boolean;
  private readonly newId: () => string;
  private readonly now: () => number;

  constructor(
    private readonly api: StationApi,
    private readonly outbox: ReportOutbox,
    options: StationOptions = {},
  ) {
    this.now = options.now ?? Date.now;
    this.newId = options.newId ?? newUuid;
    this.acceptRead = createReadDebouncer(options.debounceMs ?? 3000, this.now);
  }

  getState = (): StationState => this.state;

  subscribe = (listener: () => void): (() => void) => {
    this.listeners.add(listener);
    return () => this.listeners.delete(listener);
  };

  /** Sunucudan bağlamı ve makinede süren işi alır; bekleyen bildirimleri göndermeyi dener. */
  async synchronize(): Promise<void> {
    this.update({ pendingReports: (await this.outbox.list()).length });

    try {
      const context = await this.api.getContext();
      this.update({ context, online: true });

      await this.flushOutbox();
      if (this.state.pendingReports > 0) {
        return;
      }

      // Oturum sunucudadır: uygulama yeniden açılsa da operatör ve süren iş kaldığı yerden gelir.
      const session = await this.api.getSession();
      const job = await this.api.getCurrentOperation();
      this.update({
        synced: true,
        job,
        bundle: job && this.state.bundle?.bundleId === job.bundleId ? this.state.bundle : null,
        ...sessionState(session),
      });
    } catch (error) {
      this.fail(error);
    }
  }

  /** Düzenli çağrılır: kalp atışı, bekleyen bildirimler ve (gerekiyorsa) ilk eşitleme. */
  /** Oturumu açık operatörün performansı (performans sayfası ve sol panel için; ekran durumunu değiştirmez). */
  fetchPerformance(period: PerformancePeriod): Promise<TabletPerformance | null> {
    return this.api.getPerformance(period);
  }

  async tick(): Promise<void> {
    if (this.state.busy || this.state.needsEnrollment) {
      return;
    }

    if (!this.state.synced || this.state.pendingReports > 0) {
      await this.synchronize();
      return;
    }

    try {
      await this.api.heartbeat();
      // Makine durumu web'den de değişir (ör. arıza oradan kapatılır); plan, nedenler ve yetkiler de güncel kalır.
      this.update({ online: true, context: await this.api.getContext() });

      // Mola saati, arıza ya da yönetici kapatması sunucuda değişir: oturum düzenli yenilenir.
      if (this.state.operator) {
        this.update(sessionState(await this.api.getSession()));
      }
      await this.refreshJob();
      await this.refreshOperationChoice();
    } catch (error) {
      this.fail(error, { silent: true });
    }
  }

  /**
   * Açık duran operasyon listesi demetin güncel hâliyle yenilenir: başka yerden (ör. web'den elle bildirim)
   * tamamlanan adım listede "Hazır" kalmaz, sıradaki adım seçilebilir olur.
   */
  private async refreshOperationChoice(): Promise<void> {
    const choice = this.state.operationChoice;
    if (!choice) {
      return;
    }

    const bundle = await this.api.getBundle(choice.bundle.bundleId);
    // Yenileme sürerken liste kapatıldıysa ya da başka demet okutulduysa sonuç atılır.
    if (this.state.operationChoice?.bundle.bundleId === bundle.bundleId) {
      this.update({ operationChoice: { bundle, options: (bundle.operations ?? []).filter(step => step.canStart) } });
    }
  }

  /** Planlı molaya çıkma, ara verme ya da demet beklediğini bildirme. Uygunluğunu sunucu denetler. */
  async pause(type: PauseType, reasonId: number | null = null): Promise<void> {
    if (this.state.busy || !this.state.operator) {
      return;
    }

    // Kart isteyen tek duraklama aradır; planlı mola ve demet bekleme serbesttir.
    if (type === 'PAUSE') {
      this.requestAuthorization('pause', reasonId);
      return;
    }

    await this.run(() => this.applyPause(type, reasonId, null));
  }

  /** Duraklamadan çalışmaya dönüş. */
  async resume(): Promise<void> {
    if (this.state.busy || !this.state.operator) {
      return;
    }

    // Aradan dönüş kart ister; mola ve demet beklemeden dönüş serbesttir.
    if (this.state.session?.status === 'PAUSED') {
      this.requestAuthorization('resume', null);
      return;
    }

    await this.run(() => this.applyResume(null));
  }

  /** Okuyucudan gelen kart. Hangi adımda olunduğuna göre personel ya da demet kartı olarak işlenir. */
  async handleCard(uid: string): Promise<void> {
    const pending = this.state.authorization;
    if (pending) {
      if (!this.state.busy && this.acceptRead(uid)) {
        await this.run(() => this.authorize(pending, uid));
      }
      return;
    }

    const phase = phaseOf(this.state);
    if (this.state.busy || phase === 'loading' || !this.acceptRead(uid)) {
      return;
    }

    if (this.state.operationChoice) {
      this.update({ operationChoice: null });
    }

    // Arızalı makinede üretim yapılmaz (sunucu da başlatmayı reddeder); personel kartı yine okutulabilir.
    if (phase !== 'needs-operator' && this.machineBroken()) {
      this.update({ message: { kind: 'error', text: BREAKDOWN_BLOCK } });
      return;
    }

    if (phase === 'working') {
      this.update({ message: { kind: 'info', text: 'Süren işin adedini bildirmeden yeni kart okutulamaz.' } });
      return;
    }

    await this.run(() => (phase === 'needs-operator' ? this.readOperator(uid) : this.readBundleAndStart(uid)));
  }

  /** Süren iş için adet bildirir. Sunucuya ulaşılamazsa bildirim kuyruğa alınır ve sonra gönderilir. */
  async report(producedQuantity: number, rejectedQuantity: number): Promise<void> {
    const job = this.state.job;
    if (!job || this.state.busy) {
      return;
    }

    // Arızalı makinede yeni iş başlatılmaz ama süren işin o ana kadarki adedi bildirilebilir: aksi halde demet,
    // makine ve operatör arıza boyunca kilitli kalır (operatör çıkış yapamaz, demet başka makinede sürdürülemez).
    const requestId = this.newId();
    await this.run(async () => {
      try {
        const progress = await this.api.reportOperation(
          job.bundleOperationId, requestId, producedQuantity, rejectedQuantity);
        this.update({ job: null, bundle: null, online: true, message: reportedMessage(progress) });
        await this.refreshSession();
      } catch (error) {
        if (!isRetryable(error)) {
          throw error;
        }

        await this.outbox.add({
          requestId,
          bundleOperationId: job.bundleOperationId,
          bundleNo: job.bundleNo,
          operationName: job.operationName,
          producedQuantity,
          rejectedQuantity,
          createdAt: new Date(this.now()).toISOString(),
        });
        this.update({
          job: null,
          bundle: null,
          online: false,
          pendingReports: (await this.outbox.list()).length,
          message: {
            kind: 'info',
            text: 'Sunucuya ulaşılamadı. Adet tablette saklandı; bağlantı gelince kendiliğinden gönderilecek.',
          },
        });
      }
    });
  }

  /** Operatörün oturumunu kapatır; süren iş varken yapılamaz. */
  async signOut(): Promise<void> {
    if (this.state.busy) {
      return;
    }

    if (this.state.job) {
      this.update({ message: { kind: 'info', text: 'Önce süren işin adedini bildirin.' } });
      return;
    }

    await this.run(async () => {
      await this.api.logout();
      this.update({ online: true, operator: null, session: null, message: null });
    });
  }

  /** Arıza bildirir: her zaman kart beklenir. */
  async reportBreakdown(reasonId: number | null): Promise<void> {
    if (this.state.busy) {
      return;
    }

    this.requestAuthorization('breakdown', reasonId);
  }

  /** Arızayı kapatma isteği: her zaman bakım operatörünün ya da bant şefinin kartı beklenir. */
  reportRecovery(): void {
    if (!this.state.busy) {
      this.requestAuthorization('recover', null);
    }
  }

  /** Yetkili kartı beklemekten vazgeçer; hiçbir şey bildirilmez. */
  cancelAuthorization(): void {
    this.update({ authorization: null, message: null });
  }

  /** Seçim penceresinden operasyon seçildi: o adım başlatılır. */
  async chooseOperation(bundleOperationId: number): Promise<void> {
    const choice = this.state.operationChoice;
    if (!choice || this.state.busy) {
      return;
    }

    this.update({ operationChoice: null });
    await this.run(() => this.startStep(choice.bundle, bundleOperationId));
  }

  cancelOperationChoice(): void {
    this.update({ operationChoice: null });
  }

  /** Operatör boşta sorusunu kapattı: süre baştan sayılır. */
  acknowledgeIdle(): void {
    if (this.state.idleSince !== null) {
      this.update({ idleSince: this.now() });
    }
  }

  dismissMessage(): void {
    this.update({ message: null });
  }

  /** Okuyucunun yazdığı satır geçerli bir kart numarası değilse. */
  notifyInvalidRead(): void {
    this.update({ message: { kind: 'error', text: 'Kart okunamadı. Lütfen kartı yeniden okutun.' } });
  }

  private requestAuthorization(action: AuthorizedAction, reasonId: number | null): void {
    this.update({ authorization: { action, reasonId }, message: null });
  }

  /** Okutulan yetkili kartıyla bekleyen işlemi tamamlar; kart yetkili değilse bekleme sürer ve neden gösterilir. */
  private async authorize(pending: PendingAuthorization, uid: string): Promise<void> {
    switch (pending.action) {
      case 'pause':
        await this.applyPause('PAUSE', pending.reasonId, uid);
        break;
      case 'resume':
        await this.applyResume(uid);
        break;
      case 'breakdown':
        await this.applyBreakdown(pending.reasonId, uid);
        break;
      default:
        await this.applyRecovery(uid);
    }
  }

  /**
   * Oturum sahibinin kendi yetkisiyle denenen işlem sunucuda "yetkili kartı gerekli" diye reddedilirse
   * (yetki o arada web'den kaldırılmış olabilir) hata göstermek yerine kart beklenir.
   */
  private async withCardFallback(action: AuthorizedAction, reasonId: number | null, attempt: () => Promise<void>) {
    try {
      await attempt();
    } catch (error) {
      if (!(error instanceof ApiError) || error.code !== AUTHORIZATION_CARD_REQUIRED) {
        throw error;
      }

      this.requestAuthorization(action, reasonId);
    }
  }

  private async applyPause(type: PauseType, reasonId: number | null, authorizerUid: string | null): Promise<void> {
    const session = await this.api.pause(type, reasonId, authorizerUid);
    this.update({ online: true, ...sessionState(session), message: null, authorization: null });
    await this.refreshJob();
  }

  private async applyResume(authorizerUid: string | null): Promise<void> {
    const session = await this.api.resume(authorizerUid);
    this.update({ online: true, ...sessionState(session), message: null, authorization: null });
    await this.refreshJob();
  }

  private async applyBreakdown(reasonId: number | null, authorizerUid: string | null): Promise<void> {
    const machine = await this.api.reportBreakdown(reasonId, authorizerUid);
    this.setMachineStatus(machine.status, machine.statusReason ?? null);
    this.update({
      online: true,
      authorization: null,
      message: { kind: 'info', text: 'Arıza bildirildi. Bakım ekibi bilgilendirildi.' },
    });
    await this.refreshSession();
  }

  private async applyRecovery(authorizerUid: string): Promise<void> {
    const machine = await this.api.reportRecovery(authorizerUid);
    this.setMachineStatus(machine.status, machine.statusReason ?? null);
    this.update({
      online: true,
      authorization: null,
      message: { kind: 'success', text: 'Makine yeniden çalışır durumda.' },
    });
    await this.refreshSession();
  }

  private async readOperator(uid: string): Promise<void> {
    const session = await this.api.login(uid, this.newId());
    this.update({
      online: true,
      ...sessionState(session),
      message: { kind: 'success', text: `Hoş geldiniz, ${session.fullName}.` },
    });
  }

  private async readBundleAndStart(uid: string): Promise<void> {
    if (this.state.pendingReports > 0) {
      await this.flushOutbox();
      if (this.state.pendingReports > 0) {
        this.update({
          message: {
            kind: 'info',
            text: 'Önceki adet bildirimi henüz sunucuya ulaşmadı. Bağlantı gelince yeni demete başlayabilirsiniz.',
          },
        });
        return;
      }
    }

    const bundle = await this.api.readBundle(uid, this.newId());

    // Demetin operasyonları dökülür: sıradaki adım seçili gelir, operatör "Başlat" ile işe başlar.
    if (bundle.operations && bundle.operations.length > 0) {
      const options = bundle.operations.filter(candidate => candidate.canStart);
      this.update({ online: true, operationChoice: { bundle, options }, message: null });
      return;
    }

    // Operasyon listesi gelmediyse (eski sunucu) makinenin adımı doğrudan başlatılır.
    const step = bundle.machineOperation;
    if (!step) {
      this.update({
        online: true,
        message: { kind: 'error', text: `${bundle.bundleNo} numaralı demetin bu makinede yapılacak operasyonu yok.` },
      });
      return;
    }

    await this.startStep(bundle, step.bundleOperationId);
  }

  private async startStep(bundle: BundleRead, bundleOperationId: number): Promise<void> {
    const job = await this.api.startOperation(bundleOperationId, this.newId());
    this.update({ online: true, job, bundle, message: null });
    // İşe başlamak süren arayı / demet beklemeyi sunucuda sonlandırır.
    await this.refreshSession();
  }

  private async flushOutbox(): Promise<void> {
    const result = await this.outbox.flush(report =>
      this.api.reportOperation(
        report.bundleOperationId, report.requestId, report.producedQuantity, report.rejectedQuantity));

    this.update({ pendingReports: result.pending });

    const rejected = result.rejected[0];
    if (rejected) {
      const reason = rejected.error instanceof Error ? rejected.error.message : '';
      this.update({
        message: {
          kind: 'error',
          text:
            `${rejected.report.bundleNo} numaralı demet için bekleyen adet bildirimi sunucu tarafından reddedildi: ` +
            `${reason} Bant şefine haber verin.`,
        },
      });
    } else if (result.sent.length > 0) {
      this.update({ message: { kind: 'success', text: 'Bekleyen adet bildirimi sunucuya gönderildi.' } });
    }
  }

  /** İşlemi "meşgul" işaretiyle yürütür; hata olursa kullanıcıya gösterilecek duruma çevirir. */
  private async run(action: () => Promise<void>): Promise<void> {
    this.update({ busy: true });
    try {
      await action();
    } catch (error) {
      this.fail(error);
    } finally {
      this.update({ busy: false });
    }
  }

  private fail(error: unknown, options: { silent?: boolean } = {}): void {
    if (error instanceof ApiError && error.code === INVALID_DEVICE_CREDENTIALS) {
      this.update({ needsEnrollment: true, message: { kind: 'error', text: error.message } });
      return;
    }

    if (isRetryable(error)) {
      this.update({
        online: false,
        ...(options.silent
          ? {}
          : { message: { kind: 'error' as const, text: 'Sunucuya ulaşılamıyor. Bağlantıyı kontrol edip tekrar deneyin.' } }),
      });
      return;
    }

    // Sunucu yanıt verdi: bağlantı var, istek reddedildi. Mesaj sunucudan gelir.
    const text = error instanceof Error ? error.message : 'Beklenmeyen bir hata oluştu.';
    this.update({ online: true, message: { kind: 'error', text } });
  }

  /** Oturum bilgisini sunucudan yeniler; ulaşılamazsa eldeki bilgi kalır (bir sonraki denetimde yenilenir). */
  private async refreshSession(): Promise<void> {
    if (!this.state.operator) {
      return;
    }

    try {
      this.update(sessionState(await this.api.getSession()));
    } catch {
      // Asıl işlem başarılı oldu; oturum bilgisinin yenilenememesi kullanıcıya hata olarak gösterilmez.
    }

    await this.refreshJob();
  }

  /**
   * Süren işi sunucudan yeniler: geri sayımın net süresi (mola, ara ve arıza düşülmüş) sunucuda hesaplanır.
   * Ulaşılamazsa eldeki bilgi kalır.
   */
  private async refreshJob(): Promise<void> {
    const current = this.state.job;
    if (!current) {
      return;
    }

    try {
      const job = await this.api.getCurrentOperation();
      if (job && job.bundleOperationId === current.bundleOperationId) {
        this.update({ job });
      }
    } catch {
      // Sayaç eldeki süreyle saymaya devam eder.
    }
  }

  private machineBroken(): boolean {
    return this.state.context?.machine?.status === 'BREAKDOWN';
  }

  private setMachineStatus(status: MachineStatus, statusReason: string | null): void {
    const context = this.state.context;
    if (context?.machine) {
      this.update({ context: { ...context, machine: { ...context.machine, status, statusReason } } });
    }
  }

  private update(patch: Partial<StationState>): void {
    const previousOperatorId = this.state.operator?.id ?? null;
    this.state = { ...this.state, ...patch };
    if ('job' in patch) {
      this.state = { ...this.state, jobSyncedAt: patch.job ? this.now() : null };
    }

    // Boşta: oturum "çalışıyor" görünüyor ama iş yok (mola, ara, arıza ve demet bekleme boşta sayılmaz).
    const idle =
      this.state.session?.status === 'ACTIVE' &&
      this.state.job === null &&
      this.state.context?.machine?.status !== 'BREAKDOWN';
    if (idle !== (this.state.idleSince !== null)) {
      this.state = { ...this.state, idleSince: idle ? this.now() : null };
    }

    // Operatör değişince eski fotoğraf hemen kalkar; yenisi arka planda istenir.
    const operatorId = this.state.operator?.id ?? null;
    if (operatorId !== previousOperatorId) {
      this.state = { ...this.state, operatorPhoto: null };
      if (operatorId !== null) {
        this.loadOperatorPhoto(operatorId);
      }
    }

    this.listeners.forEach(listener => listener());
  }

  /** Fotoğraf yalnızca görsel bilgidir: alınamazsa akış etkilenmez, baş harfler gösterilir. */
  private async loadOperatorPhoto(operatorId: number): Promise<void> {
    try {
      const photo = await this.api.getOperatorPhoto();
      if (photo && this.state.operator?.id === operatorId) {
        this.update({ operatorPhoto: photo });
      }
    } catch {
      // Bağlantı durumu ve hata iletileri asıl işlemler tarafından yönetilir.
    }
  }
}

/** Sunucudaki oturumun tabletteki karşılığı; oturum yoksa operatör de yoktur. */
function sessionState(session: TabletSession | null): Pick<StationState, 'operator' | 'session'> {
  return { session, operator: session ? { id: session.operatorId, name: session.fullName } : null };
}

/** Sunucunun hesapladığı iş verimi (varsa): " Verim %112." */
function efficiencyText(progress: OperationProgress): string {
  return progress.efficiencyPercent == null ? '' : ` Verim %${progress.efficiencyPercent}.`;
}

function reportedMessage(progress: OperationProgress): StationMessage {
  if (progress.status !== 'COMPLETED') {
    return {
      kind: 'info',
      text: `${progress.bundleNo} · ${progress.operationName}: ${progress.remainingQuantity} adet kaldı.${efficiencyText(progress)} Demet yeniden okutulunca kalandan devam edilir.`,
    };
  }

  const next = progress.nextOperations.length > 0 ? ` Sıradaki: ${progress.nextOperations.join(', ')}.` : ' Demet tamamlandı.';
  return { kind: 'success', text: `${progress.bundleNo} · ${progress.operationName} tamamlandı.${efficiencyText(progress)}${next}` };
}
