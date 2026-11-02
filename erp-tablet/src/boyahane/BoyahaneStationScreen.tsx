import { useCallback, useEffect, useRef, useState } from 'react';
import { Keyboard, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { isRetryable } from '../api/http';
import type { KeyboardWedgeReader } from '../rfid/KeyboardWedgeReader';
import { WedgeInput } from '../rfid/WedgeInput';
import { Banner, BigButton } from '../ui/controls';
import { AuthorizationDialog, ReasonDialog } from '../ui/Dialog';
import { IdleDialog, useIdlePrompt } from '../ui/IdleDialog';
import { BusyOverlay, InfoStrip, MessageToast, Prompt, StationTopBar } from '../ui/station';
import { SidebarOperator, SidebarSignedOut, StationSidebarFrame } from '../ui/StationSidebarFrame';
import { colors, radius, TOUCH_HEIGHT_COMPACT } from '../ui/theme';
import { formatClock, formatElapsed } from '../util/time';
import { useNow } from '../util/useNow';
import { newUuid } from '../util/uuid';
import type {
  BoyahaneApi,
  BoyahaneBatch,
  BoyahaneBatchStep,
  BoyahaneContext,
  BoyahaneOperatorRole,
  BoyahaneReason,
  ColorCheckResult,
  CompleteStepBody,
  FinalQualityResult,
} from './BoyahaneApi';
import type { CompletionOutbox } from './completionOutbox';

const HEARTBEAT_MS = 30_000;
const number = new Intl.NumberFormat('tr-TR');

interface Props {
  api: BoyahaneApi;
  outbox: CompletionOutbox;
  reader: KeyboardWedgeReader;
  tabletCode: string;
  onOpenSetup: () => void;
}

type Message = { kind: 'error' | 'success' | 'info'; text: string };

const flowSteps = ['Operatör', 'Parti', 'Üretim'];

type AuthorizedAction = 'pause' | 'resume' | 'breakdown' | 'recover';

/** Kart bekleyen işlemin penceresi: başlık ve kartını okutabilecek görevler. */
const authorizations: Record<AuthorizedAction, { title: string; roles: string[] }> = {
  pause: { title: 'Ara onayı', roles: ['Vardiya amiri', 'Yetkili operatör (kendi kartı)'] },
  resume: { title: 'Devam onayı', roles: ['Vardiya amiri', 'Yetkili operatör (kendi kartı)'] },
  breakdown: { title: 'Arıza bildirimi onayı', roles: ['Vardiya amiri', 'Bakım', 'Yetkili operatör (kendi kartı)'] },
  recover: { title: 'Arızayı kapat', roles: ['Vardiya amiri', 'Bakım'] },
};

const operatorRoleLabels: Record<BoyahaneOperatorRole, string> = {
  OPERATOR: 'Operatör',
  SUPERVISOR: 'Vardiya amiri',
  MAINTENANCE: 'Bakım',
};

const machineStatus: Record<'AVAILABLE' | 'BREAKDOWN' | 'MAINTENANCE', { label: string; color: string }> = {
  AVAILABLE: { label: 'Müsait', color: '#3ecf8e' },
  BREAKDOWN: { label: 'Arızalı', color: '#ff8a80' },
  MAINTENANCE: { label: 'Bakımda', color: '#f0bd5b' },
};

const operatorStatus = {
  active: { label: 'Çalışıyor', color: '#3ecf8e' },
  break: { label: 'Molada', color: '#f0bd5b' },
  paused: { label: 'Ara verdi', color: '#f0bd5b' },
  waiting: { label: 'Parti bekliyor', color: '#4fc3d9' },
  breakdown: { label: 'Makine arızalı', color: '#ff8a80' },
};

const reasonOption = (reason: BoyahaneReason) => ({ ...reason, code: String(reason.id) });

const stepName = (step: BoyahaneBatchStep) =>
  step.reworkKind === 'ADDITION' ? `${step.operationName} (ilave)`
  : step.reworkKind === 'REDO' ? `${step.operationName} (yeniden işlem)`
  : step.operationName;

/**
 * Boyahane istasyonu: personel kartı oturum açar, parti kartı (RFID ya da kağıt karttaki barkod) partiyi getirir,
 * sıradaki adım başlatılır ve bitirilir. Başlatma sunucu ister; bitirme bağlantı yoksa kuyrukta bekler.
 */
export function BoyahaneStationScreen({ api, outbox, reader, tabletCode, onOpenSetup }: Props) {
  const [context, setContext] = useState<BoyahaneContext | null>(null);
  const [batch, setBatch] = useState<BoyahaneBatch | null>(null);
  const [message, setMessage] = useState<Message | null>(null);
  const [busy, setBusy] = useState(false);
  const [pending, setPending] = useState(0);
  const [editing, setEditing] = useState(false);
  /** Neden seçimi açık: ara ver ya da arıza bildir. */
  const [picker, setPicker] = useState<'pause' | 'breakdown' | null>(null);
  /** Yetkili kartı bekleyen işlem. */
  const [authorization, setAuthorization] = useState<{ action: AuthorizedAction; reasonId: number | null } | null>(null);
  const [authorizationError, setAuthorizationError] = useState<string | null>(null);
  const [operatorPhoto, setOperatorPhoto] = useState<string | null>(null);
  const [online, setOnline] = useState(true);
  /** Sunucu isteği reddettiyse (ör. cihaz anahtarı geçersiz) nedeni; bağlantı sorunu değildir. */
  const [loadError, setLoadError] = useState<string | null>(null);
  const now = useNow();

  const showError = useCallback((error: unknown) => {
    if (isRetryable(error)) {
      setOnline(false);
    }
    setMessage({
      kind: 'error',
      text: isRetryable(error) ? 'Sunucuya ulaşılamıyor. Bağlantıyı kontrol edin.' : error instanceof Error ? error.message : 'İşlem yapılamadı.',
    });
  }, []);

  const flush = useCallback(async () => {
    const result = await outbox.flush(api);
    setPending(result.pending);
    if (result.rejected.length > 0) {
      setMessage({ kind: 'error', text: result.rejected.join(' ') });
    } else if (result.sent > 0) {
      setMessage({ kind: 'success', text: `Bekleyen ${result.sent} işlem gönderildi.` });
    }
    return result;
  }, [api, outbox]);

  const synchronize = useCallback(async () => {
    try {
      await flush();
      const next = await api.getContext();
      setContext(next);
      setOnline(true);
      setLoadError(null);
      if (next.currentBatchId) {
        setBatch(await api.getBatch(next.currentBatchId));
      }
    } catch (error) {
      showError(error);
      if (!isRetryable(error)) {
        setLoadError(error instanceof Error ? error.message : 'Sunucu isteği reddetti.');
      }
    }
  }, [api, flush, showError]);

  useEffect(() => {
    void synchronize();
    const timer = setInterval(() => {
      api.heartbeat().catch(() => {});
      flush().catch(() => {});
      // Arıza ve bakım ofisten de işlenir; ekran kendini günceller.
      api.getContext()
        .then(next => {
          setContext(next);
          setOnline(true);
          setLoadError(null);
        })
        .catch(error => {
          if (isRetryable(error)) {
            setOnline(false);
          }
        });
    }, HEARTBEAT_MS);
    return () => clearInterval(timer);
  }, [api, flush, synchronize]);

  /** Okutulan yetkili kartıyla bekleyen işlemi tamamlar; kart yetkili değilse bekleme sürer ve neden gösterilir. */
  const authorize = (pending: NonNullable<typeof authorization>, uid: string) => {
    setBusy(true);
    setAuthorizationError(null);
    const action = pending.action === 'pause' ? api.pause('PAUSE', pending.reasonId, uid)
      : pending.action === 'resume' ? api.resume(uid)
      : pending.action === 'breakdown' ? api.reportBreakdown(pending.reasonId!, uid)
      : api.reportRecovery(uid);
    action
      .then(next => {
        setContext(next);
        setOnline(true);
        setAuthorization(null);
        if (pending.action === 'breakdown') {
          setMessage({ kind: 'info', text: 'Arıza bildirildi.' });
        } else if (pending.action === 'recover') {
          setMessage({ kind: 'success', text: 'Makine yeniden çalışır durumda.' });
        }
      })
      .catch(error => {
        if (isRetryable(error)) {
          setOnline(false);
        }
        setAuthorizationError(isRetryable(error) ? 'Sunucuya ulaşılamıyor. Bağlantıyı kontrol edin.' : error instanceof Error ? error.message : 'İşlem yapılamadı.');
      })
      .finally(() => setBusy(false));
  };

  // Okuyucu geri çağrısı her zaman son durumu görsün diye ref üzerinden.
  const handleCard = useRef<(code: string) => void>(() => {});
  handleCard.current = (code: string) => {
    if (busy) {
      return;
    }

    if (authorization) {
      authorize(authorization, code);
      return;
    }

    setBusy(true);
    setMessage(null);
    // Arızada okutulan kart arızayı kapatan yetkilinin kartıdır; duraklamada kart okunmaz.
    const action = !context?.operator
      ? api.login(code).then(setContext)
      : context.pause?.kind === 'MACHINE_BREAKDOWN'
        ? api.reportRecovery(code).then(setContext)
        : context.pause && context.pause.kind !== 'WAITING_FOR_BATCH'
          ? Promise.resolve(setMessage({ kind: 'info', text: 'Önce işe dönün.' }))
          : api.readBatch(code).then(setBatch);
    action.then(() => setOnline(true)).catch(showError).finally(() => setBusy(false));
  };

  useEffect(() => {
    void reader.start(code => handleCard.current(code));
    return () => {
      void reader.stop();
    };
  }, [reader]);

  const run = async (action: () => Promise<void>) => {
    // Odak metin alanında kalmasın: sonraki kart okuması kart alanına gitmelidir.
    Keyboard.dismiss();
    setBusy(true);
    setMessage(null);
    try {
      await action();
      setOnline(true);
    } catch (error) {
      showError(error);
    } finally {
      setBusy(false);
    }
  };

  const logout = () => run(async () => {
    setContext(await api.logout());
    setBatch(null);
  });

  const requireCard = (action: AuthorizedAction, reasonId: number | null) => {
    Keyboard.dismiss();
    setPicker(null);
    setAuthorizationError(null);
    setAuthorization({ action, reasonId });
  };

  // Aradan dönüş kart ister; moladan dönüş serbesttir.
  const resume = () => {
    if (context?.pause?.kind === 'PAUSE') {
      requireCard('resume', null);
      return;
    }

    void run(async () => {
      setContext(await api.resume());
    });
  };

  const takeBreak = () => run(async () => {
    setContext(await api.pause('BREAK', null));
  });

  const waitForBatch = () => run(async () => {
    setContext(await api.pause('WAITING_FOR_BATCH', null));
  });

  const current = batch?.steps.find(step => step.id === batch.currentStepId) ?? null;
  const pause = context?.pause ?? null;
  const canPause = Boolean(context?.operator) && !pause;
  const dueBreak = context?.dueBreak ?? null;
  // Boşta sorusu: oturum çalışır durumda, açık parti ve süren adım yok, makine arızalı değil.
  const idle = useIdlePrompt(canPause && !batch && !context?.currentBatchId && context?.machineStatus !== 'BREAKDOWN', context?.idlePromptMinutes ?? 0, now);

  const start = (step: BoyahaneBatchStep) => run(async () => {
    if (batch) {
      setBatch(await api.startStep(batch.id, step.id, newUuid()));
      // Adım başlayınca parti bekleme biter.
      setContext(await api.getContext());
    }
  });

  const complete = (step: BoyahaneBatchStep, body: Omit<CompleteStepBody, 'requestId'>) => run(async () => {
    if (!batch) {
      return;
    }

    const full: CompleteStepBody = { ...body, requestId: newUuid() };
    try {
      const updated = await api.completeStep(batch.id, step.id, full);
      setMessage({ kind: 'success', text: `${stepName(step)} bitti.` });
      const open = updated.status === 'PLANNED' || updated.status === 'ON_MACHINE';
      setBatch(open && updated.currentStepId ? updated : null);
    } catch (error) {
      if (!isRetryable(error)) {
        throw error;
      }

      await outbox.add({ batchId: batch.id, batchNo: batch.batchNo, stepId: step.id, body: full });
      setPending((await outbox.list()).length);
      setBatch(null);
      setMessage({ kind: 'info', text: 'Bağlantı yok. İşlem kaydedildi, bağlantı gelince gönderilecek.' });
    }
  });

  // Oturumu açan operatörün fotoğrafı; yeni oturumda yeniden alınır.
  const operatorId = context?.operator?.id;
  useEffect(() => {
    let cancelled = false;
    setOperatorPhoto(null);
    if (operatorId !== undefined) {
      api.getOperatorPhoto()
        .then(photo => {
          if (!cancelled) {
            setOperatorPhoto(photo);
          }
        })
        .catch(() => {});
    }
    return () => {
      cancelled = true;
    };
  }, [api, operatorId]);

  const statusOfOperator = pause?.kind === 'MACHINE_BREAKDOWN' ? operatorStatus.breakdown
    : pause?.kind === 'BREAK' ? operatorStatus.break
    : pause?.kind === 'WAITING_FOR_BATCH' ? operatorStatus.waiting
    : pause ? operatorStatus.paused
    : operatorStatus.active;
  const machineState = context?.machineStatus ? machineStatus[context.machineStatus] : null;
  const pauseTitle = pause?.kind === 'MACHINE_BREAKDOWN'
    ? `Makine arızalı${context?.machineStatusReason ? ` · ${context.machineStatusReason}` : ''}`
    : pause?.kind === 'BREAK' ? (pause.breakName ? `Moladasınız · ${pause.breakName}` : 'Moladasınız')
    : pause?.kind === 'WAITING_FOR_BATCH' ? 'Parti bekleniyor'
    : `Ara verdiniz${pause?.reasonName ? ` · ${pause.reasonName}` : ''}`;
  const authorizationReason = authorization?.action === 'recover'
    ? context?.machineStatusReason
    : authorization?.action === 'resume'
      ? context?.pause?.reasonName
      : [...(context?.pauseReasons ?? []), ...(context?.breakdownReasons ?? [])].find(reason => reason.id === authorization?.reasonId)?.name;
  const quantity = batch
    ? `${number.format(batch.totalKilograms)} kg${batch.totalMeters !== null ? ` · ${number.format(batch.totalMeters)} m` : ''}`
    : '—';

  return (
    <View style={styles.screen} testID="boyahane-station">
      <WedgeInput reader={reader} paused={editing} />

      <StationSidebarFrame
        moduleName="Boyahane"
        title={context?.machine?.code ?? tabletCode}
        subtitle={context ? context.machine?.name ?? 'Kontrol noktası' : undefined}
        meta={tabletCode}
        status={machineState ? { label: `Makine: ${machineState.label}`, color: machineState.color } : undefined}
        statusReason={context?.machineStatus && context.machineStatus !== 'AVAILABLE' ? context.machineStatusReason : null}
        onOpenSetup={onOpenSetup}
        personnelCaption="OPERATÖR"
        footer={
          context?.operator ? (
            <BigButton label="Oturumu kapat" tone="ghost" size="compact" onPress={logout} disabled={busy} testID="boyahane-logout" />
          ) : undefined
        }>
        {context?.operator ? (
          <SidebarOperator
            name={context.operator.name}
            role={operatorRoleLabels[context.operator.role]}
            code={context.operator.code}
            status={statusOfOperator}
            photoUri={operatorPhoto}
            facts={[
              { label: 'Giriş saati', value: formatClock(new Date(context.operator.loginAt)), testID: 'operator-login-at' },
              { label: 'Oturum süresi', value: formatElapsed(now.getTime() - new Date(context.operator.loginAt).getTime()), testID: 'operator-elapsed' },
            ]}
          />
        ) : (
          <SidebarSignedOut hint="Çalışmaya başlamak için personel kartınızı okutun." />
        )}
      </StationSidebarFrame>

      <View style={styles.main}>
        <StationTopBar
          steps={flowSteps}
          current={!context ? -1 : !context.operator ? 0 : !batch ? 1 : 2}
          online={online}
          now={now}
          actions={
            context?.machine && context.machineStatus === 'BREAKDOWN' ? (
              <BigButton
                label="Arızayı kapat"
                tone="success"
                size="compact"
                onPress={() => requireCard('recover', null)}
                disabled={busy}
                testID="boyahane-recover"
              />
            ) : canPause && context?.machine ? (
              <BigButton
                label="Arıza bildir"
                tone="dangerOutline"
                size="compact"
                // Arıza nedensiz bildirilmez.
                onPress={() => (context.breakdownReasons.length > 0
                  ? setPicker('breakdown')
                  : setMessage({ kind: 'error', text: 'Tanımlı arıza nedeni yok. Web\'de Duruş Nedenleri ekranından ekleyin.' }))}
                disabled={busy}
                testID="boyahane-breakdown"
              />
            ) : undefined
          }
        />

        {context && (
          <InfoStrip
            cells={[
              { label: 'PARTİ', value: batch?.batchNo ?? 'Okutulmadı' },
              { label: 'ADIM', value: current ? stepName(current) : '—' },
              { label: 'MİKTAR', value: quantity },
              { label: 'RENK', value: batch?.items[0]?.colorName ?? '—' },
            ]}
          />
        )}

        <View style={styles.content}>
          <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
            {pending > 0 && (
              <View style={styles.pending}>
                <Text style={styles.pendingText} testID="boyahane-pending">
                  {pending} işlem gönderilmeyi bekliyor. Bağlantı gelince kendiliğinden gönderilir.
                </Text>
              </View>
            )}

            {context?.operator && pause && (
              <View style={[styles.bar, styles.barInterrupted]} accessibilityRole="alert">
                <View style={styles.barText}>
                  <Text style={styles.barCaption}>ÇALIŞMA DURAKLATILDI</Text>
                  <Text style={styles.barTitle} testID="boyahane-session-status">{pauseTitle}</Text>
                </View>
                {pause.kind !== 'MACHINE_BREAKDOWN' && (
                  <BigButton label="Devam et" tone="success" size="compact" busy={busy} onPress={resume} testID="boyahane-resume" />
                )}
              </View>
            )}

            {canPause && (
              <View style={styles.bar}>
                <View style={styles.barText}>
                  {dueBreak ? (
                    <>
                      <Text style={styles.barCaption}>MOLA ZAMANI</Text>
                      <Text style={styles.barTitle} testID="boyahane-due-break">{dueBreak.name} ({dueBreak.plannedMinutes} dk)</Text>
                      {!dueBreak.canStartNow && dueBreak.hint && <Text style={styles.barHint}>{dueBreak.hint}</Text>}
                    </>
                  ) : (
                    <>
                      <Text style={styles.barCaption}>ÇALIŞMA DURUMU</Text>
                      <Text style={styles.barTitle}>Çalışıyor</Text>
                    </>
                  )}
                </View>
                <View style={styles.barActions}>
                  {dueBreak && (
                    <BigButton label="Molaya çık" size="compact" onPress={takeBreak} disabled={busy || !dueBreak.canStartNow} testID="boyahane-break" />
                  )}
                  {context?.pauseReasons.length ? (
                    <BigButton label="Ara ver" tone="neutral" size="compact" onPress={() => setPicker('pause')} disabled={busy} testID="boyahane-pause" />
                  ) : null}
                  {!batch && !context?.currentBatchId && (
                    <BigButton label="Parti bekliyorum" tone="neutral" size="compact" onPress={waitForBatch} disabled={busy} testID="boyahane-waiting" />
                  )}
                </View>
              </View>
            )}

            {context?.operator && !pause && context.machineStatus === 'MAINTENANCE' && (
              <View style={styles.notice}>
                <Banner kind="info" text={`Makine bakımda${context.machineStatusReason ? `: ${context.machineStatusReason}` : ''}. Makine adımı başlatılamaz.`} />
              </View>
            )}

            {!context ? (
              loadError ? (
                <Prompt glyph="alert" title="Tablet bilgileri alınamadı" detail={loadError}>
                  <BigButton label="Tekrar dene" onPress={() => void synchronize()} testID="boyahane-retry" />
                  <BigButton label="Sistem ayarları" tone="neutral" onPress={onOpenSetup} testID="boyahane-open-setup" />
                </Prompt>
              ) : (
                <Prompt
                  glyph={online ? 'loading' : 'alert'}
                  title={online ? 'Sunucuya bağlanılıyor' : 'Sunucuya ulaşılamıyor'}
                  detail={online ? undefined : 'Wi-Fi bağlantısını kontrol edin. Bağlantı gelince kendiliğinden devam edilir.'}>
                  {!online && <BigButton label="Tekrar dene" onPress={() => void synchronize()} testID="boyahane-retry" />}
                </Prompt>
              )
            ) : !context.operator ? (
              <Prompt
                glyph="card"
                step="1. ADIM"
                title="Personel kartınızı okutun"
                detail="Çalışmaya başlamak için kartınızı okuyucuya gösterin."
              />
            ) : pause?.kind === 'MACHINE_BREAKDOWN' ? (
              <Prompt glyph="alert" title="Makine arızalı" detail="Arızayı kapatmak için vardiya amiri ya da bakım personeli kartını okutsun." />
            ) : pause && pause.kind !== 'WAITING_FOR_BATCH' ? (
              <Prompt glyph="bundle" title="Çalışma duraklatıldı" detail="Parti kartı okutmak için önce çalışmaya dönün." />
            ) : !batch ? (
              <Prompt
                glyph="bundle"
                step="2. ADIM"
                title="Parti kartını okutun"
                detail="RFID kartı ya da kağıt karttaki barkodu okutun; partinin sıradaki adımı açılır."
              />
            ) : (
              <View style={styles.work}>
                <View style={styles.card}>
                  <View style={styles.row}>
                    <Text style={styles.batchNo}>{batch.batchNo}</Text>
                    <BigButton label="Kapat" tone="neutral" size="compact" onPress={() => setBatch(null)} testID="boyahane-close" />
                  </View>
                  {batch.liquorRatio !== null && (
                    <Text style={styles.meta}>Flotte 1:{number.format(batch.liquorRatio)} · {number.format(batch.liquorLiters ?? 0)} L</Text>
                  )}
                  <View style={styles.steps}>
                    {batch.steps.filter(step => step.status !== 'SKIPPED').map(step => (
                      <Text
                        key={step.id}
                        style={[
                          styles.step,
                          step.status === 'COMPLETED' && styles.stepDone,
                          step.id === batch.currentStepId && styles.stepCurrent,
                        ]}>
                        {stepName(step)}
                      </Text>
                    ))}
                  </View>
                </View>

                {current?.status === 'PENDING' && (
                  <View style={styles.card}>
                    <Text style={styles.section}>Sıradaki adım: {stepName(current)}</Text>
                    {current.behavior === 'DYEING' && current.reworkKind !== 'ADDITION' && <RecipeTable batch={batch} />}
                    <BigButton label="Başlat" onPress={() => start(current)} busy={busy} testID="boyahane-start" />
                  </View>
                )}

                {current?.status === 'IN_PROGRESS' && (
                  <CompleteForm
                    key={current.id}
                    batch={batch}
                    step={current}
                    chemicals={context.chemicals}
                    busy={busy}
                    onEditing={setEditing}
                    onComplete={body => complete(current, body)}
                  />
                )}
              </View>
            )}
          </ScrollView>

          {message && (
            <MessageToast kind={message.kind} text={message.text} onDismiss={() => setMessage(null)} testID="boyahane-message" />
          )}

          {busy && <BusyOverlay />}
        </View>
      </View>

      {idle.due && !picker && !authorization && !busy && (
        <IdleDialog
          minutes={idle.minutes}
          waitingLabel="Parti bekliyorum"
          description="bu tablette süren adım görünmüyor."
          help="Parti bekliyorsanız bildirin; bekleme süresi veriminize yansımaz. Parti elinizdeyse kartını okutun."
          canPause={Boolean(context?.pauseReasons.length)}
          busy={busy}
          onDismiss={idle.acknowledge}
          onPause={() => setPicker('pause')}
          onWait={waitForBatch}
        />
      )}

      {picker === 'pause' && canPause && context && (
        <ReasonDialog
          title="Ara ver"
          description="Ara nedenini seçin. Döndüğünüzde “Devam et” ile çalışmaya devam edersiniz."
          reasons={context.pauseReasons.map(reasonOption)}
          confirmLabel="Ara ver"
          busy={busy}
          onConfirm={reasonId => requireCard('pause', reasonId)}
          onCancel={() => setPicker(null)}
          testIDPrefix="boyahane-pause-reason"
        />
      )}

      {picker === 'breakdown' && canPause && context?.machine && (
        <ReasonDialog
          title="Arıza bildir"
          description={`${context.machine.code} için arıza kaydı açılır.`}
          reasons={context.breakdownReasons.map(reasonOption)}
          confirmLabel="Arızayı bildir"
          tone="danger"
          busy={busy}
          onConfirm={reasonId => requireCard('breakdown', reasonId)}
          onCancel={() => setPicker(null)}
          testIDPrefix="boyahane-breakdown-reason"
        />
      )}

      {authorization && (
        <AuthorizationDialog
          title={authorizations[authorization.action].title}
          roles={authorizations[authorization.action].roles}
          details={[
            ...(context?.machine ? [{ label: 'Makine', value: context.machine.code }] : []),
            ...(authorizationReason ? [{ label: 'Neden', value: authorizationReason }] : []),
          ]}
          error={authorizationError}
          busy={busy}
          onCancel={() => setAuthorization(null)}
        />
      )}
    </View>
  );
}


function RecipeTable({ batch }: { batch: BoyahaneBatch }) {
  return (
    <View>
      {batch.recipe.map(line => (
        <View key={line.dyeChemicalId} style={styles.tableRow}>
          <Text style={styles.cellGrow}>{line.code} {line.name}</Text>
          <Text style={styles.cellNumber}>{number.format(line.requiredKilograms)} kg</Text>
        </View>
      ))}
    </View>
  );
}

interface CompleteFormProps {
  batch: BoyahaneBatch;
  step: BoyahaneBatchStep;
  chemicals: BoyahaneContext['chemicals'];
  busy: boolean;
  onEditing: (editing: boolean) => void;
  onComplete: (body: Omit<CompleteStepBody, 'requestId'>) => void;
}

const decimal = (text: string) => Number(text.replace(',', '.'));

function CompleteForm({ batch, step, chemicals, busy, onEditing, onComplete }: CompleteFormProps) {
  const isAddition = step.reworkKind === 'ADDITION';
  const planned = step.behavior === 'DYEING' && !isAddition ? batch.recipe : [];
  const [actuals, setActuals] = useState<Record<number, string>>(() =>
    Object.fromEntries(planned.map(line => [line.dyeChemicalId, String(line.requiredKilograms)])));
  const [additions, setAdditions] = useState<Record<number, string>>({});
  const [results, setResults] = useState<Record<number, FinalQualityResult>>({});
  // Çıkış ölçüsü giriş ölçüsüyle hazır gelir; farklıysa düzeltilir.
  const [measures, setMeasures] = useState<Record<number, { kilograms?: string; meters?: string }>>({});
  const finalItems = batch.items.filter(item => item.finalResult === null);

  const consumptions = () => [
    ...planned.map(line => ({ dyeChemicalId: line.dyeChemicalId, actualKilograms: decimal(actuals[line.dyeChemicalId] ?? '0') })),
    ...Object.entries(additions)
      .filter(([, value]) => value.trim() !== '')
      .map(([id, value]) => ({ dyeChemicalId: Number(id), actualKilograms: decimal(value) })),
  ];

  const color = (colorResult: ColorCheckResult) => onComplete({ colorResult });

  return (
    <View style={styles.card}>
      <Text style={styles.section}>İşlemde: {stepName(step)}</Text>

      {step.behavior === 'DYEING' && (
        <>
          <Text style={styles.help}>{isAddition ? 'Kullanılan ilaveyi girin.' : 'Hesaplanan miktar hazır gelir; yalnızca farklıysa düzeltin.'}</Text>
          {planned.map(line => (
            <View key={line.dyeChemicalId} style={styles.tableRow}>
              <Text style={styles.cellGrow}>{line.code} {line.name}</Text>
              <Text style={styles.cellMuted}>{number.format(line.requiredKilograms)} kg</Text>
              <TextInput
                style={styles.input}
                keyboardType="decimal-pad"
                value={actuals[line.dyeChemicalId]}
                onFocus={() => onEditing(true)}
                onBlur={() => onEditing(false)}
                onChangeText={value => setActuals({ ...actuals, [line.dyeChemicalId]: value })}
              />
            </View>
          ))}
          {isAddition && chemicals.map(chemical => (
            <View key={chemical.id} style={styles.tableRow}>
              <Text style={styles.cellGrow}>{chemical.code} {chemical.name}</Text>
              <TextInput
                style={styles.input}
                keyboardType="decimal-pad"
                placeholder="kg"
                placeholderTextColor={colors.muted}
                value={additions[chemical.id] ?? ''}
                onFocus={() => onEditing(true)}
                onBlur={() => onEditing(false)}
                onChangeText={value => setAdditions({ ...additions, [chemical.id]: value })}
              />
            </View>
          ))}
          <BigButton label="Bitir" onPress={() => onComplete({ consumptions: consumptions() })} busy={busy} testID="boyahane-complete" />
        </>
      )}

      {step.behavior === 'COLOR_CHECK' && (
        <View style={styles.actions}>
          <BigButton label="Uygun" tone="success" onPress={() => color('APPROVED')} busy={busy} style={styles.action} testID="boyahane-color-approved" />
          <BigButton label="İlave" tone="neutral" onPress={() => color('ADDITION')} disabled={busy} style={styles.action} testID="boyahane-color-addition" />
          <BigButton label="Yeniden işlem" tone="dangerOutline" onPress={() => color('REDO')} disabled={busy} style={styles.action} testID="boyahane-color-redo" />
        </View>
      )}

      {step.behavior === 'FINAL_QUALITY' && (
        <>
          {finalItems.map(item => (
            <View key={item.id} style={styles.tableRow}>
              <View style={styles.cellGrowBox}>
                <Text style={styles.cellStrong}>{item.itemNo}</Text>
                <Text style={styles.cellMuted}>{item.customerName ?? 'Stoğa üretim'} · {number.format(item.kilograms)} kg</Text>
              </View>
              {(['APPROVED', 'REJECTED', 'REWORK'] as const).map(result => (
                <BigButton
                  key={result}
                  size="compact"
                  label={result === 'APPROVED' ? 'Uygun' : result === 'REJECTED' ? 'Red' : 'Yeniden'}
                  tone={results[item.id] === result ? (result === 'APPROVED' ? 'success' : 'danger') : 'neutral'}
                  onPress={() => setResults({ ...results, [item.id]: result })}
                  testID={`boyahane-final-${item.id}-${result}`}
                />
              ))}
              {results[item.id] === 'APPROVED' && (
                <TextInput
                  style={styles.input}
                  keyboardType="decimal-pad"
                  placeholder="kg"
                  placeholderTextColor={colors.muted}
                  value={measures[item.id]?.kilograms ?? String(item.kilograms)}
                  onFocus={() => onEditing(true)}
                  onBlur={() => onEditing(false)}
                  onChangeText={value => setMeasures({ ...measures, [item.id]: { ...measures[item.id], kilograms: value } })}
                  testID={`boyahane-final-kg-${item.id}`}
                />
              )}
              {results[item.id] === 'APPROVED' && item.meters !== null && (
                <TextInput
                  style={styles.input}
                  keyboardType="decimal-pad"
                  placeholder="m"
                  placeholderTextColor={colors.muted}
                  value={measures[item.id]?.meters ?? String(item.meters)}
                  onFocus={() => onEditing(true)}
                  onBlur={() => onEditing(false)}
                  onChangeText={value => setMeasures({ ...measures, [item.id]: { ...measures[item.id], meters: value } })}
                  testID={`boyahane-final-m-${item.id}`}
                />
              )}
            </View>
          ))}
          <BigButton
            label="Bitir"
            onPress={() => onComplete({
              itemResults: finalItems.map(item => ({
                itemId: item.id,
                result: results[item.id],
                ...(results[item.id] === 'APPROVED' ? {
                  kilograms: decimal(measures[item.id]?.kilograms ?? String(item.kilograms)),
                  ...(item.meters !== null ? { meters: decimal(measures[item.id]?.meters ?? String(item.meters)) } : {}),
                } : {}),
              })),
            })}
            disabled={finalItems.some(item => !results[item.id])}
            busy={busy}
            testID="boyahane-complete"
          />
        </>
      )}

      {step.behavior === 'STANDARD' && (
        <BigButton label="Bitir" onPress={() => onComplete({})} busy={busy} testID="boyahane-complete" />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, flexDirection: 'row', backgroundColor: colors.background },
  main: { flex: 1 },
  content: { flex: 1 },
  // İş alanı ekranı doldurur; yönlendirme ekranı kalan yüksekliği alır.
  body: { flexGrow: 1, backgroundColor: colors.surface },
  work: { padding: 16, gap: 12 },
  notice: { paddingHorizontal: 20, paddingTop: 12 },
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 10,
    paddingHorizontal: 20,
    gap: 10,
  },
  barInterrupted: { borderLeftColor: colors.warning, borderLeftWidth: 4, backgroundColor: colors.warningSoft },
  barText: { flex: 1, minWidth: 200, gap: 2 },
  barCaption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.2 },
  barTitle: { color: colors.text, fontSize: 16, fontWeight: '700' },
  barHint: { color: colors.textSecondary, fontSize: 13 },
  barActions: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  pending: {
    backgroundColor: colors.warningSoft,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 8,
    paddingHorizontal: 20,
  },
  pendingText: { color: colors.text, fontSize: 14, fontWeight: '600' },
  card: {
    gap: 10,
    padding: 16,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
  },
  row: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' },
  batchNo: { color: colors.text, fontSize: 20, fontWeight: '700' },
  meta: { color: colors.textSecondary, fontSize: 14 },
  steps: { flexDirection: 'row', flexWrap: 'wrap', gap: 6 },
  step: {
    paddingHorizontal: 10,
    paddingVertical: 4,
    borderRadius: radius.pill,
    backgroundColor: colors.neutralSoft,
    color: colors.textSecondary,
    fontSize: 13,
  },
  stepDone: { backgroundColor: colors.successSoft, color: colors.success },
  stepCurrent: { backgroundColor: colors.primary, color: '#ffffff', fontWeight: '700' },
  section: { color: colors.text, fontSize: 16, fontWeight: '700' },
  help: { color: colors.muted, fontSize: 14 },
  tableRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 8,
    paddingVertical: 6,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  cellGrow: { flex: 1, color: colors.text, fontSize: 14 },
  cellGrowBox: { flex: 1 },
  cellStrong: { color: colors.text, fontSize: 14, fontWeight: '600' },
  cellMuted: { color: colors.muted, fontSize: 13 },
  cellNumber: { color: colors.text, fontSize: 14, fontWeight: '700' },
  input: {
    width: 110,
    minHeight: TOUCH_HEIGHT_COMPACT,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
    color: colors.text,
    fontSize: 15,
    paddingHorizontal: 10,
    textAlign: 'right',
  },
  actions: { flexDirection: 'row', gap: 8 },
  action: { flex: 1 },
});
