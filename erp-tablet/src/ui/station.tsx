import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import { ActivityIndicator, StyleSheet, Text, View } from 'react-native';
import { Banner } from './controls';
import { colors, overlayShadow, radius } from './theme';
import { formatClock, formatDate } from '../util/time';

const TOAST_MS = 4000;
const ERROR_TOAST_MS = 8000;

/** İstasyon ekranlarının ortak parçaları: üst şerit, bilgi şeridi, yönlendirme ekranı, ileti ve meşgul göstergesi. */

interface StationTopBarProps {
  /** Akışın adımları (ör. Personel, Demet, Üretim); verilmezse adım göstergesi çizilmez. */
  steps?: string[];
  /** Bulunulan adımın sırası; henüz akışa girilmediyse -1. */
  current?: number;
  /** Solda adım göstergesi yerine gösterilecek içerik (ör. çalışma durumu). */
  leading?: ReactNode;
  online: boolean;
  now: Date;
  /** Bağlantı durumunun solundaki düğmeler. */
  actions?: ReactNode;
}

export function StationTopBar({ steps, current = -1, leading, online, now, actions }: StationTopBarProps) {
  return (
    <View style={styles.topBar}>
      <View style={styles.topBarLeading}>{leading ?? (steps && <StepIndicator steps={steps} current={current} />)}</View>

      <View style={styles.topBarRight}>
        {actions}

        <View style={styles.status}>
          <View style={styles.connection}>
            <View style={[styles.connectionDot, { backgroundColor: online ? colors.success : colors.danger }]} />
            <Text style={[styles.connectionText, !online && styles.connectionLost]} testID="station-connection">
              {online ? 'Bağlı' : 'Bağlantı yok'}
            </Text>
          </View>
          <Text style={styles.clock} testID="station-clock">
            {formatDate(now)} · {formatClock(now)}
          </Text>
        </View>
      </View>
    </View>
  );
}

/** Akışın adımları; bulunulan adım vurgulanır, geçilenler işaretlenir. */
function StepIndicator({ steps, current }: { steps: string[]; current: number }) {
  return (
    <View style={styles.steps} accessibilityRole="progressbar">
      {steps.map((label, index) => {
        const active = index === current;
        const done = current > index;
        return (
          <View key={label} style={styles.step}>
            {index > 0 && <View style={[styles.stepLine, (active || done) && styles.stepLineDone]} />}
            <View style={[styles.stepBadge, active && styles.stepBadgeActive, done && styles.stepBadgeDone]}>
              <Text style={[styles.stepNumber, (active || done) && styles.stepNumberActive]}>{done ? '✓' : index + 1}</Text>
            </View>
            <Text style={[styles.stepLabel, active && styles.stepLabelActive]}>{label}</Text>
          </View>
        );
      })}
    </View>
  );
}

/** İstasyon bilgi şeridi: küçük başlık ve değerden oluşan alanlar. */
export function InfoStrip({ cells }: { cells: { label: string; value: string }[] }) {
  return (
    <View style={styles.info}>
      {cells.map((cell, index) => (
        <View key={cell.label} style={[styles.infoCell, index < cells.length - 1 && styles.infoCellDivider]}>
          <Text style={styles.caption}>{cell.label}</Text>
          <Text style={styles.infoValue} numberOfLines={1}>
            {cell.value}
          </Text>
        </View>
      ))}
    </View>
  );
}

export type GlyphKind = 'card' | 'bundle' | 'alert' | 'loading';

/** Yönlendirme ekranlarının görseli; yazı tipine ya da emojiye bağlı kalmadan çizilir. */
function Glyph({ kind }: { kind: GlyphKind }) {
  if (kind === 'loading') {
    return (
      <View style={styles.glyph}>
        <ActivityIndicator size="large" color={colors.primary} />
      </View>
    );
  }

  if (kind === 'alert') {
    return (
      <View style={[styles.glyph, styles.glyphAlert]}>
        <Text style={styles.glyphAlertText}>!</Text>
      </View>
    );
  }

  if (kind === 'bundle') {
    return (
      <View style={styles.glyph}>
        <View style={[styles.bundleLayer, styles.bundleLayerBack]} />
        <View style={[styles.bundleLayer, styles.bundleLayerMiddle]} />
        <View style={[styles.bundleLayer, styles.bundleLayerFront]}>
          <View style={styles.bundleTag} />
        </View>
      </View>
    );
  }

  return (
    <View style={styles.glyph}>
      <View style={styles.card}>
        <View style={styles.cardPhoto} />
        <View style={styles.cardLines}>
          <View style={[styles.cardLine, styles.cardLineLong]} />
          <View style={styles.cardLine} />
        </View>
      </View>
    </View>
  );
}

interface PromptProps {
  glyph: GlyphKind;
  /** Akıştaki adım etiketi (ör. "1. ADIM"). */
  step?: string;
  title: string;
  detail?: string;
  children?: ReactNode;
}

export function Prompt({ glyph, step, title, detail, children }: PromptProps) {
  return (
    <View style={styles.prompt}>
      <Glyph kind={glyph} />
      {step && <Text style={styles.promptStep}>{step}</Text>}
      <Text style={styles.promptTitle} testID="station-prompt" accessibilityRole="header">
        {title}
      </Text>
      {detail && <Text style={styles.promptDetail}>{detail}</Text>}
      {children}
    </View>
  );
}

interface MessageToastProps {
  kind: 'error' | 'success' | 'info';
  text: string;
  onDismiss: () => void;
  testID: string;
}

/**
 * İletiler ekranın altında, içeriğin üzerinde açılır: ekranı itmez, üstteki durum şeritlerini kapatmaz.
 * Kendiliğinden kapanır; hata iletisi okunabilsin diye daha uzun kalır.
 */
export function MessageToast({ kind, text, onDismiss, testID }: MessageToastProps) {
  const dismiss = useRef(onDismiss);
  dismiss.current = onDismiss;

  useEffect(() => {
    const timer = setTimeout(() => dismiss.current(), kind === 'error' ? ERROR_TOAST_MS : TOAST_MS);
    return () => clearTimeout(timer);
  }, [kind, text]);

  return (
    <View style={styles.toast}>
      <Banner kind={kind} text={text} onDismiss={onDismiss} testID={testID} />
    </View>
  );
}

export function BusyOverlay() {
  return (
    <View style={styles.busy} pointerEvents="none">
      <View style={styles.busyBox}>
        <ActivityIndicator size="large" color={colors.primary} testID="station-busy" />
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  topBarLeading: { flex: 1, minWidth: 0 },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingHorizontal: 20,
    paddingVertical: 10,
    gap: 16,
  },
  topBarRight: { flexDirection: 'row', alignItems: 'center', gap: 16 },
  status: { alignItems: 'flex-end', gap: 2 },
  connection: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  connectionDot: { width: 8, height: 8, borderRadius: 4 },
  connectionText: { color: colors.text, fontSize: 13, fontWeight: '600' },
  connectionLost: { color: colors.danger },
  clock: { color: colors.muted, fontSize: 12, fontWeight: '500' },
  steps: { flexDirection: 'row', alignItems: 'center' },
  step: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  stepLine: { width: 24, height: 1, backgroundColor: colors.borderStrong, marginHorizontal: 8 },
  stepLineDone: { backgroundColor: colors.primary },
  stepBadge: {
    width: 24,
    height: 24,
    borderRadius: 12,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderColor: colors.borderStrong,
    backgroundColor: colors.surface,
  },
  stepBadgeActive: { borderColor: colors.primary, backgroundColor: colors.primary },
  stepBadgeDone: { borderColor: colors.primary, backgroundColor: colors.primary },
  stepNumber: { color: colors.muted, fontSize: 12, fontWeight: '700' },
  stepNumberActive: { color: '#ffffff' },
  stepLabel: { color: colors.muted, fontSize: 13, fontWeight: '500' },
  stepLabelActive: { color: colors.text, fontWeight: '700' },
  info: {
    flexDirection: 'row',
    backgroundColor: colors.surfaceMuted,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  infoCell: { flex: 1, paddingVertical: 10, paddingHorizontal: 20, gap: 2 },
  infoCellDivider: { borderRightWidth: 1, borderRightColor: colors.border },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.2 },
  infoValue: { color: colors.text, fontSize: 15, fontWeight: '600' },
  toast: { position: 'absolute', bottom: 16, left: 20, right: 20, zIndex: 20, borderRadius: radius.control, ...overlayShadow },
  busy: {
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    zIndex: 30,
    alignItems: 'center',
    justifyContent: 'center',
  },
  busyBox: {
    width: 80,
    height: 80,
    borderRadius: radius.card,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    ...overlayShadow,
  },
  prompt: {
    flex: 1,
    minHeight: 260,
    alignItems: 'center',
    justifyContent: 'center',
    gap: 8,
    backgroundColor: colors.surface,
    padding: 28,
  },
  promptStep: { color: colors.primary, fontSize: 12, fontWeight: '700', letterSpacing: 1.4 },
  promptTitle: { color: colors.text, fontSize: 22, fontWeight: '700', textAlign: 'center' },
  promptDetail: { color: colors.muted, fontSize: 15, textAlign: 'center', maxWidth: 480, lineHeight: 22, marginBottom: 6 },
  glyph: {
    width: 104,
    height: 104,
    borderRadius: 52,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.primarySoft,
    marginBottom: 10,
  },
  glyphAlert: { backgroundColor: colors.dangerSoft },
  glyphAlertText: { color: colors.danger, fontSize: 56, fontWeight: '700', lineHeight: 66 },
  card: {
    width: 64,
    height: 44,
    borderRadius: 7,
    borderWidth: 2,
    borderColor: colors.primary,
    backgroundColor: colors.surface,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 6,
    gap: 6,
  },
  cardPhoto: { width: 15, height: 20, borderRadius: 3, backgroundColor: colors.primary },
  cardLines: { flex: 1, gap: 5 },
  cardLine: { height: 3, width: '60%', borderRadius: 2, backgroundColor: colors.primary, opacity: 0.45 },
  cardLineLong: { width: '100%', opacity: 1 },
  bundleLayer: {
    position: 'absolute',
    width: 58,
    height: 32,
    borderRadius: 6,
    borderWidth: 2,
    borderColor: colors.primary,
    backgroundColor: colors.surface,
  },
  bundleLayerBack: { top: 26, opacity: 0.4 },
  bundleLayerMiddle: { top: 35, opacity: 0.7 },
  bundleLayerFront: { top: 44, alignItems: 'flex-end', justifyContent: 'center', paddingRight: 6 },
  bundleTag: { width: 12, height: 12, borderRadius: 3, backgroundColor: colors.primary },
});
