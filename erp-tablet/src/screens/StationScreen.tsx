import { useEffect, useRef, useState } from 'react';
import type { ReactNode } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { ComponentRef } from 'react';
import type { BundleStep, PerformancePeriod, StaffRole, StaffRolePermissions, TabletPerformance } from '../api/types';
import type { KeyboardWedgeReader } from '../rfid/KeyboardWedgeReader';
import { WedgeInput } from '../rfid/WedgeInput';
import { phaseOf } from '../station/StationController';
import type { AuthorizedAction, StationController, StationPhase, StationState } from '../station/StationController';
import { useStation } from '../station/useStation';
import { Banner, BigButton, QuantityStepper } from '../ui/controls';
import { DialogFrame } from '../ui/Dialog';
import { AuthorizationPanel, ReasonPanel } from '../ui/InlinePanels';
import { OperationPanel } from '../ui/OperationPanel';
import type { OperationNotice, OperationRow, OperationTone } from '../ui/OperationPanel';
import { BusyOverlay, MessageToast, Prompt, StationTopBar } from '../ui/station';
import { colors, radius } from '../ui/theme';
import { formatTimer } from '../util/time';
import { useNow } from '../util/useNow';
import { PerformancePage } from './PerformancePage';
import { SessionBar } from './SessionBar';
import { staffRoleLabels, StationSidebar } from './StationSidebar';

interface StationScreenProps {
  controller: StationController;
  reader: KeyboardWedgeReader;
  tabletCode: string;
  /** Markaya uzun basılınca sistem ayarları açılır (yönetici işlemi). */
  onOpenSetup: () => void;
}

/**
 * Üst çubuğun solu: operatörün çalışma durumu (adım göstergesi yerine). Planlı molanın zamanı geldiyse mola adı,
 * henüz başlatılamıyorsa nedeni de yazar.
 */
function WorkStatus({ session, phase, needsEnrollment }: { session: StationState['session']; phase: StationPhase; needsEnrollment: boolean }) {
  if (needsEnrollment || phase === 'loading') {
    return null;
  }

  const dueBreak = session?.status === 'ACTIVE' ? session.dueBreak : null;
  const caption = !session ? 'PERSONEL' : dueBreak ? 'MOLA ZAMANI' : 'ÇALIŞMA DURUMU';
  const title = !session
    ? 'Personel kartı bekleniyor'
    : dueBreak
      ? `${dueBreak.name} (${dueBreak.plannedMinutes} dk)`
      : session.status === 'ACTIVE'
        ? phase === 'working' ? 'Üretimde' : 'Çalışıyor'
        : 'Duraklatıldı';
  const hint = dueBreak && !dueBreak.canStartNow ? dueBreak.hint : null;

  return (
    <View style={styles.workStatus} testID="station-work-status">
      <Text style={styles.workStatusCaption}>{caption}</Text>
      <Text style={styles.workStatusTitle} numberOfLines={1}>{title}</Text>
      {hint && <Text style={styles.workStatusHint} numberOfLines={1}>{hint}</Text>}
    </View>
  );
}

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

const stepTones: Record<BundleStep['status'], OperationTone> = {
  WAITING: 'idle',
  READY: 'ready',
  IN_PROGRESS: 'active',
  PAUSED: 'active',
  COMPLETED: 'done',
  QUALITY_HOLD: 'warning',
  CANCELLED: 'idle',
};

/** Demetin adımları, operasyon listesinin satırları olarak. Neyin başlatılabileceğini sunucu söyler. */
function operationRows(steps: BundleStep[]): OperationRow[] {
  return steps.map(step => {
    const done = step.status === 'COMPLETED';
    const elsewhere = !done && step.machineCanDo === false;
    return {
      id: step.bundleOperationId,
      sequenceNo: step.sequenceNo,
      name: step.operationName,
      status: elsewhere ? 'Başka makinede' : stepStatusLabels[step.status],
      tone: elsewhere ? 'idle' : stepTones[step.status],
      quantity: `${step.producedQuantity} / ${step.plannedQuantity}`,
      duration: step.sam ? `${Math.round(step.sam * step.plannedQuantity)} dk` : '—',
      done,
      startable: step.canStart === true,
    };
  });
}

/**
 * Demetin kendisi üretime kapalıysa (sunucu hiçbir adımı başlatmaz): operatöre gösterilecek neden.
 * Durum adları sunucunun demet durumudur (BundleStatus).
 */
const closedBundleNotices: Record<string, { title: string; detail: string }> = {
  QUALITY_HOLD: {
    title: 'Demet kalite bekletmesinde',
    detail: 'Kalite serbest bırakana kadar hiçbir operasyonu başlatılamaz. Demeti kalite sorumlusuna iletin.',
  },
  CANCELLED: { title: 'Demet iptal edilmiş', detail: 'Bu demetle üretim yapılamaz. Demeti üretim planlamaya iletin.' },
  CLOSED: { title: 'Demet kapatılmış', detail: 'Bu demetle üretim yapılamaz.' },
  CREATED: { title: 'Demet henüz üretime verilmemiş', detail: 'Demet sahaya verilince operasyonları başlatılabilir.' },
};

/** Bu makinede başlatılabilecek adım yoksa nedenini ve sıradaki adımı söyler; varsa boştur. */
function operationNotice(steps: BundleStep[], bundleStatus: string): OperationNotice | null {
  if (steps.some(step => step.canStart)) {
    return null;
  }

  const next = steps.find(step => step.status !== 'COMPLETED' && step.status !== 'CANCELLED');
  // Neden demetin kendisiyse (ör. kalite bekletmesi) adımın durumu yanıltıcı olur: demetin durumu söylenir.
  const closed = closedBundleNotices[bundleStatus];
  if (closed) {
    return { ...closed, nextId: next?.bundleOperationId ?? null };
  }

  if (!next) {
    return { title: 'Demetin tüm operasyonları tamamlandı', detail: 'Bu demette yapılacak operasyon kalmadı.', nextId: null };
  }

  return {
    title: `Sıradaki operasyon: ${next.sequenceNo} · ${next.operationName}`,
    detail:
      next.machineCanDo === false
        ? 'Bu operasyon bu makinede yapılamıyor. Demeti o operasyonun makinesine iletin.'
        : `Bu operasyon şu an başlatılamıyor (${stepStatusLabels[next.status].toLocaleLowerCase('tr-TR')}).`,
    nextId: next.bundleOperationId,
  };
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
  const [dialog, setDialogState] = useState<'pause' | 'breakdown' | null>(null);

  // İş alanı oturum açıkken yatayda iki sayfadır: Üretim | Performansım (kaydırarak ya da sekmeye dokunarak).
  const pager = useRef<ComponentRef<typeof ScrollView>>(null);
  const [pageWidth, setPageWidth] = useState(0);
  const [pageHeight, setPageHeight] = useState(0);
  const pageWidthRef = useRef(0);
  pageWidthRef.current = pageWidth;
  const [page, setPage] = useState(0);
  const pageRef = useRef(0);
  const dragStartX = useRef(0);
  // Kaydırmanın anlık konumu: bırakıldıktan sonra sayfa tam yerine oturmadıysa yeniden oturtmak için.
  const scrollX = useRef(0);
  const settleTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  /** Sayfayı yerine oturtur; atalet komutu ezdiyse kısa süre sonra yeniden dener (sayfa yarıda kalmaz). */
  const settleOn = (target: number) => {
    requestAnimationFrame(() => goToPageRef.current(target));
    if (settleTimer.current) {
      clearTimeout(settleTimer.current);
    }
    settleTimer.current = setTimeout(() => {
      settleTimer.current = null;
      if (Math.abs(scrollX.current - target * pageWidthRef.current) > 1) {
        goToPageRef.current(target);
      }
    }, SETTLE_CHECK_MS);
  };
  const goToPage = (index: number, animated = true) => {
    pageRef.current = index;
    setPage(index);
    pager.current?.scrollTo({ x: index * pageWidth, animated });
  };
  const goToPageRef = useRef(goToPage);
  goToPageRef.current = goToPage;
  const setDialog = (next: 'pause' | 'breakdown' | null) => {
    if (next !== null && pageRef.current !== 0) {
      goToPage(0);
    }
    setDialogState(next);
  };
  // Yetkili kartı beklenirken de üretim sayfası gösterilir.
  useEffect(() => {
    if (state.authorization && pageRef.current !== 0) {
      goToPageRef.current(0);
    }
  }, [state.authorization]);

  const sessionId = state.session?.sessionId ?? null;
  // Sol panel hep bugünü gösterir; hafta verisi yalnızca o dönem seçilince istenir.
  const [performancePeriod, setPerformancePeriod] = useState<PerformancePeriod>('TODAY');
  const performance = usePerformance(controller, sessionId, page === 1, state.job, 'TODAY');
  // Hafta verisi bir kez alınınca saklanır: dönemler arası geçiş anında olur, veri arka planda yenilenir.
  const weekPerformance = usePerformance(controller, sessionId, page === 1, state.job, 'WEEK', performancePeriod === 'WEEK');
  // Yeni dönemin verisi gelene kadar eldeki görünüm kalır (sayfa boşalıp yeniden dolmaz); soluk gösterilir.
  const shownPerformance = performancePeriod === 'WEEK'
    ? {
        data: weekPerformance.data ?? performance.data,
        loading: weekPerformance.loading || weekPerformance.data === null,
        failed: weekPerformance.failed,
      }
    : performance;

  useEffect(() => {
    if (sessionId === null && pageRef.current !== 0) {
      pageRef.current = 0;
      setPage(0);
    }
  }, [sessionId]);

  // Performans sayfasında unutulmasın: 30 sn sonra üretime döner.
  useEffect(() => {
    if (page !== 1) {
      return;
    }
    const timer = setTimeout(() => goToPageRef.current(0), PERFORMANCE_PAGE_MS);
    return () => clearTimeout(timer);
  }, [page]);

  useEffect(() => {
    reader.start(uid => {
      // Kart okutulunca üretim sayfasına dönülür; okutmanın sonucu orada görünür.
      if (pageRef.current !== 0) {
        goToPageRef.current(0);
      }
      controller.handleCard(uid);
    });
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
  // Operasyon listesi açıkken iş alanı kaymaz; liste kendi içinde kayar.
  const choosingOperation =
    phase === 'needs-bundle' && !state.needsEnrollment && !!machine && machine.status !== 'BREAKDOWN' && !!state.operationChoice;
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

  // Performans sayfası yalnızca oturum açıkken vardır.
  const pagerEnabled = state.session !== null && !state.needsEnrollment && phase !== 'loading';
  // Ara ve arıza nedeni seçimi ile yetkili onayı iş alanında gösterilir (pencere açılmaz).
  const inlinePanel = authorization ? (
    <AuthorizationPanel
      title={authorizations[authorization.action].title}
      roles={authorizerRoles(authorization.action, context?.staffRoles)}
      details={authorizationDetails}
      error={state.message?.kind === 'error' ? state.message.text : null}
      busy={state.busy}
      onCancel={() => controller.cancelAuthorization()}
    />
  ) : dialog === 'pause' && state.session?.status === 'ACTIVE' ? (
    <ReasonPanel
      caption="ARA VER"
      title="Ara nedenini seçin"
      description="Döndüğünüzde “Devam et” ile çalışmaya devam edersiniz."
      reasons={pauseReasons}
      busy={state.busy}
      onSelect={reasonId => {
        setDialog(null);
        controller.pause('PAUSE', reasonId);
      }}
      onCancel={() => setDialog(null)}
      testIDPrefix="session-reason"
    />
  ) : dialog === 'breakdown' && machine && machine.status !== 'BREAKDOWN' ? (
    <ReasonPanel
      caption="ARIZA BİLDİR"
      title="Arıza nedenini seçin"
      description={`${machine.machineCode} için arıza kaydı açılır ve bakım ekibi bilgilendirilir.`}
      reasons={breakdownReasons}
      tone="danger"
      busy={state.busy}
      onSelect={reasonId => {
        setDialog(null);
        controller.reportBreakdown(reasonId);
      }}
      onCancel={() => setDialog(null)}
      testIDPrefix="breakdown-reason"
    />
  ) : null;

  const productionBody = inlinePanel ? (
    <ContentBody fixed>{inlinePanel}</ContentBody>
  ) : (
    <ContentBody fixed={choosingOperation}>
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
          busy={state.busy}
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
          title="Personel kartınızı okutun"
          detail="Çalışmaya başlamak için kartınızı okuyucuya gösterin."
        />
      ) : machine.status === 'BREAKDOWN' && !state.job ? (
        <Prompt glyph="alert" title="Makine arızalı" detail={machine.statusReason ?? undefined} />
      ) : phase === 'needs-bundle' && state.operationChoice ? (
        <OperationPanel
          // Yeni okutmada ya da liste yenilenip başlatılabilir adımlar değişince seçim baştan kurulur.
          key={`${state.operationChoice.bundle.bundleId}:${state.operationChoice.options.map(step => step.bundleOperationId).join(',')}`}
          title={`Demet ${state.operationChoice.bundle.bundleNo}`}
          summary={[
            { label: 'İŞ EMRİ', value: state.operationChoice.bundle.orderNo },
            { label: 'MODEL', value: state.operationChoice.bundle.modelCode },
            {
              label: 'RENK / BEDEN',
              value: `${state.operationChoice.bundle.colorName} / ${state.operationChoice.bundle.sizeName}`,
            },
            { label: 'ADET', value: String(state.operationChoice.bundle.quantity) },
          ]}
          rows={operationRows(state.operationChoice.bundle.operations ?? [])}
          notice={operationNotice(state.operationChoice.bundle.operations ?? [], state.operationChoice.bundle.status)}
          busy={state.busy}
          onStart={bundleOperationId => controller.chooseOperation(bundleOperationId)}
          onCancel={() => controller.cancelOperationChoice()}
        />
      ) : phase === 'needs-bundle' ? (
        <Prompt
          glyph="bundle"
          title="Demet kartını okutun"
          detail="Demet kartı okutulunca operasyonları burada listelenir; sıradaki operasyon seçili gelir."
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
    </ContentBody>
  );

  return (
    <View style={styles.screen}>
      <WedgeInput reader={reader} paused={state.needsEnrollment} />

      <StationSidebar
        state={state}
        tabletCode={tabletCode}
        now={now}
        onOpenSetup={onOpenSetup}
        onSignOut={() => controller.signOut()}
        todayPerformance={performance.data}
      />

      <View style={styles.main}>
        <StationTopBar
          leading={<WorkStatus session={state.session} phase={phase} needsEnrollment={state.needsEnrollment} />}
          online={state.online}
          now={now}
          actions={
            machine &&
            !state.needsEnrollment && (
            <>
              {/* Çalışırken: planlı mola (zamanı geldiyse), ara ve demet bekleme. Uygunluğu sunucu denetler. */}
              {state.session?.status === 'ACTIVE' && state.session.dueBreak && (
                <BigButton
                  label="Molaya çık"
                  size="compact"
                  style={styles.topAction}
                  disabled={state.busy || !state.session.dueBreak.canStartNow}
                  onPress={() => controller.pause('BREAK', null)}
                  testID="session-break"
                />
              )}
              {state.session?.status === 'ACTIVE' && pauseReasons.length > 0 && (
                <BigButton
                  label="Ara ver"
                  tone="primaryOutline"
                  size="compact"
                  style={styles.topAction}
                  disabled={state.busy}
                  onPress={() => setDialog('pause')}
                  testID="session-pause"
                />
              )}
              {state.session?.status === 'ACTIVE' && state.job === null && (
                <BigButton
                  label="Demet bekliyorum"
                  tone="primaryOutline"
                  size="compact"
                  style={styles.topAction}
                  disabled={state.busy}
                  onPress={() => controller.pause('WAITING_FOR_BUNDLE', null)}
                  testID="session-waiting"
                />
              )}
            {state.session?.status === 'ACTIVE' && <View style={styles.topDivider} />}
            {machine.status === 'BREAKDOWN' ? (
              <BigButton
                label="Arızayı kapat"
                tone="success"
                size="compact"
                style={styles.topAction}
                disabled={state.busy}
                onPress={() => controller.reportRecovery()}
                testID="station-recover"
              />
            ) : (
              <BigButton
                label="Arıza bildir"
                tone="dangerOutline"
                size="compact"
                style={styles.topAction}
                disabled={state.busy}
                // Tanımlı arıza nedeni varsa önce neden seçilir; yoksa arıza doğrudan bildirilir.
                onPress={() => (breakdownReasons.length > 0 ? setDialog('breakdown') : controller.reportBreakdown(null))}
                testID="station-breakdown"
              />
            )}
            </>
            )
          }
        />

        {pagerEnabled && (
          <View style={styles.pageTabs} accessibilityRole="tablist">
            {['Üretim', 'Performansım'].map((label, index) => (
              <Pressable
                key={label}
                accessibilityRole="tab"
                accessibilityState={{ selected: page === index }}
                onPress={() => goToPage(index)}
                style={[styles.pageTab, page === index && styles.pageTabActive]}
                testID={index === 0 ? 'station-page-production' : 'station-page-performance'}>
                <Text style={[styles.pageTabText, page === index && styles.pageTabTextActive]}>{label}</Text>
              </Pressable>
            ))}
            <View style={styles.pageDots}>
              {[0, 1].map(index => (
                <View key={index} style={[styles.pageDot, page === index && styles.pageDotActive]} />
              ))}
            </View>
          </View>
        )}

        <View
          style={styles.content}
          onLayout={event => {
            const { width, height } = event.nativeEvent.layout;
            setPageHeight(height);
            if (width !== pageWidth) {
              setPageWidth(width);
              // Ekran boyu değişince açık sayfa yerinde kalır.
              requestAnimationFrame(() => pager.current?.scrollTo({ x: pageRef.current * width, animated: false }));
            }
          }}>
          {pagerEnabled && pageWidth > 0 ? (
            <ScrollView
              ref={pager}
              horizontal
              // Sayfa geçişi burada karar verilir: kısa bir sürükleme ya da hızlı bir fırlatma yeter (Android'in
              // sayfalı kaydırması yarıdan fazla sürükleme ister ve kısa hareketi geri esnetir).
              decelerationRate="fast"
              showsHorizontalScrollIndicator={false}
              keyboardShouldPersistTaps="handled"
              overScrollMode="never"
              scrollEventThrottle={16}
              onScroll={event => {
                scrollX.current = event.nativeEvent.contentOffset.x;
              }}
              onScrollBeginDrag={event => {
                dragStartX.current = event.nativeEvent.contentOffset.x;
              }}
              onScrollEndDrag={event => {
                const moved = event.nativeEvent.contentOffset.x - dragStartX.current;
                const speed = Math.abs(event.nativeEvent.velocity?.x ?? 0);
                const from = pageRef.current;
                const passed = Math.abs(moved) > pageWidth * SWIPE_DISTANCE || (speed > SWIPE_SPEED && Math.abs(moved) > SWIPE_MIN_PX);
                const target = passed ? Math.max(0, Math.min(1, from + Math.sign(moved))) : from;
                // Android sürükleme bitmeden gelen kaydırma komutunu yok sayar: sonraki karede verilir.
                settleOn(target);
              }}
              onMomentumScrollEnd={event => {
                // Sayfa sınırında durmadıysa en yakın sayfaya oturtulur.
                const index = Math.round(event.nativeEvent.contentOffset.x / pageWidth);
                if (Math.abs(event.nativeEvent.contentOffset.x - index * pageWidth) > 1) {
                  settleOn(index);
                } else if (index !== pageRef.current) {
                  pageRef.current = index;
                  setPage(index);
                }
              }}
              testID="station-pager">
              <View style={{ width: pageWidth, height: pageHeight }}>{productionBody}</View>
              <View style={{ width: pageWidth, height: pageHeight }}>
                <PerformancePage
                  performance={shownPerformance.data}
                  loading={shownPerformance.loading}
                  failed={shownPerformance.failed}
                  period={performancePeriod}
                  onPeriodChange={setPerformancePeriod}
                />
              </View>
            </ScrollView>
          ) : (
            productionBody
          )}

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

    </View>
  );
}


/** Sayfa değiştirmek için sürükleme: sayfa genişliğinin oranı ya da kısa ama hızlı fırlatma. */
const SWIPE_DISTANCE = 0.12;
const SWIPE_SPEED = 0.3;
const SWIPE_MIN_PX = 12;
/** Bırakıldıktan sonra sayfanın yerine oturduğunun denetlendiği an. */
const SETTLE_CHECK_MS = 300;

/** Performans sayfasında kalma süresi; sonra üretim sayfasına dönülür. */
const PERFORMANCE_PAGE_MS = 30_000;
/** Performans sayfası açıkken yenileme aralığı. */
const PERFORMANCE_REFRESH_MS = 60_000;

/**
 * Oturumu açık operatörün bugünkü performansı. Oturum açılınca, performans sayfasına geçilince, iş bitince /
 * adet bildirilince ve sayfa açıkken dakikada bir yenilenir. Bağlantı yoksa son değer korunur.
 */
function usePerformance(
  controller: StationController,
  sessionId: number | null,
  visible: boolean,
  job: StationState['job'],
  period: PerformancePeriod,
  /** Kapalıyken istek yapılmaz ama eldeki veri korunur (dönem değişince silinmez). */
  enabled = true,
): { data: TabletPerformance | null; loading: boolean; failed: boolean } {
  const [data, setData] = useState<TabletPerformance | null>(null);
  const [loading, setLoading] = useState(false);
  const [failed, setFailed] = useState(false);
  const jobKey = job ? `${job.bundleOperationId}:${job.remainingQuantity}` : 'none';

  useEffect(() => {
    if (sessionId === null) {
      setData(null);
      return;
    }
    if (!enabled) {
      return;
    }

    let cancelled = false;
    const load = () => {
      setLoading(true);
      controller
        .fetchPerformance(period)
        .then(result => {
          if (!cancelled) {
            setData(result);
            setFailed(false);
          }
        })
        .catch(() => !cancelled && setFailed(true))
        .finally(() => !cancelled && setLoading(false));
    };

    load();
    const timer = visible ? setInterval(load, PERFORMANCE_REFRESH_MS) : null;
    return () => {
      cancelled = true;
      if (timer !== null) {
        clearInterval(timer);
      }
    };
  }, [controller, sessionId, visible, jobKey, period, enabled]);

  return { data, loading, failed };
}

/** İş alanı: içerik sayfa olarak kayar; `fixed` ise alan sabittir, kaydırmayı içerik kendisi yapar. */
function ContentBody({ fixed, children }: { fixed: boolean; children: ReactNode }) {
  return fixed ? (
    <View style={styles.bodyFixed}>{children}</View>
  ) : (
    <ScrollView contentContainerStyle={styles.body} keyboardShouldPersistTaps="handled">
      {children}
    </ScrollView>
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
  const total = job.remainingQuantity;
  const left = total - produced - rejected;
  // Sağlam ve hatalı adetin toplamı kalan adedi geçemez: biri artınca taşan kısım diğerinden düşülür. Biri
  // azalınca diğeri değişmez; aradaki fark "kalır" olarak bildirilir.
  const changeProduced = (next: number) => {
    setProduced(next);
    setRejected(current => Math.min(current, total - next));
  };
  const changeRejected = (next: number) => {
    setRejected(next);
    setProduced(current => Math.min(current, total - next));
  };

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
                {state.bundle.modelCode} · {state.bundle.colorName} · {state.bundle.sizeName} · İş emri {state.bundle.orderNo}
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
          <RouteProgress steps={state.bundle.operations} currentId={job.bundleOperationId} />
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
          max={total}
          bigStep={total}
          onChange={changeProduced}
          testID="job-produced"
        />
        <QuantityStepper
          label="HATALI ADET"
          value={rejected}
          max={total}
          bigStep={total}
          onChange={changeRejected}
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

/**
 * Süren işin rotadaki yeri: yalnızca önceki, şu anki ve sıradaki adım ile ilerleme. Rotanın tamamı demet
 * okutulunca operasyon listesinde görülür.
 */
function RouteProgress({ steps, currentId }: { steps: BundleStep[]; currentId: number }) {
  const index = steps.findIndex(step => step.bundleOperationId === currentId);
  if (index < 0) {
    return null;
  }

  const cells: { label: string; step: BundleStep | undefined; current?: boolean }[] = [
    { label: 'ÖNCEKİ ADIM', step: steps[index - 1] },
    { label: 'ŞU ANKİ ADIM', step: steps[index], current: true },
    { label: 'SIRADAKİ ADIM', step: steps[index + 1] },
  ];

  return (
    <View style={styles.route} testID="job-route">
      <View style={styles.routeProgress}>
        <Text style={styles.routeProgressText}>
          Adım {index + 1} / {steps.length}
        </Text>
        <View style={styles.routeTrack}>
          <View style={[styles.routeFill, { width: `${((index + 1) / steps.length) * 100}%` }]} />
        </View>
      </View>
      <View style={styles.routeCells}>
        {cells.map(cell => (
          <View key={cell.label} style={[styles.routeCell, cell.current && styles.routeCellCurrent]}>
            <Text style={[styles.caption, cell.current && styles.routeCaptionCurrent]}>{cell.label}</Text>
            <Text
              style={[styles.routeCellText, cell.current && styles.routeCellTextCurrent, !cell.step && styles.routeCellEmpty]}
              numberOfLines={1}>
              {cell.step ? `${cell.step.sequenceNo} · ${cell.step.operationName}` : cell.current ? '—' : 'Yok'}
            </Text>
          </View>
        ))}
      </View>
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
  // Üst çubuk düğmeleri: eldivenle de rahat dokunulsun.
  topAction: { minHeight: 46 },
  // Duraklatma düğmeleriyle "Arıza bildir" arasında ayraç.
  topDivider: { width: 1, height: 30, marginHorizontal: 6, backgroundColor: colors.borderStrong },
  workStatus: { gap: 1 },
  workStatusCaption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.2 },
  workStatusTitle: { color: colors.text, fontSize: 16, fontWeight: '700' },
  workStatusHint: { color: colors.muted, fontSize: 12 },
  pageTabs: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 4,
    paddingHorizontal: 12,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  pageTab: { paddingVertical: 9, paddingHorizontal: 14, borderBottomWidth: 3, borderBottomColor: 'transparent' },
  pageTabActive: { borderBottomColor: colors.primary },
  pageTabText: { color: colors.muted, fontSize: 15, fontWeight: '600' },
  pageTabTextActive: { color: colors.primary, fontWeight: '700' },
  pageDots: { flexDirection: 'row', gap: 6, marginLeft: 'auto', paddingRight: 8 },
  pageDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.borderStrong },
  pageDotActive: { backgroundColor: colors.primary, width: 20 },
  // İş alanı kenar boşluğu olmadan ekranı doldurur; bölümler ince çizgilerle ayrılır.
  body: { flexGrow: 1, backgroundColor: colors.surface },
  bodyFixed: { flex: 1, backgroundColor: colors.surface },
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
  route: { gap: 8 },
  routeProgress: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  routeProgressText: { color: colors.textSecondary, fontSize: 13, fontWeight: '700', fontVariant: ['tabular-nums'] },
  routeTrack: { flex: 1, height: 4, borderRadius: 2, backgroundColor: colors.neutralSoft, overflow: 'hidden' },
  routeFill: { height: 4, borderRadius: 2, backgroundColor: colors.primary },
  routeCells: {
    flexDirection: 'row',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surfaceMuted,
    overflow: 'hidden',
  },
  routeCell: { flex: 1, gap: 2, paddingVertical: 8, paddingHorizontal: 12 },
  routeCellCurrent: {
    backgroundColor: colors.primarySoft,
    borderLeftWidth: 1,
    borderRightWidth: 1,
    borderColor: colors.primary,
  },
  routeCaptionCurrent: { color: colors.primary },
  routeCellText: { color: colors.textSecondary, fontSize: 14, fontWeight: '600' },
  routeCellTextCurrent: { color: colors.primary, fontWeight: '700' },
  routeCellEmpty: { color: colors.muted, fontWeight: '500' },
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
