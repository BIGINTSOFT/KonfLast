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
import type { DokumaApi, DokumaContext, DokumaLoom, DokumaOperatorRole, DokumaStopCategory, DokumaStopReason } from './DokumaApi';
import type { RollOutbox } from './rollOutbox';

const HEARTBEAT_MS = 30_000;
const SUCCESS_MESSAGE_MS = 4000;
const number = new Intl.NumberFormat('tr-TR');

interface Props {
  api: DokumaApi;
  outbox: RollOutbox;
  reader: KeyboardWedgeReader;
  tabletCode: string;
  onOpenSetup: () => void;
}

type Message = { kind: 'error' | 'success' | 'info'; text: string };

const flowSteps = ['Operatör', 'Tezgâh'];

type AuthorizedAction = 'pause' | 'resume' | 'breakdown' | 'recover';

/** Kart bekleyen işlemin penceresi: başlık ve kartını okutabilecek görevler. */
const authorizations: Record<AuthorizedAction, { title: string; roles: string[] }> = {
  pause: { title: 'Ara onayı', roles: ['Vardiya amiri', 'Yetkili operatör (kendi kartı)'] },
  resume: { title: 'Devam onayı', roles: ['Vardiya amiri', 'Yetkili operatör (kendi kartı)'] },
  breakdown: { title: 'Arıza bildirimi onayı', roles: ['Vardiya amiri', 'Bakım', 'Yetkili operatör (kendi kartı)'] },
  recover: { title: 'Arızayı kapat', roles: ['Vardiya amiri', 'Bakım'] },
};

const operatorRoleLabels: Record<DokumaOperatorRole, string> = {
  WEAVER: 'Dokumacı',
  SUPERVISOR: 'Vardiya amiri',
  MAINTENANCE: 'Bakım',
};

const operatorStatus = {
  active: { label: 'Çalışıyor', color: '#3ecf8e' },
  break: { label: 'Molada', color: '#f0bd5b' },
  paused: { label: 'Ara verdi', color: '#f0bd5b' },
  waiting: { label: 'Levent bekliyor', color: '#4fc3d9' },
  breakdown: { label: 'Tezgâh arızalı', color: '#ff8a80' },
};

const stopLabel: Record<Exclude<DokumaStopCategory, 'PAUSE'>, string> = {
  STOP: 'Duruşta',
  BREAKDOWN: 'Arızalı',
  PLANNED: 'Planlı duruşta',
};

const loomState = (loom: DokumaLoom) =>
  loom.openStopCategory && loom.openStopCategory !== 'PAUSE'
    ? { label: stopLabel[loom.openStopCategory], color: loom.openStopCategory === 'BREAKDOWN' ? colors.danger : colors.warning }
    : loom.beamId !== null ? { label: 'Çalışıyor', color: colors.success }
    : { label: 'Levent yok', color: colors.muted };

const reasonOption = (reason: DokumaStopReason) => ({ ...reason, code: String(reason.id) });

/** Tabletin yerel tarihi: kesim kuyrukta bekleyip sonradan gönderilse de kesildiği günle kaydolur. */
const localDate = () => {
  const now = new Date();
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
};

const decimal = (text: string) => Number(text.replace(',', '.'));

/**
 * Dokuma istasyonu: operatör kartı oturum açar; tezgâhta top kesilir ve duruş bildirilir. Mola, ara ve arıza Konfeksiyon
 * ile aynı işler: ara ve arıza yetkili kartı ister. Tablet tezgâha bağlıysa yalnızca o tezgâh, bağlı değilse (salon
 * tableti) bütün tezgâhlar görünür. Kesim bağlantı yoksa kuyrukta bekler, diğer işlemler sunucu ister.
 */
export function DokumaStationScreen({ api, outbox, reader, tabletCode, onOpenSetup }: Props) {
  const [context, setContext] = useState<DokumaContext | null>(null);
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
      setMessage({ kind: 'success', text: `Bekleyen ${result.sent} kesim gönderildi.` });
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
      // Duruş ve levent ofisten de değişir; ekran kendini günceller.
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

  const looms = context?.looms ?? [];
  const selected = looms.find(loom => loom.loomId === selectedId) ?? (looms.length === 1 ? looms[0] : null);
  const pause = context?.pause ?? null;
  const canPause = Boolean(context?.operator) && !pause;
  const waiting = pause?.kind === 'WAITING_FOR_BEAM';
  // Levent beklerken tezgâh işlemleri açıktır: top kesimi ya da duruş bekleme kaydını kapatır.
  const canWork = Boolean(context?.operator) && (!pause || waiting);
  const dueBreak = context?.dueBreak ?? null;
  const fixedLoom = context?.loomId ? looms[0] : null;
  // Boşta sorusu: tezgâhın leventi yoksa sorulur; salon tabletinde operatör başka tezgâhlara bakar, sorulmaz.
  const idle = useIdlePrompt(canPause && fixedLoom !== null && fixedLoom !== undefined && fixedLoom.beamId === null && !fixedLoom.openStopCategory, context?.idlePromptMinutes ?? 0, now);
  const stopReasons = context?.stopReasons.filter(reason => reason.category !== 'BREAKDOWN') ?? [];
  const breakdownReasons = context?.stopReasons.filter(reason => reason.category === 'BREAKDOWN') ?? [];

  /** Okutulan yetkili kartıyla bekleyen işlemi tamamlar; kart yetkili değilse bekleme sürer ve neden gösterilir. */
  const authorize = (pending_: NonNullable<typeof authorization>, uid: string) => {
    setBusy(true);
    setAuthorizationError(null);
    const action = async () => {
      if (pending_.action === 'pause') {
        // Kuyruktaki kesim araya kalmasın.
        await flush();
        return api.pause('PAUSE', pending_.reasonId, uid);
      }

      if (pending_.action === 'resume') {
        return api.resume(uid);
      }

      if (!selected) {
        throw new Error('Önce tezgâh seçin.');
      }

      return pending_.action === 'breakdown'
        ? api.startStop(selected.loomId, pending_.reasonId!, uid)
        : api.endStop(selected.loomId, uid);
    };
    action()
      .then(next => {
        setContext(next);
        setOnline(true);
        setAuthorization(null);
        if (pending_.action === 'breakdown') {
          setMessage({ kind: 'info', text: 'Arıza bildirildi.' });
        } else if (pending_.action === 'recover') {
          setMessage({ kind: 'success', text: 'Tezgâh yeniden çalışır durumda.' });
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
    // Arızalı tezgâhta okutulan kart arızayı kapatan yetkilinin kartıdır; duraklamada kart okunmaz; aksi hâlde oturum devralınır.
    const action = !context?.operator
      ? api.login(code).then(setContext)
      : selected?.openStopCategory === 'BREAKDOWN'
        ? api.endStop(selected.loomId, code).then(setContext)
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

  const waitForBeam = () => run(async () => {
    setContext(await api.pause('WAITING_FOR_BEAM', null));
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

  const startStop = (loom: DokumaLoom, reasonId: number) => run(async () => {
    setPicker(null);
    setContext(await api.startStop(loom.loomId, reasonId));
  });

  const endStop = (loom: DokumaLoom) => run(async () => {
    setContext(await api.endStop(loom.loomId));
  });

  const cutRoll = (loom: DokumaLoom, meters: number, kilograms: number | undefined, notes: string | undefined, done: () => void) => run(async () => {
    if (loom.beamId === null) {
      return;
    }

    const body = { requestId: newUuid(), cutOn: localDate(), meters, kilograms, notes };
    try {
      const roll = await api.cutRoll(loom.beamId, body);
      setMessage({ kind: 'success', text: `Top ${roll.rollNo} kaydedildi: ${number.format(roll.meters)} m.` });
      setContext(await api.getContext());
    } catch (error) {
      if (!isRetryable(error)) {
        throw error;
      }

      await outbox.add({ beamId: loom.beamId, beamNo: loom.beamNo ?? '', body });
      setPending((await outbox.list()).length);
      setMessage({ kind: 'info', text: 'Bağlantı yok. Kesim kaydedildi, bağlantı gelince gönderilecek.' });
    }
    done();
  });

  const remaining = selected?.expectedFabricMeters !== null && selected?.expectedFabricMeters !== undefined
    ? selected.expectedFabricMeters - (selected.wovenMeters ?? 0)
    : null;
  const fixedState = fixedLoom ? loomState(fixedLoom) : null;
  const statusOfOperator = pause?.kind === 'BREAK' ? operatorStatus.break
    : waiting ? operatorStatus.waiting
    : pause ? operatorStatus.paused
    : fixedLoom?.openStopCategory === 'BREAKDOWN' ? operatorStatus.breakdown
    : operatorStatus.active;
  const pauseTitle = pause?.kind === 'BREAK' ? (pause.breakName ? `Moladasınız · ${pause.breakName}` : 'Moladasınız')
    : waiting ? 'Levent bekleniyor'
    : `Ara verdiniz${pause?.reasonName ? ` · ${pause.reasonName}` : ''}`;
  const authorizationReason = authorization?.action === 'resume'
    ? pause?.reasonName
    : authorization?.action === 'recover'
      ? selected?.openStopReason
      : [...(context?.pauseReasons ?? []), ...(context?.stopReasons ?? [])].find(reason => reason.id === authorization?.reasonId)?.name;

  return (
    <View style={styles.screen} testID="dokuma-station">
      <WedgeInput reader={reader} paused={editing} />

      <StationSidebarFrame
        moduleName="Dokuma"
        title={fixedLoom?.loomCode ?? tabletCode}
        subtitle={context ? fixedLoom?.loomName ?? 'Salon tableti' : undefined}
        meta={tabletCode}
        status={fixedState ? { label: `Tezgâh: ${fixedState.label}`, color: fixedState.color } : undefined}
        statusReason={fixedLoom?.openStopReason ?? null}
        onOpenSetup={onOpenSetup}
        personnelCaption="OPERATÖR"
        footer={
          context?.operator ? (
            <BigButton label="Oturumu kapat" tone="ghost" size="compact" onPress={logout} disabled={busy} testID="dokuma-logout" />
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
                testID="dokuma-recover"
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
                testID="dokuma-breakdown"
              />
            ) : undefined
          }
        />

        {context && selected && (
          <InfoStrip
            cells={[
              { label: 'TEZGÂH', value: selected.loomCode },
              { label: 'LEVENT', value: selected.beamNo ?? 'Yok' },
              { label: 'ARTİKEL', value: selected.articleCode ?? '—' },
              { label: 'KALAN', value: remaining !== null ? `${number.format(remaining)} m` : '—' },
            ]}
          />
        )}

        <View style={styles.content}>
          <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
            {pending > 0 && (
              <View style={styles.pending}>
                <Text style={styles.pendingText} testID="dokuma-pending">
                  {pending} kesim gönderilmeyi bekliyor. Bağlantı gelince kendiliğinden gönderilir.
                </Text>
              </View>
            )}

            {context?.operator && pause && (
              <View style={[styles.bar, styles.barInterrupted]} accessibilityRole="alert">
                <View style={styles.barText}>
                  <Text style={styles.barCaption}>ÇALIŞMA DURAKLATILDI</Text>
                  <Text style={styles.barTitle} testID="dokuma-session-status">{pauseTitle}</Text>
                </View>
                <BigButton label="Devam et" tone="success" size="compact" busy={busy} onPress={resume} testID="dokuma-resume" />
              </View>
            )}

            {canPause && (
              <View style={styles.bar}>
                <View style={styles.barText}>
                  {dueBreak ? (
                    <>
                      <Text style={styles.barCaption}>MOLA ZAMANI</Text>
                      <Text style={styles.barTitle} testID="dokuma-due-break">{dueBreak.name} ({dueBreak.plannedMinutes} dk)</Text>
                    </>
                  ) : (
                    <>
                      <Text style={styles.barCaption}>ÇALIŞMA DURUMU</Text>
                      <Text style={styles.barTitle}>Çalışıyor</Text>
                    </>
                  )}
                </View>
                <View style={styles.barActions}>
                  {dueBreak && <BigButton label="Molaya çık" size="compact" onPress={takeBreak} disabled={busy} testID="dokuma-break" />}
                  {context?.pauseReasons.length ? (
                    <BigButton label="Ara ver" tone="neutral" size="compact" onPress={() => setPicker('pause')} disabled={busy} testID="dokuma-pause" />
                  ) : null}
                  {fixedLoom && fixedLoom.beamId === null && (
                    <BigButton label="Levent bekliyorum" tone="neutral" size="compact" onPress={waitForBeam} disabled={busy} testID="dokuma-waiting" />
                  )}
                </View>
              </View>
            )}

            {!context ? (
              loadError ? (
                <Prompt glyph="alert" title="Tablet bilgileri alınamadı" detail={loadError}>
                  <BigButton label="Tekrar dene" onPress={() => void synchronize()} testID="dokuma-retry" />
                  <BigButton label="Sistem ayarları" tone="neutral" onPress={onOpenSetup} testID="dokuma-open-setup" />
                </Prompt>
              ) : (
                <Prompt
                  glyph={online ? 'loading' : 'alert'}
                  title={online ? 'Sunucuya bağlanılıyor' : 'Sunucuya ulaşılamıyor'}
                  detail={online ? undefined : 'Wi-Fi bağlantısını kontrol edin. Bağlantı gelince kendiliğinden devam edilir.'}>
                  {!online && <BigButton label="Tekrar dene" onPress={() => void synchronize()} testID="dokuma-retry" />}
                </Prompt>
              )
            ) : !context.operator ? (
              <Prompt glyph="card" step="1. ADIM" title="Personel kartınızı okutun" detail="Çalışmaya başlamak için kartınızı okuyucuya gösterin." />
            ) : pause && !waiting ? (
              <Prompt glyph="bundle" title="Çalışma duraklatıldı" detail="Top kesmek ya da duruş bildirmek için önce çalışmaya dönün." />
            ) : !selected ? (
              <LoomList looms={looms} onSelect={setSelectedId} />
            ) : (
              <View style={styles.work}>
                <LoomCard
                  loom={selected}
                  canGoBack={looms.length > 1}
                  busy={busy}
                  onBack={() => setSelectedId(null)}
                  onStop={() => (stopReasons.length > 0
                    ? setPicker('stop')
                    : setMessage({ kind: 'error', text: 'Tanımlı duruş nedeni yok. Web\'de Duruş Nedenleri ekranından ekleyin.' }))}
                  onEndStop={() => endStop(selected)}
                />
                {selected.beamId !== null && (
                  <CutForm key={selected.beamId} busy={busy} onEditing={setEditing} onCut={(meters, kilograms, notes, done) => cutRoll(selected, meters, kilograms, notes, done)} />
                )}
              </View>
            )}
          </ScrollView>

          {message && <MessageToast kind={message.kind} text={message.text} onDismiss={() => setMessage(null)} testID="dokuma-message" />}
          {busy && <BusyOverlay />}
        </View>
      </View>

      {idle.due && !picker && !authorization && !busy && (
        <IdleDialog
          minutes={idle.minutes}
          waitingLabel="Levent bekliyorum"
          description="bu tezgâhta levent görünmüyor."
          help="Levent bekliyorsanız bildirin; bekleme süresi veriminize yansımaz."
          canPause={Boolean(context?.pauseReasons.length)}
          busy={busy}
          onDismiss={idle.acknowledge}
          onPause={() => setPicker('pause')}
          onWait={waitForBeam}
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
          testIDPrefix="dokuma-pause-reason"
        />
      )}

      {picker === 'stop' && canWork && selected && (
        <ReasonDialog
          title="Duruş bildir"
          description={`${selected.loomCode} için duruş kaydı açılır.`}
          reasons={stopReasons.map(reasonOption)}
          confirmLabel="Duruşu başlat"
          busy={busy}
          onConfirm={reasonId => startStop(selected, reasonId)}
          onCancel={() => setPicker(null)}
          testIDPrefix="dokuma-stop-reason"
        />
      )}

      {picker === 'breakdown' && canWork && selected && (
        <ReasonDialog
          title="Arıza bildir"
          description={`${selected.loomCode} için arıza kaydı açılır.`}
          reasons={breakdownReasons.map(reasonOption)}
          confirmLabel="Arızayı bildir"
          tone="danger"
          busy={busy}
          onConfirm={reasonId => requireCard('breakdown', reasonId)}
          onCancel={() => setPicker(null)}
          testIDPrefix="dokuma-breakdown-reason"
        />
      )}

      {authorization && (
        <AuthorizationDialog
          title={authorizations[authorization.action].title}
          roles={authorizations[authorization.action].roles}
          details={[
            ...(selected && authorization.action !== 'pause' && authorization.action !== 'resume' ? [{ label: 'Tezgâh', value: selected.loomCode }] : []),
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

/** Salon tabletinde tezgâh seçimi. */
function LoomList({ looms, onSelect }: { looms: DokumaLoom[]; onSelect: (loomId: number) => void }) {
  if (looms.length === 0) {
    return <Prompt glyph="alert" title="Etkin tezgâh yok" detail="Tezgâh kartları ofisten tanımlanır." />;
  }

  return (
    <View style={styles.list}>
      {looms.map(loom => {
        const state = loomState(loom);
        return (
          <Pressable
            key={loom.loomId}
            style={({ pressed }) => [styles.listRow, pressed && styles.listRowPressed]}
            onPress={() => onSelect(loom.loomId)}
            testID={`dokuma-loom-${loom.loomCode}`}>
            <View style={styles.listMain}>
              <Text style={styles.cellStrong}>{loom.loomCode}</Text>
              <Text style={styles.cellMuted}>{loom.articleCode ? `${loom.articleCode} · ${loom.beamNo}` : loom.loomName}</Text>
            </View>
            {loom.beamId !== null && <Text style={styles.cellMuted}>{number.format(loom.wovenMeters ?? 0)} / {number.format(loom.expectedFabricMeters ?? 0)} m</Text>}
            <Text style={[styles.state, { color: state.color }]}>{state.label}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}

interface LoomCardProps {
  loom: DokumaLoom;
  canGoBack: boolean;
  busy: boolean;
  onBack: () => void;
  onStop: () => void;
  onEndStop: () => void;
}

function LoomCard({ loom, canGoBack, busy, onBack, onStop, onEndStop }: LoomCardProps) {
  const state = loomState(loom);
  return (
    <View style={styles.card}>
      <View style={styles.row}>
        <View style={styles.listMain}>
          <Text style={styles.loomCode}>{loom.loomCode}</Text>
          {loom.beamId !== null && (
            <Text style={styles.meta}>
              {loom.orderNo}{loom.customerName ? ` · ${loom.customerName}` : ''} · {number.format(loom.wovenMeters ?? 0)} / {number.format(loom.expectedFabricMeters ?? 0)} m · {loom.rollCount} top
            </Text>
          )}
        </View>
        <Text style={[styles.state, { color: state.color }]} testID="dokuma-loom-state">{state.label}</Text>
        {canGoBack && <BigButton label="Tezgâhlar" tone="neutral" size="compact" onPress={onBack} testID="dokuma-back" />}
      </View>

      {loom.openStopCategory ? (
        <View style={styles.stopBar}>
          <Text style={styles.stopText} testID="dokuma-stop-reason-text">{loom.openStopReason}</Text>
          {loom.openStopCategory === 'BREAKDOWN' ? (
            <Text style={styles.help}>Arızayı kapatmak için vardiya amiri ya da bakım personeli kartını okutsun.</Text>
          ) : (
            <BigButton label="Duruşu bitir" tone="success" size="compact" onPress={onEndStop} busy={busy} testID="dokuma-end-stop" />
          )}
        </View>
      ) : (
        <View style={styles.actions}>
          <BigButton label="Duruş bildir" tone="neutral" size="compact" onPress={onStop} disabled={busy} testID="dokuma-stop" />
        </View>
      )}
    </View>
  );
}

interface CutFormProps {
  busy: boolean;
  onEditing: (editing: boolean) => void;
  onCut: (meters: number, kilograms: number | undefined, notes: string | undefined, done: () => void) => void;
}

function CutForm({ busy, onEditing, onCut }: CutFormProps) {
  const [meters, setMeters] = useState('');
  const [kilograms, setKilograms] = useState('');
  const [notes, setNotes] = useState('');
  const metersValue = decimal(meters);
  const kilogramsValue = kilograms.trim() === '' ? undefined : decimal(kilograms);
  const valid = metersValue > 0 && (kilogramsValue === undefined || kilogramsValue > 0);

  const clear = () => {
    setMeters('');
    setKilograms('');
    setNotes('');
  };

  return (
    <View style={styles.card}>
      <Text style={styles.section}>Top kes</Text>
      <View style={styles.formRow}>
        <View style={styles.field}>
          <Text style={styles.label}>Metre</Text>
          <TextInput
            style={styles.input}
            keyboardType="decimal-pad"
            value={meters}
            onFocus={() => onEditing(true)}
            onBlur={() => onEditing(false)}
            onChangeText={setMeters}
            testID="dokuma-cut-meters"
          />
        </View>
        <View style={styles.field}>
          <Text style={styles.label}>Kilo (tartıldıysa)</Text>
          <TextInput
            style={styles.input}
            keyboardType="decimal-pad"
            value={kilograms}
            onFocus={() => onEditing(true)}
            onBlur={() => onEditing(false)}
            onChangeText={setKilograms}
            testID="dokuma-cut-kilograms"
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
            testID="dokuma-cut-notes"
          />
        </View>
      </View>
      <BigButton
        label="Topu kaydet"
        onPress={() => onCut(metersValue, kilogramsValue, notes.trim() || undefined, clear)}
        disabled={!valid}
        busy={busy}
        testID="dokuma-cut"
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
  loomCode: { color: colors.text, fontSize: 20, fontWeight: '700' },
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
