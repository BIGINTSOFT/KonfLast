import { useRef, useState } from 'react';
import type { ComponentRef } from 'react';
import { Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { BigButton } from './controls';
import { colors, radius } from './theme';

/** Durum rozetinin rengi. */
export type OperationTone = 'ready' | 'active' | 'done' | 'warning' | 'idle';

export interface OperationRow {
  id: number;
  sequenceNo: number;
  name: string;
  /** Adımın durumu (ör. "Tamamlandı", "Hazır", "Başka makinede"). */
  status: string;
  tone: OperationTone;
  /** Üretilen / planlanan adet (ör. "12 / 20"). */
  quantity: string;
  /** Adımın hedef süresi (ör. "12 dk"). */
  duration: string;
  done: boolean;
  /** Bu makinede şu an başlatılabilir mi. */
  startable: boolean;
}

export interface OperationSummary {
  label: string;
  value: string;
}

/** Bu makinede başlatılabilecek adım yoksa: ne olduğu, ne yapılacağı ve tabloda vurgulanacak sıradaki adım. */
export interface OperationNotice {
  title: string;
  detail: string;
  nextId: number | null;
}

interface OperationPanelProps {
  title: string;
  rows: OperationRow[];
  /** Demetin özeti (sipariş, model, renk / beden, adet): başlığın yanında hücreler hâlinde gösterilir. */
  summary?: OperationSummary[];
  /** Başlatılabilen adım yoksa tablonun üstünde belirgin uyarı olarak gösterilir. */
  notice?: OperationNotice | null;
  busy?: boolean;
  onStart: (id: number) => void;
  onCancel: () => void;
}

const toneStyles: Record<OperationTone, { backgroundColor: string; color: string }> = {
  ready: { backgroundColor: colors.primarySoft, color: colors.primary },
  active: { backgroundColor: colors.warningSoft, color: colors.warning },
  done: { backgroundColor: colors.successSoft, color: colors.success },
  warning: { backgroundColor: colors.dangerSoft, color: colors.danger },
  idle: { backgroundColor: colors.neutralSoft, color: colors.muted },
};

/**
 * Okutulan demetin operasyonları, iş alanında (pencere açılmadan). Rota sırasıyla listelenir; bu makinede
 * başlatılabilen sıradaki adım seçili gelir, başlatılamayanlar yalnızca bilgi olarak görünür. Liste kendi içinde
 * kayar; başlık, özet ve düğmeler yerinde kalır.
 */
export function OperationPanel({ title, rows, summary = [], notice, busy, onStart, onCancel }: OperationPanelProps) {
  const [selected, setSelected] = useState<number | null>(() => rows.find(row => row.startable)?.id ?? null);
  const selectedRow = rows.find(row => row.id === selected);
  const startableCount = rows.filter(row => row.startable).length;
  const doneCount = rows.filter(row => row.done).length;
  // Liste açılınca seçili (yoksa uyarıdaki sıradaki) satır görünür alana kaydırılır; çoğu zaman elle kaydırmak gerekmez.
  const focusId = selected ?? notice?.nextId ?? null;
  const list = useRef<ComponentRef<typeof ScrollView>>(null);
  const scrolledToFocus = useRef(false);
  const showFocusRow = (y: number) => {
    if (!scrolledToFocus.current) {
      scrolledToFocus.current = true;
      // Üstünde bir satır görünsün: operatör rotadaki yerini anlar.
      list.current?.scrollTo({ y: Math.max(0, y - ROW_HEIGHT), animated: false });
    }
  };

  return (
    <View style={styles.panel} testID="operation-choice">
      <View style={styles.header}>
        <View style={styles.titleBlock}>
          <Text style={styles.caption}>OKUTULAN DEMET</Text>
          <Text style={styles.title} numberOfLines={1}>
            {title}
          </Text>
        </View>
        {summary.map(item => (
          <View key={item.label} style={styles.summaryCell}>
            <Text style={styles.caption}>{item.label}</Text>
            <Text style={styles.summaryValue} numberOfLines={1}>
              {item.value}
            </Text>
          </View>
        ))}
      </View>

      <View style={styles.body}>
        {notice && (
          <View style={styles.notice} testID="operation-choice-notice" accessibilityRole="alert">
            <View style={styles.noticeMark}>
              <Text style={styles.noticeMarkText}>!</Text>
            </View>
            <View style={styles.noticeText}>
              <Text style={styles.noticeTitle}>{notice.title}</Text>
              <Text style={styles.noticeDetail}>{notice.detail}</Text>
            </View>
          </View>
        )}

        <View style={styles.table}>
          <View style={styles.tableTitle}>
            <Text style={styles.tableTitleText}>Operasyonlar</Text>
            <Text style={styles.tableTitleMeta}>
              {rows.length} adım · {doneCount} tamamlandı · {startableCount} bu makinede başlatılabilir
            </Text>
          </View>

          <View style={styles.headRow}>
            <View style={styles.markCell} />
            <Text style={[styles.headText, styles.seqCell]}>SIRA</Text>
            <Text style={[styles.headText, styles.nameCell]}>OPERASYON</Text>
            <Text style={[styles.headText, styles.durationCell]}>HEDEF SÜRE</Text>
            <Text style={[styles.headText, styles.quantityCell]}>ADET</Text>
            <Text style={[styles.headText, styles.statusHead]}>DURUM</Text>
          </View>

          <ScrollView
            ref={list}
            style={styles.rows}
            // Fırlatınca liste daha uzağa gider; kaydırılabildiği çubuk ve solan kenarlarla belli olur.
            decelerationRate={0.998}
            persistentScrollbar
            fadingEdgeLength={24}
            accessibilityRole="radiogroup"
            testID="operation-choice-list">
            {rows.map((row, index) => {
              const isSelected = row.id === selected;
              // Seçili satırın zemini açık renkli olduğundan rozeti dolu renkle ayrışır.
              const tone = isSelected ? { backgroundColor: colors.primary, color: colors.surface } : toneStyles[row.tone];
              return (
                <Pressable
                  key={row.id}
                  testID={`operation-choice-${row.sequenceNo}`}
                  disabled={!row.startable}
                  onPress={() => setSelected(row.id)}
                  // Kaydırmak için dokunulan satır basılı görünmez; hareket kaydırma olarak algılanır.
                  unstable_pressDelay={120}
                  onLayout={row.id === focusId ? event => showFocusRow(event.nativeEvent.layout.y) : undefined}
                  accessibilityRole="radio"
                  accessibilityState={{ selected: isSelected, disabled: !row.startable }}
                  style={({ pressed }) => [
                    styles.row,
                    index > 0 && styles.rowDivider,
                    index % 2 === 1 && styles.rowStriped,
                    pressed && styles.rowPressed,
                    isSelected && styles.rowSelected,
                    row.id === notice?.nextId && styles.rowNext,
                  ]}>
                  <View style={styles.markCell}>
                    {row.done ? (
                      <View style={styles.doneMark}>
                        <Text style={styles.doneMarkText}>✓</Text>
                      </View>
                    ) : (
                      <View style={[styles.radio, isSelected && styles.radioSelected, !row.startable && styles.radioDisabled]}>
                        {isSelected && <View style={styles.radioDot} />}
                      </View>
                    )}
                  </View>
                  <Text style={[styles.seq, styles.seqCell, !row.startable && !isSelected && styles.muted]}>{row.sequenceNo}</Text>
                  <Text
                    style={[styles.name, styles.nameCell, isSelected && styles.nameSelected, !row.startable && styles.muted]}
                    numberOfLines={1}>
                    {row.name}
                  </Text>
                  <Text style={[styles.cell, styles.durationCell]}>{row.duration}</Text>
                  <Text style={[styles.cell, styles.quantityCell]}>{row.quantity}</Text>
                  <View style={styles.statusCell}>
                    <View style={[styles.badge, { backgroundColor: tone.backgroundColor }]}>
                      <Text style={[styles.badgeText, { color: tone.color }]} numberOfLines={1}>
                        {row.status}
                      </Text>
                    </View>
                  </View>
                </Pressable>
              );
            })}
          </ScrollView>
        </View>

      </View>

      <View style={styles.footer}>
        <View style={styles.selection}>
          <Text style={styles.caption}>SEÇİLİ OPERASYON</Text>
          <Text style={styles.selectionValue} numberOfLines={1} testID="operation-choice-selected">
            {selectedRow ? `${selectedRow.sequenceNo} · ${selectedRow.name}` : 'Bu makinede başlatılabilecek operasyon yok'}
          </Text>
          {selectedRow && (
            <Text style={styles.selectionMeta}>
              {selectedRow.quantity} adet · hedef {selectedRow.duration}
            </Text>
          )}
        </View>
        <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID="operation-choice-cancel" />
        <BigButton
          label="Operasyonu başlat"
          size="compact"
          disabled={selected === null}
          busy={busy}
          onPress={() => selected !== null && onStart(selected)}
          testID="operation-choice-confirm"
          style={styles.startButton}
        />
      </View>
    </View>
  );
}

const ROW_HEIGHT = 46;

const styles = StyleSheet.create({
  panel: { flex: 1, backgroundColor: colors.background },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },

  header: {
    flexDirection: 'row',
    alignItems: 'stretch',
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  titleBlock: { minWidth: 220, gap: 2, paddingVertical: 10, paddingHorizontal: 20, justifyContent: 'center' },
  title: { color: colors.text, fontSize: 20, fontWeight: '700' },
  summaryCell: {
    flex: 1,
    gap: 3,
    paddingVertical: 10,
    paddingHorizontal: 16,
    justifyContent: 'center',
    borderLeftWidth: 1,
    borderLeftColor: colors.border,
  },
  summaryValue: { color: colors.text, fontSize: 15, fontWeight: '600' },

  body: { flex: 1, padding: 16, gap: 12 },
  table: {
    flex: 1,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
    overflow: 'hidden',
  },
  tableTitle: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    gap: 12,
    paddingVertical: 10,
    paddingHorizontal: 16,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  tableTitleText: { color: colors.text, fontSize: 15, fontWeight: '700' },
  tableTitleMeta: { color: colors.muted, fontSize: 13, fontWeight: '500' },
  headRow: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    minHeight: 34,
    paddingHorizontal: 16,
    backgroundColor: colors.surfaceMuted,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headText: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1 },
  rows: { flex: 1 },
  row: {
    minHeight: ROW_HEIGHT,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingHorizontal: 16,
    borderLeftWidth: 3,
    borderLeftColor: 'transparent',
  },
  rowDivider: { borderTopWidth: 1, borderTopColor: colors.border },
  rowStriped: { backgroundColor: colors.surfaceMuted },
  rowPressed: { backgroundColor: colors.neutralSoft },
  rowSelected: { backgroundColor: colors.primarySoft, borderLeftColor: colors.primary },
  // Bu makinede başlatılamayan sıradaki adım: uyarıyla aynı renkte vurgulanır.
  rowNext: { backgroundColor: colors.warningSoft, borderLeftColor: colors.warning },

  notice: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 14,
    paddingVertical: 14,
    paddingHorizontal: 16,
    backgroundColor: colors.warningSoft,
    borderWidth: 1,
    borderColor: colors.warning,
    borderLeftWidth: 5,
    borderRadius: radius.card,
  },
  noticeMark: {
    width: 36,
    height: 36,
    borderRadius: 18,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.warning,
  },
  noticeMarkText: { color: colors.surface, fontSize: 20, fontWeight: '800' },
  noticeText: { flex: 1, gap: 3 },
  noticeTitle: { color: colors.text, fontSize: 18, fontWeight: '700' },
  noticeDetail: { color: colors.textSecondary, fontSize: 15, fontWeight: '500' },

  markCell: { width: 20, alignItems: 'center' },
  seqCell: { width: 44 },
  nameCell: { flex: 1 },
  durationCell: { width: 92, textAlign: 'right' },
  quantityCell: { width: 76, textAlign: 'right' },
  statusCell: { width: 150, alignItems: 'flex-end' },
  statusHead: { width: 150, textAlign: 'right' },

  radio: {
    width: 18,
    height: 18,
    borderRadius: 9,
    borderWidth: 2,
    borderColor: colors.borderStrong,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.surface,
  },
  radioSelected: { borderColor: colors.primary },
  radioDisabled: { borderColor: colors.border, backgroundColor: colors.surfaceMuted },
  radioDot: { width: 8, height: 8, borderRadius: 4, backgroundColor: colors.primary },
  doneMark: {
    width: 18,
    height: 18,
    borderRadius: 9,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.success,
  },
  doneMarkText: { color: colors.surface, fontSize: 11, fontWeight: '800' },

  seq: { color: colors.textSecondary, fontSize: 14, fontWeight: '600', fontVariant: ['tabular-nums'] },
  name: { color: colors.textSecondary, fontSize: 15, fontWeight: '500' },
  nameSelected: { color: colors.text, fontWeight: '700' },
  muted: { color: colors.muted },
  cell: { color: colors.textSecondary, fontSize: 14, fontWeight: '600', fontVariant: ['tabular-nums'] },
  badge: { borderRadius: radius.control, paddingVertical: 3, paddingHorizontal: 8 },
  badgeText: { fontSize: 12, fontWeight: '700' },

  footer: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 10,
    paddingVertical: 10,
    paddingHorizontal: 20,
    backgroundColor: colors.surface,
    borderTopWidth: 1,
    borderTopColor: colors.border,
  },
  selection: { flex: 1, gap: 1 },
  selectionValue: { color: colors.text, fontSize: 16, fontWeight: '700' },
  selectionMeta: { color: colors.muted, fontSize: 13 },
  startButton: { minWidth: 180 },
});
