import { useCallback, useEffect, useRef, useState } from 'react';
import { Keyboard, Pressable, ScrollView, StyleSheet, Text, TextInput, View } from 'react-native';
import { isRetryable } from '../api/http';
import type { KeyboardWedgeReader } from '../rfid/KeyboardWedgeReader';
import { WedgeInput } from '../rfid/WedgeInput';
import { BigButton } from '../ui/controls';
import { AuthorizationDialog, ReasonDialog } from '../ui/Dialog';
import { IdleDialog, useIdlePrompt } from '../ui/IdleDialog';
import { BusyOverlay, InfoStrip, MessageToast, Prompt, StationTopBar } from '../ui/station';
import { SidebarOperator, SidebarSignedOut, StationSidebarFrame } from '../ui/StationSidebarFrame';
import { colors, radius, TOUCH_HEIGHT_COMPACT } from '../ui/theme';
import { formatClock, formatElapsed } from '../util/time';
import { useNow } from '../util/useNow';
import { newUuid } from '../util/uuid';
import type { OrmeApi, OrmeContext, OrmeMachine, OrmeOperatorRole, OrmeStopCategory, OrmeStopReason } from './OrmeApi';
import type { RollOutbox } from './rollOutbox';

const HEARTBEAT_MS = 30_000;
const SUCCESS_MESSAGE_MS = 4000;
const number = new Intl.NumberFormat('tr-TR');

interface Props {
  api: OrmeApi;
  outbox: RollOutbox;
  reader: KeyboardWedgeReader;
  tabletCode: string;
  onOpenSetup: () => void;
}

type Message = { kind: 'error' | 'success' | 'info'; text: string };

const flowSteps = ['Operatör', 'Makine'];

type AuthorizedAction = 'pause' | 'resume' | 'breakdown' | 'recover';

/** Kart bekleyen işlemin penceresi: başlık ve kartını okutabilecek görevler. */
const authorizations: Record<AuthorizedAction, { title: string; roles: string[] }> = {
  pause: { title: 'Ara onayı', roles: ['Vardiya amiri', 'Yetkili operatör (kendi kartı)'] },
  resume: { title: 'Devam onayı', roles: ['Vardiya amiri', 'Yetkili operatör (kendi kartı)'] },
  breakdown: { title: 'Arıza bildirimi onayı', roles: ['Vardiya amiri', 'Bakım', 'Yetkili operatör (kendi kartı)'] },
  recover: { title: 'Arızayı kapat', roles: ['Vardiya amiri', 'Bakım'] },
};

const operatorRoleLabels: Record<OrmeOperatorRole, string> = {
  KNITTER: 'Örme operatörü',
  SUPERVISOR: 'Vardiya amiri',
  MAINTENANCE: 'Bakım',
};

const operatorStatus = {
  active: { label: 'Çalışıyor', color: '#3ecf8e' },
  break: { label: 'Molada', color: '#f0bd5b' },
  paused: { label: 'Ara verdi', color: '#f0bd5b' },
  waiting: { label: 'Emir bekliyor', color: '#4fc3d9' },
  breakdown: { label: 'Makine arızalı', color: '#ff8a80' },
};

const stopLabel: Record<Exclude<OrmeStopCategory, 'PAUSE'>, string> = {
  STOP: 'Duruşta',
  BREAKDOWN: 'Arızalı',
  PLANNED: 'Planlı duruşta',
};

const machineState = (machine: OrmeMachine) =>
  machine.openStopCategory && machine.openStopCategory !== 'PAUSE'
    ? { label: stopLabel[machine.openStopCategory], color: machine.openStopCategory === 'BREAKDOWN' ? colors.danger : colors.warning }
    : machine.knitOrderId !== null ? { label: 'Çalışıyor', color: colors.success }
    : { label: 'Emir yok', color: colors.muted };

const reasonOption = (reason: OrmeStopReason) => ({ ...reason, code: String(reason.id) });

/** Tabletin yerel tarihi: top kuyrukta bekleyip sonradan gönderilse de çıktığı günle kaydolur. */
const localDate = () => {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
};

const decimal = (text: string) => Number(text.replace(',', '.'));

/**
 * Örme istasyonu: operatör kartı oturum açar; makinede top tartılıp kaydedilir ve duruş bildirilir. Mola, ara ve arıza Konfeksiyon
 * ile aynı işler: ara ve arıza yetkili kartı ister. Tablet makineye bağlıysa yalnızca o makine, bağlı değilse (salon
 * tableti) bütün makineler görünür. Top kaydı bağlantı yoksa kuyrukta bekler, diğer işlemler sunucu ister.
 */
export function OrmeStationScreen({ api, outbox, reader, tabletCode, onOpenSetup }: Props) {
  const [context, setContext] = useState<OrmeContext | null>(null);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [message, setMessage] = useState<Message | null>(null);
  const [busy, setBusy] = useState(false);
  const [pending, setPending] = useState(0);
  const [editing, setEditing] = useState(false);
  /** Neden seçimi açık: ara ver, duruş ya da arıza. */
  const [picker, setPicker] = useState<'pause' | 'stop' | 'breakdown' | null>(null);
  /** Yetkili kartı bekleyen işlem. */
  const [authorization, setAuthorization] = useState<{ action: AuthorizedAction; reasonId: number | null } | null>(null);
  const [authorizationError, setAuthorizationError] = useState<string | null>(null);
  const [operatorPhoto, setOperatorPhoto] = useState<string | null>(null);
  const [online, setOnline] = useState(true);
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
      setMessage({ kind: 'success', text: `Bekleyen ${result.sent} top gönderildi.` });
    }
    return result;
  }, [api, outbox]);

  const synchronize = useCallback(async () => {
    try {
      await flush();
      setContext(await api.getContext());
      setOnline(true);
      setLoadError(null);
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
      // Duruş ve emir ofisten de değişir; ekran kendini günceller.
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

  // Başarı iletileri kendiliğinden kapanır; hata ve bilgi iletileri operatör kapatana kadar kalır.
  useEffect(() => {
    if (message?.kind !== 'success') {
      return undefined;
    }

    const timer = setTimeout(() => setMessage(null), SUCCESS_MESSAGE_MS);
    return () => clearTimeout(timer);
  }, [message]);

  const operatorId = context?.operator?.id ?? null;
  useEffect(() => {
    let cancelled = false;
    setOperatorPhoto(null);
    if (operatorId !== null) {
      api.getSessionPhoto()
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

  const machines = context?.machines ?? [];
  const selected = machines.find(machine => machine.machineId === selectedId) ?? (machines.length === 1 ? machines[0] : null);
  const pause = context?.pause ?? null;
  const canPause = Boolean(context?.operator) && !pause;
  const waiting = pause?.kind === 'WAITING_FOR_ORDER';
  // Emir beklerken makine işlemleri açıktır: top kaydı ya da duruş bekleme kaydını kapatır.
  const canWork = Boolean(context?.operator) && (!pause || waiting);
  const dueBreak = context?.dueBreak ?? null;
  const fixedMachine = context?.machineId ? machines[0] : null;
  // Boşta sorusu: makinenin emri yoksa sorulur; salon tabletinde operatör başka makinelere bakar, sorulmaz.
  const idle = useIdlePrompt(canPause && fixedMachine !== null && fixedMachine !== undefined && fixedMachine.knitOrderId === null && !fixedMachine.openStopCategory, context?.idlePromptMinutes ?? 0, now);
  const stopReasons = context?.stopReasons.filter(reason => reason.category !== 'BREAKDOWN') ?? [];
  const breakdownReasons = context?.stopReasons.filter(reason => reason.category === 'BREAKDOWN') ?? [];

  /** Okutulan yetkili kartıyla bekleyen işlemi tamamlar; kart yetkili değilse bekleme sürer ve neden gösterilir. */
  const authorize = (pending_: NonNullable<typeof authorization>, uid: string) => {
    setBusy(true);
    setAuthorizationError(null);
    const action = async () => {
      if (pending_.action === 'pause') {
        // Kuyruktaki top araya kalmasın.
        await flush();
        return api.pause('PAUSE', pending_.reasonId, uid);
      }

      if (pending_.action === 'resume') {
        return api.resume(uid);
      }

      if (!selected) {
        throw new Error('Önce makine seçin.');
      }

      return pending_.action === 'breakdown'
        ? api.startStop(selected.machineId, pending_.reasonId!, uid)
        : api.endStop(selected.machineId, uid);
    };
    action()
      .then(next => {
        setContext(next);
        setOnline(true);
        setAuthorization(null);
        if (pending_.action === 'breakdown') {
          setMessage({ kind: 'info', text: 'Arıza bildirildi.' });
        } else if (pending_.action === 'recover') {
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
    // Arızalı makinede okutulan kart arızayı kapatan yetkilinin kartıdır; duraklamada kart okunmaz; aksi hâlde oturum devralınır.
    const action = !context?.operator
      ? api.login(code).then(setContext)
      : selected?.openStopCategory === 'BREAKDOWN'
        ? api.endStop(selected.machineId, code).then(setContext)
        : pause && !waiting
          ? Promise.resolve(setMessage({ kind: 'info', text: 'Önce işe dönün.' }))
          : api.login(code).then(setContext);
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
    setSelectedId(null);
  });

  const requireCard = (action: AuthorizedAction, reasonId: number | null) => {
    Keyboard.dismiss();
    setPicker(null);
    setAuthorizationError(null);
    setAuthorization({ action, reasonId });
  };

  const takeBreak = () => run(async () => {
    await flush();
    setContext(await api.pause('BREAK', null));
  });

  const waitForOrder = () => run(async () => {
    setContext(await api.pause('WAITING_FOR_ORDER', null));
  });

  // Aradan dönüş kart ister; moladan dönüş serbesttir.
  const resume = () => {
    if (pause?.kind === 'PAUSE') {
      requireCard('resume', null);
      return;
    }

    void run(async () => {
      setContext(await api.resume());
    });
  };

  const startStop = (machine: OrmeMachine, reasonId: number) => run(async () => {
    setPicker(null);
    setContext(await api.startStop(machine.machineId, reasonId));
  });

  const endStop = (machine: OrmeMachine) => run(async () => {
    setContext(await api.endStop(machine.machineId));
  });

  const cutRoll = (machine: OrmeMachine, kilograms: number, meters: number | undefined, notes: string | undefined, done: () => void) => run(async () => {
    if (machine.knitOrderId === null) {
      return;
    }

    const body = { requestId: newUuid(), machineId: machine.machineId, cutOn: localDate(), kilograms, meters, notes };
    try {
      const roll = await api.cutRoll(machine.knitOrderId, body);
      setMessage({ kind: 'success', text: `Top ${roll.rollNo} kaydedildi: ${number.format(roll.kilograms)} kg.` });
      setContext(await api.getContext());
    } catch (error) {
      if (!isRetryable(error)) {
        throw error;
      }

      await outbox.add({ knitOrderId: machine.knitOrderId, knitOrderNo: machine.knitOrderNo ?? '', body });
      setPending((await outbox.list()).length);
      setMessage({ kind: 'info', text: 'Bağlantı yok. Top kaydedildi, bağlantı gelince gönderilecek.' });
    }
    done();
  });

  const remaining = selected?.remainingKilograms ?? null;
  const fixedState = fixedMachine ? machineState(fixedMachine) : null;
  const statusOfOperator = pause?.kind === 'BREAK' ? operatorStatus.break
    : waiting ? operatorStatus.waiting
    : pause ? operatorStatus.paused
    : fixedMachine?.openStopCategory === 'BREAKDOWN' ? operatorStatus.breakdown
    : operatorStatus.active;
  const pauseTitle = pause?.kind === 'BREAK' ? (pause.breakName ? `Moladasınız · ${pause.breakName}` : 'Moladasınız')
    : waiting ? 'Emir bekleniyor'
    : `Ara verdiniz${pause?.reasonName ? ` · ${pause.reasonName}` : ''}`;
  const authorizationReason = authorization?.action === 'resume'
    ? pause?.reasonName
    : authorization?.action === 'recover'
      ? selected?.openStopReason
      : [...(context?.pauseReasons ?? []), ...(context?.stopReasons ?? [])].find(reason => reason.id === authorization?.reasonId)?.name;

  return (
    <View style={styles.screen} testID="orme-station">
      <WedgeInput reader={reader} paused={editing} />

      <StationSidebarFrame
        moduleName="Örme"
        title={fixedMachine?.machineCode ?? tabletCode}
        subtitle={context ? fixedMachine?.machineName ?? 'Salon tableti' : undefined}
        details={[{ label: 'Tablet', value: tabletCode }]}
        status={fixedState ? { label: `Makine: ${fixedState.label}`, color: fixedState.color } : undefined}
        statusReason={fixedMachine?.openStopReason ?? null}
        onOpenSetup={onOpenSetup}
        personnelCaption="OPERATÖR"
        // Düğme hep yerinde durur (alanlar kaymaz); oturum yokken pasiftir.
        footer={
          <BigButton label="Oturumu kapat" tone="ghost" size="compact" onPress={logout} disabled={busy || !context?.operator} testID="orme-logout" />
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
          current={!context ? -1 : !context.operator ? 0 : 1}
          online={online}
          now={now}
          actions={
            context?.operator && selected?.openStopCategory === 'BREAKDOWN' ? (
              <BigButton
                label="Arızayı kapat"
                tone="success"
                size="compact"
                onPress={() => requireCard('recover', null)}
                disabled={busy}
                testID="orme-recover"
              />
            ) : canWork && selected && !selected.openStopCategory ? (
              <BigButton
                label="Arıza bildir"
                tone="dangerOutline"
                size="compact"
                // Arıza nedensiz bildirilmez.
                onPress={() => (breakdownReasons.length > 0
                  ? setPicker('breakdown')
                  : setMessage({ kind: 'error', text: 'Tanımlı arıza nedeni yok. Web\'de Duruş Nedenleri ekranından ekleyin.' }))}
                disabled={busy}
                testID="orme-breakdown"
              />
            ) : undefined
          }
        />

        {context && selected && (
          <InfoStrip
            cells={[
              { label: 'MAKİNE', value: selected.machineCode },
              { label: 'EMİR', value: selected.knitOrderNo ?? 'Yok' },
              { label: 'KUMAŞ', value: selected.fabricCode ?? '—' },
              { label: 'KALAN', value: remaining !== null ? `${number.format(remaining)} kg` : '—' },
            ]}
          />
        )}

        <View style={styles.content}>
          <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
            {pending > 0 && (
              <View style={styles.pending}>
                <Text style={styles.pendingText} testID="orme-pending">
                  {pending} top gönderilmeyi bekliyor. Bağlantı gelince kendiliğinden gönderilir.
                </Text>
              </View>
            )}

            {context?.operator && pause && (
              <View style={[styles.bar, styles.barInterrupted]} accessibilityRole="alert">
                <View style={styles.barText}>
                  <Text style={styles.barCaption}>ÇALIŞMA DURAKLATILDI</Text>
                  <Text style={styles.barTitle} testID="orme-session-status">{pauseTitle}</Text>
                </View>
                <BigButton label="Devam et" tone="success" size="compact" busy={busy} onPress={resume} testID="orme-resume" />
              </View>
            )}

            {canPause && (
              <View style={styles.bar}>
                <View style={styles.barText}>
                  {dueBreak ? (
                    <>
                      <Text style={styles.barCaption}>MOLA ZAMANI</Text>
                      <Text style={styles.barTitle} testID="orme-due-break">{dueBreak.name} ({dueBreak.plannedMinutes} dk)</Text>
                    </>
                  ) : (
                    <>
                      <Text style={styles.barCaption}>ÇALIŞMA DURUMU</Text>
                      <Text style={styles.barTitle}>Çalışıyor</Text>
                    </>
                  )}
                </View>
                <View style={styles.barActions}>
                  {dueBreak && <BigButton label="Molaya çık" size="compact" onPress={takeBreak} disabled={busy} testID="orme-break" />}
                  {context?.pauseReasons.length ? (
                    <BigButton label="Ara ver" tone="primaryOutline" size="compact" onPress={() => setPicker('pause')} disabled={busy} testID="orme-pause" />
                  ) : null}
                  {fixedMachine && fixedMachine.knitOrderId === null && (
                    <BigButton label="Emir bekliyorum" tone="primaryOutline" size="compact" onPress={waitForOrder} disabled={busy} testID="orme-waiting" />
                  )}
                </View>
              </View>
            )}

            {!context ? (
              loadError ? (
                <Prompt glyph="alert" title="Tablet bilgileri alınamadı" detail={loadError}>
                  <BigButton label="Tekrar dene" onPress={() => void synchronize()} testID="orme-retry" />
                  <BigButton label="Sistem ayarları" tone="neutral" onPress={onOpenSetup} testID="orme-open-setup" />
                </Prompt>
              ) : (
                <Prompt
                  glyph={online ? 'loading' : 'alert'}
                  title={online ? 'Sunucuya bağlanılıyor' : 'Sunucuya ulaşılamıyor'}
                  detail={online ? undefined : 'Wi-Fi bağlantısını kontrol edin. Bağlantı gelince kendiliğinden devam edilir.'}>
                  {!online && <BigButton label="Tekrar dene" onPress={() => void synchronize()} testID="orme-retry" />}
                </Prompt>
              )
            ) : !context.operator ? (
              <Prompt glyph="card" title="Personel kartınızı okutun" detail="Çalışmaya başlamak için kartınızı okuyucuya gösterin." />
            ) : pause && !waiting ? (
              <Prompt glyph="bundle" title="Çalışma duraklatıldı" detail="Top kaydetmek ya da duruş bildirmek için önce çalışmaya dönün." />
            ) : !selected ? (
              <MachineList machines={machines} onSelect={setSelectedId} />
            ) : (
              <View style={styles.work}>
                <MachineCard
                  machine={selected}
                  canGoBack={machines.length > 1}
                  busy={busy}
                  onBack={() => setSelectedId(null)}
                  onStop={() => (stopReasons.length > 0
                    ? setPicker('stop')
                    : setMessage({ kind: 'error', text: 'Tanımlı duruş nedeni yok. Web\'de Duruş Nedenleri ekranından ekleyin.' }))}
                  onEndStop={() => endStop(selected)}
                />
                {selected.knitOrderId !== null && (
                  <CutForm key={selected.knitOrderId} busy={busy} onEditing={setEditing} onCut={(kilograms, meters, notes, done) => cutRoll(selected, kilograms, meters, notes, done)} />
                )}
              </View>
            )}
          </ScrollView>

          {message && <MessageToast kind={message.kind} text={message.text} onDismiss={() => setMessage(null)} testID="orme-message" />}
          {busy && <BusyOverlay />}
        </View>
      </View>

      {idle.due && !picker && !authorization && !busy && (
        <IdleDialog
          minutes={idle.minutes}
          waitingLabel="Emir bekliyorum"
          description="bu makinede emir görünmüyor."
          help="Emir bekliyorsanız bildirin; bekleme süresi veriminize yansımaz."
          canPause={Boolean(context?.pauseReasons.length)}
          busy={busy}
          onDismiss={idle.acknowledge}
          onPause={() => setPicker('pause')}
          onWait={waitForOrder}
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
          testIDPrefix="orme-pause-reason"
        />
      )}

      {picker === 'stop' && canWork && selected && (
        <ReasonDialog
          title="Duruş bildir"
          description={`${selected.machineCode} için duruş kaydı açılır.`}
          reasons={stopReasons.map(reasonOption)}
          confirmLabel="Duruşu başlat"
          busy={busy}
          onConfirm={reasonId => startStop(selected, reasonId)}
          onCancel={() => setPicker(null)}
          testIDPrefix="orme-stop-reason"
        />
      )}

      {picker === 'breakdown' && canWork && selected && (
        <ReasonDialog
          title="Arıza bildir"
          description={`${selected.machineCode} için arıza kaydı açılır.`}
          reasons={breakdownReasons.map(reasonOption)}
          confirmLabel="Arızayı bildir"
          tone="danger"
          busy={busy}
          onConfirm={reasonId => requireCard('breakdown', reasonId)}
          onCancel={() => setPicker(null)}
          testIDPrefix="orme-breakdown-reason"
        />
      )}

      {authorization && (
        <AuthorizationDialog
          title={authorizations[authorization.action].title}
          roles={authorizations[authorization.action].roles}
          details={[
            ...(selected && authorization.action !== 'pause' && authorization.action !== 'resume' ? [{ label: 'Makine', value: selected.machineCode }] : []),
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

/** Salon tabletinde makine seçimi. */
function MachineList({ machines, onSelect }: { machines: OrmeMachine[]; onSelect: (machineId: number) => void }) {
  if (machines.length === 0) {
    return <Prompt glyph="alert" title="Etkin makine yok" detail="Makine kartları ofisten tanımlanır." />;
  }

  return (
    <View style={styles.list}>
      {machines.map(machine => {
        const state = machineState(machine);
        return (
          <Pressable
            key={machine.machineId}
            style={({ pressed }) => [styles.listRow, pressed && styles.listRowPressed]}
            onPress={() => onSelect(machine.machineId)}
            testID={`orme-machine-${machine.machineCode}`}>
            <View style={styles.listMain}>
              <Text style={styles.cellStrong}>{machine.machineCode}</Text>
              <Text style={styles.cellMuted}>{machine.fabricCode ? `${machine.fabricCode} · ${machine.knitOrderNo}` : machine.machineName}</Text>
            </View>
            {machine.knitOrderId !== null && <Text style={styles.cellMuted}>{number.format(machine.knittedKilograms ?? 0)} / {number.format(machine.plannedKilograms ?? 0)} kg</Text>}
            <Text style={[styles.state, { color: state.color }]}>{state.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

interface MachineCardProps {
  machine: OrmeMachine;
  canGoBack: boolean;
  busy: boolean;
  onBack: () => void;
  onStop: () => void;
  onEndStop: () => void;
}

function MachineCard({ machine, canGoBack, busy, onBack, onStop, onEndStop }: MachineCardProps) {
  const state = machineState(machine);
  return (
    <View style={styles.card}>
      <View style={styles.row}>
        <View style={styles.listMain}>
          <Text style={styles.machineCode}>{machine.machineCode}</Text>
          {machine.knitOrderId !== null && (
            <Text style={styles.meta}>
              {machine.orderNo}{machine.customerName ? ` · ${machine.customerName}` : ''} · {number.format(machine.knittedKilograms ?? 0)} / {number.format(machine.plannedKilograms ?? 0)} kg · {machine.rollCount} top
            </Text>
          )}
        </View>
        <Text style={[styles.state, { color: state.color }]} testID="orme-machine-state">{state.label}</Text>
        {canGoBack && <BigButton label="Makineler" tone="neutral" size="compact" onPress={onBack} testID="orme-back" />}
      </View>

      {machine.openStopCategory ? (
        <View style={styles.stopBar}>
          <Text style={styles.stopText} testID="orme-stop-reason-text">{machine.openStopReason}</Text>
          {machine.openStopCategory === 'BREAKDOWN' ? (
            <Text style={styles.help}>Arızayı kapatmak için vardiya amiri ya da bakım personeli kartını okutsun.</Text>
          ) : (
            <BigButton label="Duruşu bitir" tone="success" size="compact" onPress={onEndStop} busy={busy} testID="orme-end-stop" />
          )}
        </View>
      ) : (
        <View style={styles.actions}>
          <BigButton label="Duruş bildir" tone="primaryOutline" size="compact" onPress={onStop} disabled={busy} testID="orme-stop" />
        </View>
      )}
    </View>
  );
}

interface CutFormProps {
  busy: boolean;
  onEditing: (editing: boolean) => void;
  onCut: (kilograms: number, meters: number | undefined, notes: string | undefined, done: () => void) => void;
}

function CutForm({ busy, onEditing, onCut }: CutFormProps) {
  const [kilograms, setKilograms] = useState('');
  const [meters, setMeters] = useState('');
  const [notes, setNotes] = useState('');
  const kilogramsValue = decimal(kilograms);
  const metersValue = meters.trim() === '' ? undefined : decimal(meters);
  const valid = kilogramsValue > 0 && (metersValue === undefined || metersValue > 0);

  const clear = () => {
    setMeters('');
    setKilograms('');
    setNotes('');
  };

  return (
    <View style={styles.card}>
      <Text style={styles.section}>Top kaydet</Text>
      <View style={styles.formRow}>
        <View style={styles.field}>
          <Text style={styles.label}>Tartılan kilo</Text>
          <TextInput
            style={styles.input}
            keyboardType="decimal-pad"
            value={kilograms}
            onFocus={() => onEditing(true)}
            onBlur={() => onEditing(false)}
            onChangeText={setKilograms}
            testID="orme-cut-kilograms"
          />
        </View>
        <View style={styles.field}>
          <Text style={styles.label}>Metre (ölçüldüyse)</Text>
          <TextInput
            style={styles.input}
            keyboardType="decimal-pad"
            value={meters}
            onFocus={() => onEditing(true)}
            onBlur={() => onEditing(false)}
            onChangeText={setMeters}
            testID="orme-cut-meters"
          />
        </View>
        <View style={styles.fieldGrow}>
          <Text style={styles.label}>Not</Text>
          <TextInput
            style={[styles.input, styles.inputText]}
            maxLength={500}
            value={notes}
            onFocus={() => onEditing(true)}
            onBlur={() => onEditing(false)}
            onChangeText={setNotes}
            testID="orme-cut-notes"
          />
        </View>
      </View>
      <BigButton
        label="Topu kaydet"
        onPress={() => onCut(kilogramsValue, metersValue, notes.trim() || undefined, clear)}
        disabled={!valid}
        busy={busy}
        testID="orme-cut"
      />
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
  barActions: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  pending: {
    backgroundColor: colors.warningSoft,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 8,
    paddingHorizontal: 20,
  },
  pendingText: { color: colors.text, fontSize: 14, fontWeight: '600' },
  list: { padding: 16, gap: 6 },
  listRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    minHeight: TOUCH_HEIGHT_COMPACT,
    paddingVertical: 8,
    paddingHorizontal: 12,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
  },
  listRowPressed: { backgroundColor: colors.neutralSoft },
  listMain: { flex: 1, gap: 2 },
  cellStrong: { color: colors.text, fontSize: 15, fontWeight: '600' },
  cellMuted: { color: colors.muted, fontSize: 13 },
  state: { fontSize: 13, fontWeight: '700' },
  card: {
    gap: 10,
    padding: 16,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
  },
  row: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  machineCode: { color: colors.text, fontSize: 20, fontWeight: '700' },
  meta: { color: colors.textSecondary, fontSize: 14 },
  stopBar: {
    gap: 8,
    padding: 10,
    borderLeftWidth: 4,
    borderLeftColor: colors.warning,
    backgroundColor: colors.warningSoft,
    alignItems: 'flex-start',
  },
  stopText: { color: colors.text, fontSize: 15, fontWeight: '700' },
  help: { color: colors.textSecondary, fontSize: 14 },
  actions: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
  section: { color: colors.text, fontSize: 16, fontWeight: '700' },
  formRow: { flexDirection: 'row', flexWrap: 'wrap', gap: 10 },
  field: { width: 150, gap: 4 },
  fieldGrow: { flex: 1, minWidth: 200, gap: 4 },
  label: { color: colors.muted, fontSize: 13 },
  input: {
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
  inputText: { textAlign: 'left' },
});
