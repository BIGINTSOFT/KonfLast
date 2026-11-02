import { useState } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import type { ReactNode } from 'react';
import { Banner, BigButton } from './controls';
import { colors, overlayShadow, radius } from './theme';

interface DialogFrameProps {
  title: string;
  /** Başlığın altındaki kısa açıklama. */
  description?: string;
  onClose: () => void;
  children: ReactNode;
  /** Alt çubuktaki düğmeler (sağa yaslanır). */
  footer: ReactNode;
  /** Çok sütunlu içerik (ör. operasyon listesi) için geniş pencere. */
  wide?: boolean;
  testID?: string;
}

/**
 * Ekranın ortasında açılan pencere: başlık çubuğu, içerik ve düğme çubuğu. Arka plan karartılır;
 * karartılmış alana ya da kapatma düğmesine basınca pencere hiçbir işlem yapmadan kapanır.
 */
export function DialogFrame({ title, description, onClose, children, footer, wide, testID }: DialogFrameProps) {
  return (
    <View style={styles.overlay} testID={testID} accessibilityViewIsModal>
      <Pressable style={styles.backdrop} onPress={onClose} accessibilityLabel="Pencereyi kapat" />

      <View style={[styles.dialog, wide && styles.dialogWide]} accessibilityRole="alert">
        <View style={styles.header}>
          <View style={styles.headerText}>
            <Text style={styles.title} accessibilityRole="header">
              {title}
            </Text>
            {description && <Text style={styles.description}>{description}</Text>}
          </View>
          <Pressable
            onPress={onClose}
            accessibilityRole="button"
            accessibilityLabel="Kapat"
            style={({ pressed }) => [styles.close, pressed && styles.closePressed]}>
            <Text style={styles.closeText}>✕</Text>
          </Pressable>
        </View>

        <View style={styles.body}>{children}</View>

        <View style={styles.footer}>{footer}</View>
      </View>
    </View>
  );
}

export interface ReasonOption {
  id: number;
  code: string;
  name: string;
}

interface ReasonDialogProps {
  title: string;
  description?: string;
  reasons: ReasonOption[];
  /** Listenin üstündeki etiket; verilmezse "NEDEN". */
  listLabel?: string;
  /** Onay düğmesinin metni (ör. "Ara ver", "Arızayı bildir"). */
  confirmLabel: string;
  /** `danger`: arıza gibi olumsuz bildirimler. */
  tone?: 'primary' | 'danger';
  busy?: boolean;
  onConfirm: (reasonId: number) => void;
  onCancel: () => void;
  /** Satır ve düğme test kimliklerinin öneki: `{önek}-{kod}`, `{önek}-confirm`, `{önek}-cancel`. */
  testIDPrefix: string;
}

/** Neden seçimi: listeden bir neden işaretlenir, ardından onaylanır (yanlışlıkla dokunma işlem başlatmaz). */
export function ReasonDialog({
  title, description, reasons, listLabel = 'NEDEN', confirmLabel, tone = 'primary', busy, onConfirm, onCancel,
  testIDPrefix,
}: ReasonDialogProps) {
  const [selected, setSelected] = useState<number | null>(null);
  const accent = tone === 'danger' ? colors.danger : colors.primary;
  const soft = tone === 'danger' ? colors.dangerSoft : colors.primarySoft;

  return (
    <DialogFrame
      title={title}
      description={description}
      onClose={onCancel}
      testID={`${testIDPrefix}-dialog`}
      footer={
        <>
          <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID={`${testIDPrefix}-cancel`} />
          <BigButton
            label={confirmLabel}
            tone={tone}
            size="compact"
            disabled={selected === null}
            busy={busy}
            onPress={() => selected !== null && onConfirm(selected)}
            testID={`${testIDPrefix}-confirm`}
          />
        </>
      }>
      <Text style={styles.fieldLabel}>{listLabel}</Text>
      <ScrollView style={styles.list} accessibilityRole="radiogroup">
        {reasons.map((reason, index) => {
          const isSelected = reason.id === selected;
          return (
            <Pressable
              key={reason.id}
              testID={`${testIDPrefix}-${reason.code}`}
              onPress={() => setSelected(reason.id)}
              accessibilityRole="radio"
              accessibilityState={{ selected: isSelected }}
              style={[styles.row, index > 0 && styles.rowDivider, isSelected && { backgroundColor: soft }]}>
              <View style={[styles.radio, isSelected && { borderColor: accent }]}>
                {isSelected && <View style={[styles.radioDot, { backgroundColor: accent }]} />}
              </View>
              <Text style={[styles.rowLabel, isSelected && styles.rowLabelSelected]} numberOfLines={1}>
                {reason.name}
              </Text>
            </Pressable>
          );
        })}
      </ScrollView>
    </DialogFrame>
  );
}

export interface DialogDetail {
  label: string;
  value: string;
}

export interface OperationRow {
  id: number;
  sequenceNo: number;
  name: string;
  /** Adımın durumu (ör. "Tamamlandı", "Hazır", "Başka makinede"). */
  status: string;
  /** Üretilen / planlanan adet (ör. "12 / 20"). */
  quantity: string;
  /** Adımın hedef süresi (ör. "12 dk"). */
  duration: string;
  done: boolean;
  /** Bu makinede şu an başlatılabilir mi. */
  startable: boolean;
}

interface OperationDialogProps {
  title: string;
  description?: string;
  rows: OperationRow[];
  /** Demetin özeti (sipariş, model, renk / beden, adet): listenin üstünde hücreler hâlinde gösterilir. */
  summary?: DialogDetail[];
  /** Başlatılabilen adım yoksa nedenini söyleyen bilgi. */
  notice?: string | null;
  busy?: boolean;
  onStart: (id: number) => void;
  onCancel: () => void;
}

/**
 * Okutulan demetin operasyonları: rotanın tamamı sırasıyla listelenir. Bu makinede başlatılabilen ilk adım
 * seçili gelir; başlatılamayan adımlar yalnızca bilgi olarak görünür.
 */
export function OperationDialog({
  title,
  description,
  rows,
  summary = [],
  notice,
  busy,
  onStart,
  onCancel,
}: OperationDialogProps) {
  const [selected, setSelected] = useState<number | null>(() => rows.find(row => row.startable)?.id ?? null);

  return (
    <DialogFrame
      title={title}
      description={description}
      onClose={onCancel}
      wide
      testID="operation-choice-dialog"
      footer={
        <>
          <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID="operation-choice-cancel" />
          <BigButton
            label="Başlat"
            size="compact"
            disabled={selected === null}
            busy={busy}
            onPress={() => selected !== null && onStart(selected)}
            testID="operation-choice-confirm"
          />
        </>
      }>
      {summary.length > 0 && (
        <View style={styles.summary}>
          {summary.map((item, index) => (
            <View key={item.label} style={[styles.summaryCell, index > 0 && styles.summaryDivider]}>
              <Text style={styles.fieldLabel}>{item.label}</Text>
              <Text style={styles.summaryValue} numberOfLines={1}>
                {item.value}
              </Text>
            </View>
          ))}
        </View>
      )}

      <View style={styles.operationHeader}>
        <Text style={[styles.fieldLabel, styles.operationHeaderName]}>OPERASYON</Text>
        <Text style={[styles.fieldLabel, styles.operationDuration]}>HEDEF SÜRE</Text>
        <Text style={[styles.fieldLabel, styles.operationQuantity]}>ADET</Text>
        <Text style={[styles.fieldLabel, styles.operationStatusHead]}>DURUM</Text>
      </View>
      <ScrollView style={styles.list} accessibilityRole="radiogroup">
        {rows.map((row, index) => {
          const isSelected = row.id === selected;
          return (
            <Pressable
              key={row.id}
              testID={`operation-choice-${row.sequenceNo}`}
              disabled={!row.startable}
              onPress={() => setSelected(row.id)}
              accessibilityRole="radio"
              accessibilityState={{ selected: isSelected, disabled: !row.startable }}
              style={[styles.row, index > 0 && styles.rowDivider, isSelected && styles.operationSelected]}>
              {row.done ? (
                <Text style={styles.operationDone}>✓</Text>
              ) : (
                <View style={[styles.radio, isSelected && styles.radioSelected, !row.startable && styles.radioDisabled]}>
                  {isSelected && <View style={[styles.radioDot, styles.radioDotSelected]} />}
                </View>
              )}
              <Text
                style={[styles.rowLabel, isSelected && styles.rowLabelSelected, !row.startable && styles.operationMuted]}
                numberOfLines={1}>
                {row.sequenceNo} · {row.name}
              </Text>
              <Text style={[styles.operationCell, styles.operationDuration]}>{row.duration}</Text>
              <Text style={[styles.operationCell, styles.operationQuantity]}>{row.quantity}</Text>
              <Text style={[styles.operationStatus, row.done && styles.operationStatusDone, isSelected && styles.operationStatusSelected]}>
                {row.status}
              </Text>
            </Pressable>
          );
        })}
      </ScrollView>

      {notice && <Banner kind="info" text={notice} testID="operation-choice-notice" />}
    </DialogFrame>
  );
}

interface AuthorizationDialogProps {
  title: string;
  /** Neden yetkili kartı istendiği. */
  description?: string;
  /** Onaylanacak işlemin bilgileri (ör. makine, neden). */
  details?: DialogDetail[];
  /** Kartını okutabilecek görevler (ör. "Bant Şefi"). */
  roles: string[];
  /** Okutulan kart reddedildiyse sunucunun açıklaması. */
  error?: string | null;
  busy?: boolean;
  onCancel: () => void;
}

/** Yetkili onayı: işlem, yetkili personelin kartı okutulana kadar bekler. */
export function AuthorizationDialog({
  title,
  description,
  details = [],
  roles,
  error,
  busy,
  onCancel,
}: AuthorizationDialogProps) {
  const rows: DialogDetail[] = [...details, { label: 'Yetkili görev', value: roles.join(', ') }];

  return (
    <DialogFrame
      title={title}
      description={description}
      onClose={onCancel}
      testID="authorization-dialog"
      footer={<BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID="authorization-cancel" />}>
      <View style={styles.authorization}>
        <View style={styles.cardGlyph} accessibilityElementsHidden>
          <View style={styles.cardGlyphPhoto} />
          <View style={styles.cardGlyphLines}>
            <View style={[styles.cardGlyphLine, styles.cardGlyphLineLong]} />
            <View style={styles.cardGlyphLine} />
          </View>
        </View>
        <View style={styles.authorizationText}>
          <Text style={styles.fieldLabel}>YETKİLİ ONAYI</Text>
          <Text style={styles.authorizationTitle} testID="authorization-prompt">
            Yetkili kartını okutun
          </Text>
        </View>
      </View>

      <View style={styles.details}>
        {rows.map((row, index) => (
          <View key={row.label} style={[styles.detailRow, index > 0 && styles.rowDivider]}>
            <Text style={styles.detailLabel}>{row.label}</Text>
            <Text style={styles.detailValue}>{row.value}</Text>
          </View>
        ))}
        <View style={[styles.detailRow, styles.rowDivider]}>
          <Text style={styles.detailLabel}>Durum</Text>
          <View style={styles.state}>
            <View style={[styles.stateDot, busy && styles.stateDotBusy]} />
            <Text style={styles.detailValue} testID="authorization-state">
              {busy ? 'Kart doğrulanıyor' : 'Kart bekleniyor'}
            </Text>
          </View>
        </View>
      </View>

      {error && <Banner kind="error" text={error} testID="authorization-message" />}
    </DialogFrame>
  );
}

const ROW_HEIGHT = 50;

const styles = StyleSheet.create({
  overlay: {
    position: 'absolute',
    top: 0,
    right: 0,
    bottom: 0,
    left: 0,
    zIndex: 50,
    alignItems: 'center',
    justifyContent: 'center',
    padding: 24,
  },
  backdrop: { position: 'absolute', top: 0, right: 0, bottom: 0, left: 0, backgroundColor: 'rgba(8, 14, 22, 0.55)' },
  dialog: {
    width: '100%',
    maxWidth: 520,
    backgroundColor: colors.surface,
    borderRadius: radius.card,
    borderWidth: 1,
    borderColor: colors.border,
    overflow: 'hidden',
    ...overlayShadow,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'flex-start',
    gap: 12,
    paddingLeft: 20,
    paddingRight: 10,
    paddingVertical: 14,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerText: { flex: 1, gap: 3 },
  title: { color: colors.text, fontSize: 17, fontWeight: '700' },
  description: { color: colors.muted, fontSize: 14, lineHeight: 20 },
  close: { width: 38, height: 38, borderRadius: radius.control, alignItems: 'center', justifyContent: 'center' },
  closePressed: { backgroundColor: colors.neutralSoft },
  closeText: { color: colors.muted, fontSize: 16, fontWeight: '600' },
  body: { padding: 20, gap: 12 },
  footer: {
    flexDirection: 'row',
    justifyContent: 'flex-end',
    gap: 10,
    paddingHorizontal: 20,
    paddingVertical: 12,
    borderTopWidth: 1,
    borderTopColor: colors.border,
    backgroundColor: colors.surfaceMuted,
  },
  fieldLabel: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  // En fazla altı satır görünür; daha fazlası kaydırılır.
  list: {
    maxHeight: ROW_HEIGHT * 6,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
  },
  row: { minHeight: ROW_HEIGHT, flexDirection: 'row', alignItems: 'center', gap: 14, paddingHorizontal: 16 },
  rowDivider: { borderTopWidth: 1, borderTopColor: colors.border },
  rowLabel: { flex: 1, color: colors.textSecondary, fontSize: 16, fontWeight: '500' },
  rowLabelSelected: { color: colors.text, fontWeight: '600' },
  radio: {
    width: 20,
    height: 20,
    borderRadius: 10,
    borderWidth: 2,
    borderColor: colors.borderStrong,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
  },
  radioDot: { width: 10, height: 10, borderRadius: 5 },
  radioSelected: { borderColor: colors.primary },
  radioDotSelected: { backgroundColor: colors.primary },
  radioDisabled: { borderColor: colors.border, backgroundColor: colors.surfaceMuted },
  operationSelected: { backgroundColor: colors.primarySoft },
  operationDone: { width: 20, textAlign: 'center', color: colors.success, fontSize: 16, fontWeight: '700' },
  operationMuted: { color: colors.muted },
  dialogWide: { maxWidth: 700 },
  summary: {
    flexDirection: 'row',
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surfaceMuted,
  },
  summaryCell: { flex: 1, gap: 3, paddingVertical: 10, paddingHorizontal: 14 },
  summaryDivider: { borderLeftWidth: 1, borderLeftColor: colors.border },
  summaryValue: { color: colors.text, fontSize: 15, fontWeight: '700' },
  operationHeader: { flexDirection: 'row', alignItems: 'center', gap: 14, paddingHorizontal: 16, marginBottom: -4 },
  operationHeaderName: { flex: 1, paddingLeft: 34 },
  operationCell: { color: colors.textSecondary, fontSize: 14, fontWeight: '600' },
  operationDuration: { width: 90, textAlign: 'right' },
  operationStatusHead: { width: 130, textAlign: 'right' },
  operationQuantity: { width: 70, textAlign: 'right' },
  operationStatus: { width: 130, textAlign: 'right', color: colors.muted, fontSize: 13, fontWeight: '600' },
  operationStatusDone: { color: colors.success },
  operationStatusSelected: { color: colors.primary },
  authorization: { flexDirection: 'row', alignItems: 'center', gap: 16 },
  cardGlyph: {
    width: 72,
    height: 48,
    borderRadius: 7,
    borderWidth: 2,
    borderColor: colors.primary,
    backgroundColor: colors.primarySoft,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 7,
    gap: 7,
  },
  cardGlyphPhoto: { width: 17, height: 22, borderRadius: 3, backgroundColor: colors.primary },
  cardGlyphLines: { flex: 1, gap: 5 },
  cardGlyphLine: { height: 4, width: '60%', borderRadius: 2, backgroundColor: colors.primary, opacity: 0.45 },
  cardGlyphLineLong: { width: '100%', opacity: 1 },
  authorizationText: { flex: 1, gap: 4 },
  authorizationTitle: { color: colors.text, fontSize: 17, fontWeight: '700' },
  details: {
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
  },
  detailRow: { minHeight: 42, flexDirection: 'row', alignItems: 'center', gap: 12, paddingHorizontal: 14, paddingVertical: 8 },
  detailLabel: { width: 120, color: colors.muted, fontSize: 13, fontWeight: '600' },
  detailValue: { flex: 1, color: colors.text, fontSize: 15, fontWeight: '600' },
  state: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: 8 },
  stateDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.primary },
  stateDotBusy: { backgroundColor: colors.warning },
});
