import { useEffect, useState } from 'react';
import { ScrollView, StyleSheet, Text, View } from 'react-native';
import type { BundleStep, StaffRole, StaffRolePermissions } from '../api/types';
import type { KeyboardWedgeReader } from '../rfid/KeyboardWedgeReader';
import { WedgeInput } from '../rfid/WedgeInput';
import { phaseOf } from '../station/StationController';
import type { AuthorizedAction, StationController, StationPhase, StationState } from '../station/StationController';
import { useStation } from '../station/useStation';
import { Banner, BigButton, QuantityStepper } from '../ui/controls';
import { AuthorizationDialog, DialogFrame, OperationDialog, ReasonDialog } from '../ui/Dialog';
import type { OperationRow } from '../ui/Dialog';
import { BusyOverlay, InfoStrip, MessageToast, Prompt, StationTopBar } from '../ui/station';
import { colors, radius } from '../ui/theme';
import { formatTimer } from '../util/time';
import { useNow } from '../util/useNow';
import { SessionBar } from './SessionBar';
import { staffRoleLabels, StationSidebar } from './StationSidebar';

interface StationScreenProps {
  controller: StationController;
  reader: KeyboardWedgeReader;
  tabletCode: string;
  /** Markaya uzun basılınca sistem ayarları açılır (yönetici işlemi). */
  onOpenSetup: () => void;
}

const stepPhases: StationPhase[] = ['needs-operator', 'needs-bundle', 'working'];
const stepLabels = ['Personel', 'Demet', 'Üretim'];

/**
 * Yetkili kartı beklenen işlemin penceresi: başlık, görevin yetkisini taşıyan alan ve sunucu görev
 * yetkilerini göndermediyse gösterilecek görevler.
 */
const authorizations: Record<
  AuthorizedAction,
  { title: string; permission: 'canPause' | 'canReportBreakdown' | 'canResolveBreakdown'; fallback: StaffRole[] }
> = {
  pause: { title: 'Ara onayı', permission: 'canPause', fallback: ['LINE_CHIEF'] },
  resume: { title: 'Devam onayı', permission: 'canPause', fallback: ['LINE_CHIEF'] },
  breakdown: { title: 'Arıza bildirimi onayı', permission: 'canReportBreakdown', fallback: ['LINE_CHIEF', 'MAINTENANCE'] },
  recover: { title: 'Arızayı kapat', permission: 'canResolveBreakdown', fallback: ['MAINTENANCE', 'LINE_CHIEF'] },
};

/** Birim süre: "0,45 dk / adet". */
function unitTime(sam: number | undefined): string {
  return sam === undefined ? '—' : `${String(sam).replace('.', ',')} dk / adet`;
}

const stepStatusLabels: Record<BundleStep['status'], string> = {
  WAITING: 'Sırasını bekliyor',
  READY: 'Hazır',
  IN_PROGRESS: 'İşlemde',
  PAUSED: 'Yarım kaldı',
  COMPLETED: 'Tamamlandı',
  QUALITY_HOLD: 'Kalitede bekliyor',
  CANCELLED: 'İptal',
};

/** Demetin adımları, operasyon penceresinin satırları olarak. Neyin başlatılabileceğini sunucu söyler. */
function operationRows(steps: BundleStep[]): OperationRow[] {
  return steps.map(step => {
    const done = step.status === 'COMPLETED';
    return {
      id: step.bundleOperationId,
      sequenceNo: step.sequenceNo,
      name: step.operationName,
      status: !done && step.machineCanDo === false ? 'Başka makinede' : stepStatusLabels[step.status],
      quantity: `${step.producedQuantity} / ${step.plannedQuantity}`,
      duration: step.sam ? `${Math.round(step.sam * step.plannedQuantity)} dk` : '—',
      done,
      startable: step.canStart === true,
    };
  });
}

/** Bu makinede başlatılabilecek adım yoksa nedenini söyler; varsa boştur. */
function operationNotice(steps: BundleStep[]): string | null {
  if (steps.some(step => step.canStart)) {
    return null;
  }

  const next = steps.find(step => step.status !== 'COMPLETED' && step.status !== 'CANCELLED');
  if (!next) {
    return 'Demetin tüm operasyonları tamamlandı.';
  }

  return next.machineCanDo === false
    ? `Sıradaki operasyon “${next.operationName}” bu makinede yapılamıyor. Demeti o operasyonun makinesine iletin.`
    : `“${next.operationName}” şu an başlatılamıyor (${stepStatusLabels[next.status].toLocaleLowerCase('tr-TR')}).`;
}

/** İşlemi kartıyla onaylayabilecek görevlerin adları (web'deki Görev Yetkileri'nden). */
function authorizerRoles(action: AuthorizedAction, staffRoles: StaffRolePermissions[] | undefined): string[] {
  const { permission, fallback } = authorizations[action];
  const roles = staffRoles ? staffRoles.filter(role => role[permission]).map(role => role.role) : fallback;
  const labels = roles.map(role => staffRoleLabels[role]);
  // Kişiye özel yetkili operatör kendi tabletinde kendi kartıyla yapar; arıza kapatma yalnızca göreve bağlıdır.
  return action === 'recover' ? labels : [...labels, 'Yetkili operatör (kendi kartı)'];
}

/** Makine başındaki ana ekran: solda istasyon ve personel, sağda iş akışı (kart → demet → adet bildirimi). */
export function StationScreen({ controller, reader, tabletCode, onOpenSetup }: StationScreenProps) {
  const state = useStation(controller);
  const now = useNow();
  // Neden seçimi ekranın ortasında açılan pencerede yapılır.
  const [dialog, setDialog] = useState<'pause' | 'breakdown' | null>(null);

  useEffect(() => {
    reader.start(uid => controller.handleCard(uid));
    return () => {
      reader.stop();
    };
  }, [reader, controller]);

  const phase = phaseOf(state);
  const context = state.context;
  const machine = context?.machine;
  const pauseReasons = context?.pauseReasons ?? [];
  const breakdownReasons = context?.breakdownReasons ?? [];
  const authorization = state.authorization;
  // Boşta sorusu: süre dolduysa ve başka bir pencere açık değilse.
  const idleLimit = context?.idlePromptMinutes ?? 0;
  const idleMinutes = state.idleSince === null ? 0 : Math.floor((now.getTime() - state.idleSince) / 60_000);
  const idlePrompt =
    idleLimit > 0 && idleMinutes >= idleLimit && dialog === null && !authorization && !state.operationChoice && !state.busy;
  // Yetkili, neyi onayladığını pencerede görür: makine ve (varsa) neden.
  const authorizationReason =
    authorization?.action === 'recover'
      ? machine?.statusReason
      : [...pauseReasons, ...breakdownReasons].find(reason => reason.id === authorization?.reasonId)?.name;
  const authorizationDetails = [
    ...(machine ? [{ label: 'Makine', value: machine.machineCode }] : []),
    ...(authorizationReason ? [{ label: 'Neden', value: authorizationReason }] : []),
  ];

  return (
    <View style={styles.screen}>
      <WedgeInput reader={reader} paused={state.needsEnrollment} />

      <StationSidebar
        state={state}
        tabletCode={tabletCode}
        now={now}
        onOpenSetup={onOpenSetup}
        onSignOut={() => controller.signOut()}
      />

      <View style={styles.main}>
        <StationTopBar
          steps={stepLabels}
          current={stepPhases.indexOf(phase)}
          online={state.online}
          now={now}
          actions={
            machine &&
            !state.needsEnrollment &&
            (machine.status === 'BREAKDOWN' ? (
              <BigButton
                label="Arızayı kapat"
                tone="success"
                size="compact"
                disabled={state.busy}
                onPress={() => controller.reportRecovery()}
                testID="station-recover"
              />
            ) : (
              <BigButton
                label="Arıza bildir"
                tone="dangerOutline"
                size="compact"
                disabled={state.busy}
                // Tanımlı arıza nedeni varsa önce neden seçilir; yoksa arıza doğrudan bildirilir.
                onPress={() => (breakdownReasons.length > 0 ? setDialog('breakdown') : controller.reportBreakdown(null))}
                testID="station-breakdown"
              />
            ))
          }
        />

        {machine && (
          <InfoStrip
            cells={[
              { label: 'OPERASYON', value: state.job?.operationName ?? context?.operation?.name ?? 'Tanımsız' },
              { label: 'BİRİM SÜRE', value: unitTime(state.job?.sam ?? context?.operation?.sam) },
              { label: 'BANT', value: context?.line?.name ?? 'Atanmamış' },
              {
                label: 'GÜNCEL SİPARİŞ',
                value: context?.currentPlan ? `${context.currentPlan.orderNo} · ${context.currentPlan.modelCode}` : 'Plan yok',
              },
            ]}
          />
        )}

        <View style={styles.content}>
          <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
            {state.pendingReports > 0 && (
              <View style={styles.pending}>
                <Text style={styles.pendingText} testID="station-pending">
                  {state.pendingReports} bildirim gönderilmeyi bekliyor. Bağlantı gelince kendiliğinden gönderilir.
                </Text>
              </View>
            )}

            {state.session && !state.needsEnrollment && phase !== 'loading' && (
              <SessionBar
                session={state.session}
                breakdownReason={machine?.statusReason}
                hasJob={state.job !== null}
                busy={state.busy}
                canPause={pauseReasons.length > 0}
                onRequestPause={() => setDialog('pause')}
                onPause={(type, reason) => controller.pause(type, reason)}
                onResume={() => controller.resume()}
              />
            )}

            {state.needsEnrollment ? (
              <Prompt
                glyph="alert"
                title="Tabletin yeniden eşleştirilmesi gerekiyor"
                detail="Cihaz anahtarı artık geçerli değil. Sistem ayarlarından makineyi yeniden seçin.">
                <BigButton label="Sistem ayarları" onPress={onOpenSetup} testID="station-open-setup" />
              </Prompt>
            ) : phase === 'loading' ? (
              <Prompt
                glyph={state.online ? 'loading' : 'alert'}
                title={state.online ? 'Sunucuya bağlanılıyor' : 'Sunucuya ulaşılamıyor'}
                detail={
                  state.online ? undefined : 'Wi-Fi bağlantısını kontrol edin. Bağlantı gelince kendiliğinden devam edilir.'
                }>
                {!state.online && <BigButton label="Tekrar dene" onPress={() => controller.synchronize()} testID="station-retry" />}
              </Prompt>
            ) : !machine ? (
              <Prompt
                glyph="alert"
                title="Tablet bir makineye bağlı değil"
                detail="Yönetim ekranında Tabletler bölümünden bu tableti bir makineye bağlayın.">
                <BigButton label="Yenile" onPress={() => controller.synchronize()} testID="station-retry" />
              </Prompt>
            ) : phase === 'needs-operator' ? (
              <Prompt
                glyph="card"
                step="1. ADIM"
                title="Personel kartınızı okutun"
                detail="Çalışmaya başlamak için kartınızı okuyucuya gösterin."
              />
            ) : machine.status === 'BREAKDOWN' && !state.job ? (
              <Prompt glyph="alert" title="Makine arızalı" detail={machine.statusReason ?? undefined} />
            ) : phase === 'needs-bundle' ? (
              <Prompt
                glyph="bundle"
                step="2. ADIM"
                title="Demet kartını okutun"
                detail="Demet kartı okutulunca operasyonlar listelenir; sıradaki operasyonu başlatırsınız."
              />
            ) : (
              // Yeni iş ya da değişen adetlerde giriş alanları baştan kurulur. Arıza sırasında da süren işin
              // yapılan adedi bildirilebilir; kalan adet arıza giderilince ya da başka makinede sürdürülür.
              <JobPanel
                key={`${state.job!.bundleOperationId}:${state.job!.remainingQuantity}`}
                state={state}
                broken={machine.status === 'BREAKDOWN'}
                onReport={(produced, rejected) => controller.report(produced, rejected)}
              />
            )}
          </ScrollView>

          {/* Yetki penceresi açıkken ileti orada gösterilir. */}
          {state.message && !authorization && (
            <MessageToast
              kind={state.message.kind}
              text={state.message.text}
              onDismiss={() => controller.dismissMessage()}
              testID="station-message"
            />
          )}

          {state.busy && <BusyOverlay />}
        </View>
      </View>

      {idlePrompt && (
        <DialogFrame
          title="Süren iş yok"
          description={`${idleMinutes} dakikadır bu makinede süren iş görünmüyor.`}
          onClose={() => controller.acknowledgeIdle()}
          testID="idle-dialog"
          footer={
            <>
              <BigButton label="Kapat" tone="neutral" size="compact" onPress={() => controller.acknowledgeIdle()} testID="idle-dismiss" />
              {pauseReasons.length > 0 && (
                <BigButton
                  label="Ara ver"
                  tone="neutral"
                  size="compact"
                  onPress={() => {
                    controller.acknowledgeIdle();
                    setDialog('pause');
                  }}
                  testID="idle-pause"
                />
              )}
              <BigButton
                label="Demet bekliyorum"
                size="compact"
                busy={state.busy}
                onPress={() => controller.pause('WAITING_FOR_BUNDLE', null)}
                testID="idle-waiting"
              />
            </>
          }>
          <Text style={styles.idleText}>
            Demet bekliyorsanız bildirin; bekleme süresi veriminize yansımaz. Demet elinizdeyse kartını okutun.
          </Text>
        </DialogFrame>
      )}

      {dialog === 'pause' && !authorization && state.session?.status === 'ACTIVE' && (
        <ReasonDialog
          title="Ara ver"
          description="Ara nedenini seçin. Döndüğünüzde “Devam et” ile çalışmaya devam edersiniz."
          reasons={pauseReasons}
          confirmLabel="Ara ver"
          busy={state.busy}
          onConfirm={reasonId => {
            setDialog(null);
            controller.pause('PAUSE', reasonId);
          }}
          onCancel={() => setDialog(null)}
          testIDPrefix="session-reason"
        />
      )}

      {dialog === 'breakdown' && !authorization && machine && machine.status !== 'BREAKDOWN' && (
        <ReasonDialog
          title="Arıza bildir"
          description={`${machine.machineCode} için arıza kaydı açılır ve bakım ekibi bilgilendirilir.`}
          reasons={breakdownReasons}
          confirmLabel="Arızayı bildir"
          tone="danger"
          busy={state.busy}
          onConfirm={reasonId => {
            setDialog(null);
            controller.reportBreakdown(reasonId);
          }}
          onCancel={() => setDialog(null)}
          testIDPrefix="breakdown-reason"
        />
      )}

      {state.operationChoice && !authorization && (
        <OperationDialog
          // Yeni okutmada seçim baştan kurulur.
          key={state.operationChoice.bundle.bundleId}
          title={`Demet ${state.operationChoice.bundle.bundleNo}`}
          summary={[
            { label: 'SİPARİŞ', value: state.operationChoice.bundle.orderNo },
            { label: 'MODEL', value: state.operationChoice.bundle.modelCode },
            {
              label: 'RENK / BEDEN',
              value: `${state.operationChoice.bundle.colorName} / ${state.operationChoice.bundle.sizeName}`,
            },
            { label: 'ADET', value: String(state.operationChoice.bundle.quantity) },
          ]}
          rows={operationRows(state.operationChoice.bundle.operations ?? [])}
          notice={operationNotice(state.operationChoice.bundle.operations ?? [])}
          busy={state.busy}
          onStart={bundleOperationId => controller.chooseOperation(bundleOperationId)}
          onCancel={() => controller.cancelOperationChoice()}
        />
      )}

      {authorization && (
        <AuthorizationDialog
          title={authorizations[authorization.action].title}
          roles={authorizerRoles(authorization.action, context?.staffRoles)}
          details={authorizationDetails}
          error={state.message?.kind === 'error' ? state.message.text : null}
          busy={state.busy}
          onCancel={() => controller.cancelAuthorization()}
        />
      )}
    </View>
  );
}


/** Süren iş: demet bilgisi ve adet bildirimi. Varsayılan, kalan adedin tamamının sağlam üretilmesidir. */
function JobPanel({
  state,
  broken,
  onReport,
}: {
  state: StationState;
  /** Makine arızalı: iş yarım kalmıştır, yapılan adet operatörden beklenir. */
  broken: boolean;
  onReport: (produced: number, rejected: number) => void;
}) {
  const job = state.job!;
  // Arızada kalanın tamamı yapılmış sayılmaz; operatör yaptığı adedi kendisi girer.
  const [produced, setProduced] = useState(broken ? 0 : job.remainingQuantity);
  const [rejected, setRejected] = useState(0);
  const left = job.remainingQuantity - produced - rejected;

  return (
    <View style={styles.job}>
      {broken && (
        <Banner
          kind="error"
          text="Makine arızalı. Yaptığınız adedi bildirip işi bırakabilirsiniz; kalan adet arıza giderilince ya da başka makinede sürdürülür."
          testID="job-breakdown"
        />
      )}
      <View style={styles.jobCard}>
        <View style={styles.jobHeader}>
          <View style={styles.jobTitle}>
            <Text style={styles.caption}>SÜREN İŞ</Text>
            <Text style={styles.jobBundle} testID="job-bundle">
              Demet {job.bundleNo}
            </Text>
            <Text style={styles.jobOperation} testID="job-operation">
              {job.sequenceNo} · {job.operationName}
            </Text>
            {state.bundle && (
              <Text style={styles.jobDetail} testID="job-detail">
                {state.bundle.modelCode} · {state.bundle.colorName} · {state.bundle.sizeName} · Sipariş {state.bundle.orderNo}
              </Text>
            )}
          </View>

          <View style={styles.tiles}>
            <Tile label="Planlanan" value={job.plannedQuantity} testID="job-planned" />
            <Tile label="Üretilen" value={job.producedQuantity} testID="job-done" />
            <Tile label="Hatalı" value={job.rejectedQuantity} testID="job-rejected-total" />
            <Tile label="Kalan" value={job.remainingQuantity} testID="job-remaining" highlighted />
          </View>
        </View>

        {state.bundle?.operations && state.bundle.operations.length > 1 && (
          <View style={styles.route} testID="job-route">
            <View style={styles.routeSteps}>
              {state.bundle.operations.map(step => {
                const current = step.bundleOperationId === job.bundleOperationId;
                const done = !current && step.status === 'COMPLETED';
                return (
                  <View key={step.bundleOperationId} style={[styles.routeStep, current && styles.routeStepCurrent]}>
                    <Text style={[styles.routeStepText, current && styles.routeStepTextCurrent, done && styles.routeStepTextDone]}>
                      {done ? '✓ ' : ''}
                      {step.sequenceNo} · {step.operationName}
                    </Text>
                  </View>
                );
              })}
            </View>
          </View>
        )}
      </View>

      {job.runTargetSeconds != null && job.runElapsedSeconds != null && state.jobSyncedAt !== null && (
        <JobTimer
          targetSeconds={job.runTargetSeconds}
          elapsedSeconds={job.runElapsedSeconds}
          syncedAt={state.jobSyncedAt}
          // Mola, ara ve arızada sayaç durur; net süreyi sunucu hesaplar.
          running={!state.session || state.session.status === 'ACTIVE'}
          sam={job.sam ?? 0}
          quantity={job.remainingQuantity}
        />
      )}

      <View style={[styles.jobCard, styles.steppers]}>
        <QuantityStepper
          label="SAĞLAM ADET"
          value={produced}
          max={job.remainingQuantity - rejected}
          onChange={setProduced}
          testID="job-produced"
        />
        <QuantityStepper
          label="HATALI ADET"
          value={rejected}
          max={job.remainingQuantity - produced}
          onChange={setRejected}
          testID="job-rejected"
        />
      </View>

      <BigButton
        label={left === 0 ? 'Tamamla' : `Bildir ve bırak (${left} adet kalır)`}
        tone="success"
        busy={state.busy}
        onPress={() => onReport(produced, rejected)}
        testID="job-report"
        style={styles.reportButton}
      />
    </View>
  );
}

interface JobTimerProps {
  targetSeconds: number;
  /** Sunucunun verdiği net geçen süre (saniye). */
  elapsedSeconds: number;
  /** O sürenin alındığı an (cihaz saati, ms). */
  syncedAt: number;
  running: boolean;
  sam: number;
  quantity: number;
}

/**
 * Geri sayım: hedef süre (birim süre × adet) dolana kadar kalan süre; dolunca aşım süresi. Sunucunun verdiği
 * net süreye, cihazda o andan beri geçen süre eklenir (cihaz saatinin doğruluğuna bağlı değildir).
 */
function JobTimer({ targetSeconds, elapsedSeconds, syncedAt, running, sam, quantity }: JobTimerProps) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 1000);
    return () => clearInterval(timer);
  }, []);

  const elapsed = elapsedSeconds + (running ? Math.max(0, (now - syncedAt) / 1000) : 0);
  const remaining = targetSeconds - elapsed;
  const overdue = remaining < 0;
  const ratio = targetSeconds > 0 ? Math.min(1, elapsed / targetSeconds) : 1;
  const expected = sam > 0 ? Math.min(quantity, Math.floor(elapsed / (sam * 60))) : 0;
  const accent = overdue ? colors.danger : colors.primary;

  return (
    <View style={[styles.jobCard, styles.timer]} testID="job-timer">
      <View style={styles.timerMain}>
        <Text style={[styles.caption, overdue && styles.timerCaptionOverdue]}>
          {!running ? 'SAYAÇ DURDU' : overdue ? 'SÜRE AŞILDI' : 'KALAN SÜRE'}
        </Text>
        <Text style={[styles.timerValue, { color: accent }]} testID="job-timer-value">
          {overdue ? '+' : ''}
          {formatTimer(Math.abs(remaining))}
        </Text>
      </View>

      <View style={styles.timerDetail}>
        <View style={styles.timerTrack}>
          <View style={[styles.timerFill, { width: `${ratio * 100}%`, backgroundColor: accent }]} />
        </View>
        <View style={styles.timerFacts}>
          <TimerFact label="Birim süre" value={unitTime(sam)} />
          <TimerFact label="Hedef süre" value={formatTimer(targetSeconds)} />
          <TimerFact label="Geçen süre" value={formatTimer(elapsed)} />
          <TimerFact label="Beklenen adet" value={`${expected} / ${quantity}`} />
        </View>
      </View>
    </View>
  );
}

function TimerFact({ label, value }: { label: string; value: string }) {
  return (
    <View style={styles.timerFact}>
      <Text style={styles.timerFactLabel}>{label}</Text>
      <Text style={styles.timerFactValue}>{value}</Text>
    </View>
  );
}

function Tile({ label, value, testID, highlighted }: { label: string; value: number; testID: string; highlighted?: boolean }) {
  return (
    <View style={[styles.tile, highlighted && styles.tileHighlighted]}>
      <Text style={[styles.tileValue, highlighted && styles.tileValueHighlighted]} testID={testID}>
        {value}
      </Text>
      <Text style={styles.tileLabel}>{label}</Text>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, flexDirection: 'row', backgroundColor: colors.background },
  main: { flex: 1 },
  content: { flex: 1 },
  // İş alanı kenar boşluğu olmadan ekranı doldurur; bölümler ince çizgilerle ayrılır.
  body: { flexGrow: 1, backgroundColor: colors.surface },
  pending: {
    backgroundColor: colors.warningSoft,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 8,
    paddingHorizontal: 20,
  },
  pendingText: { color: colors.text, fontSize: 14, fontWeight: '600' },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.2 },
  job: { flex: 1 },
  jobCard: {
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 12,
    paddingHorizontal: 20,
    gap: 10,
  },
  jobHeader: { flexDirection: 'row', flexWrap: 'wrap', alignItems: 'center', gap: 16 },
  jobTitle: { flex: 1, minWidth: 280, gap: 2 },
  jobBundle: { color: colors.text, fontSize: 22, fontWeight: '700' },
  jobOperation: { color: colors.primary, fontSize: 16, fontWeight: '600' },
  jobDetail: { color: colors.muted, fontSize: 14 },
  idleText: { color: colors.textSecondary, fontSize: 15, lineHeight: 22 },
  route: { gap: 6 },
  routeSteps: { flexDirection: 'row', flexWrap: 'wrap', gap: 6 },
  routeStep: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surfaceMuted,
    paddingVertical: 4,
    paddingHorizontal: 10,
  },
  routeStepCurrent: { borderColor: colors.primary, backgroundColor: colors.primarySoft },
  routeStepText: { color: colors.textSecondary, fontSize: 13, fontWeight: '500' },
  routeStepTextCurrent: { color: colors.primary, fontWeight: '700' },
  routeStepTextDone: { color: colors.success },
  tiles: { flexDirection: 'row', gap: 8 },
  tile: {
    minWidth: 84,
    alignItems: 'center',
    backgroundColor: colors.surfaceMuted,
    borderRadius: radius.control,
    borderWidth: 1,
    borderColor: colors.border,
    paddingVertical: 8,
    paddingHorizontal: 10,
  },
  tileHighlighted: { backgroundColor: colors.primarySoft, borderColor: colors.primary },
  tileValue: { color: colors.text, fontSize: 21, fontWeight: '700' },
  tileValueHighlighted: { color: colors.primary },
  tileLabel: { color: colors.muted, fontSize: 12, fontWeight: '500' },
  // Sayaç ve adet girişi, düğmeye kadar olan boşluğu aralarında paylaşır.
  steppers: { flexGrow: 1, flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'space-around', alignItems: 'center', alignContent: 'center', gap: 20 },
  timer: { flexGrow: 1, flexDirection: 'row', alignItems: 'center', gap: 28 },
  timerMain: { minWidth: 190, gap: 2 },
  timerCaptionOverdue: { color: colors.danger },
  timerValue: { fontSize: 42, fontWeight: '700', letterSpacing: 1, fontVariant: ['tabular-nums'] },
  timerDetail: { flex: 1, gap: 12 },
  timerTrack: { height: 6, borderRadius: 3, backgroundColor: colors.neutralSoft, overflow: 'hidden' },
  timerFill: { height: 6, borderRadius: 3 },
  timerFacts: { flexDirection: 'row', gap: 8 },
  timerFact: { flex: 1, gap: 2 },
  timerFactLabel: { color: colors.muted, fontSize: 12, fontWeight: '600' },
  timerFactValue: { color: colors.text, fontSize: 16, fontWeight: '700', fontVariant: ['tabular-nums'] },
  // Düğme iş alanının altına yaslanır; adet girişiyle arasında boşluk kalır.
  reportButton: { alignSelf: 'stretch', minHeight: 52, marginHorizontal: 20, marginTop: 16, marginBottom: 16 },
});
