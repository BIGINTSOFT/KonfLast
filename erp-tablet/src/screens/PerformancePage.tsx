import { memo } from 'react';
import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import type { PerformanceComparison, PerformancePeriod, TabletPerformance } from '../api/types';
import { BarChart } from '../ui/BarChart';
import { Donut } from '../ui/Donut';
import { colors, radius } from '../ui/theme';

interface PerformancePageProps {
  performance: TabletPerformance | null;
  /** İlk yükleme sürüyor. */
  loading: boolean;
  /** Son yükleme başarısız (bağlantı yok); eldeki değer gösterilir. */
  failed: boolean;
  period: PerformancePeriod;
  onPeriodChange: (period: PerformancePeriod) => void;
}

/** Halka renkleri: her ölçünün kendi rengi; durum (iyi / izlenmeli / düşük) ayrı etiketle gösterilir. */
const EFFICIENCY_COLOR = colors.primary;
const QUALITY_COLOR = colors.brandAccent;
const DONUT_SIZE = 164;

const periods: { key: PerformancePeriod; label: string }[] = [
  { key: 'TODAY', label: 'Bugün' },
  { key: 'WEEK', label: 'Bu hafta' },
];

type Level = { label: string; color: string; background: string };
const levels: Record<'good' | 'watch' | 'low', Level> = {
  good: { label: 'İyi', color: colors.success, background: colors.successSoft },
  watch: { label: 'İzlenmeli', color: colors.warning, background: colors.warningSoft },
  low: { label: 'Düşük', color: colors.danger, background: colors.dangerSoft },
};

/** Verim eşikleri: %90 ve üstü iyi, %70–90 izlenmeli, altı düşük. */
function efficiencyLevel(percent: number | null): Level | null {
  if (percent === null) {
    return null;
  }
  return percent >= 90 ? levels.good : percent >= 70 ? levels.watch : levels.low;
}

/** Kalite eşikleri: %98 ve üstü iyi, %95–98 izlenmeli, altı düşük. */
function qualityLevel(percent: number | null): Level | null {
  if (percent === null) {
    return null;
  }
  return percent >= 98 ? levels.good : percent >= 95 ? levels.watch : levels.low;
}

const formatNumber = (value: number | null, digits = 0) =>
  value === null ? '—' : value.toLocaleString('tr-TR', { maximumFractionDigits: digits });

const formatMinutes = (minutes: number) => {
  const total = Math.round(minutes);
  return total >= 60 ? `${Math.floor(total / 60)} sa ${total % 60} dk` : `${total} dk`;
};

/** Personelin performansı: dönem seçimi, verim ve kalite kartları (halka, ayrıntı, kıyas), adet grafiği. */
export const PerformancePage = memo(function PerformancePage({
  performance,
  loading,
  failed,
  period,
  onPeriodChange,
}: PerformancePageProps) {
  const header = (
    <View style={styles.header}>
      <View style={styles.headerText}>
        <Text style={styles.caption}>PERFORMANSIM</Text>
        {failed && performance && <Text style={styles.stale}>Bağlantı yok; son alınan değerler gösteriliyor.</Text>}
      </View>
      {loading && performance && <ActivityIndicator size="small" color={colors.primary} testID="performance-refreshing" />}
      <View style={styles.segmented} accessibilityRole="tablist">
        {periods.map(item => (
          <Pressable
            key={item.key}
            accessibilityRole="tab"
            accessibilityState={{ selected: period === item.key }}
            onPress={() => onPeriodChange(item.key)}
            style={[styles.segment, period === item.key && styles.segmentActive]}
            testID={`performance-period-${item.key.toLowerCase()}`}>
            <Text style={[styles.segmentText, period === item.key && styles.segmentTextActive]}>{item.label}</Text>
          </Pressable>
        ))}
      </View>
    </View>
  );

  if (!performance) {
    return (
      <View style={styles.page}>
        {header}
        <View style={styles.empty}>
          {loading ? (
            <ActivityIndicator size="large" color={colors.primary} />
          ) : (
            <Text style={styles.emptyText}>
              {failed ? 'Performans bilgisi alınamadı. Bağlantı gelince yenilenir.' : 'Performans için personel kartınızı okutun.'}
            </Text>
          )}
        </View>
      </View>
    );
  }

  const total = performance.produced + performance.rejected;
  const isToday = performance.period === 'TODAY';

  return (
    <View style={styles.page} testID="performance-page">
      {header}

      <View style={[styles.cards, loading && styles.refreshing]}>
        <MetricCard
          title="Verim"
          percent={performance.efficiencyPercent}
          color={EFFICIENCY_COLOR}
          level={efficiencyLevel(performance.efficiencyPercent)}
          rows={[
            { label: 'Üretilen / hedef', value: `${formatNumber(performance.produced)} / ${formatNumber(performance.targetQuantity)} adet` },
            { label: 'Kazanılan süre', value: formatMinutes(performance.earnedMinutes) },
            { label: 'Çalışılabilir süre', value: formatMinutes(performance.availableMinutes) },
            { label: 'Demet bekleme', value: formatMinutes(performance.waitingMinutes) },
          ]}
          comparisons={(performance.comparisons ?? []).map(item => ({ label: item.label, value: item.efficiencyPercent }))}
          testID="performance-efficiency"
        />
        <MetricCard
          title="Kalite"
          percent={performance.qualityPercent}
          color={QUALITY_COLOR}
          level={qualityLevel(performance.qualityPercent)}
          rows={[
            { label: 'Sağlam', value: `${formatNumber(performance.produced)} adet` },
            { label: 'Hatalı', value: `${formatNumber(performance.rejected)} adet` },
            { label: 'Toplam bildirilen', value: `${formatNumber(total)} adet` },
          ]}
          comparisons={(performance.comparisons ?? []).map((item: PerformanceComparison) => ({ label: item.label, value: item.qualityPercent }))}
          testID="performance-quality"
        />
      </View>

      <View style={[styles.chartCard, loading && styles.refreshing]}>
        <View style={styles.chartHeader}>
          <Text style={styles.chartTitle}>{isToday ? 'Saatlik üretim' : 'Günlük üretim'}</Text>
          <View style={styles.legend}>
            <View style={[styles.legendSwatch, { backgroundColor: EFFICIENCY_COLOR }]} />
            <Text style={styles.legendText}>Sağlam</Text>
            <View style={[styles.legendSwatch, { backgroundColor: colors.danger }]} />
            <Text style={styles.legendText}>Hatalı</Text>
            {isToday && performance.targetPerHour !== null && (
              <>
                <View style={styles.legendTarget} />
                <Text style={styles.legendText}>Saatlik hedef</Text>
              </>
            )}
          </View>
        </View>
        {(performance.buckets ?? []).length > 0 ? (
          <BarChart
            buckets={performance.buckets ?? []}
            target={isToday ? performance.targetPerHour : null}
            color={EFFICIENCY_COLOR}
          />
        ) : (
          <Text style={styles.emptyText}>Bu dönemde bildirim yok.</Text>
        )}
      </View>
    </View>
  );
});

interface MetricCardProps {
  title: string;
  percent: number | null;
  color: string;
  level: Level | null;
  rows: { label: string; value: string }[];
  comparisons: { label: string; value: number | null }[];
  testID: string;
}

/** Halka ve yanında ölçünün ayrıntıları; altta kıyas dönemleri ve fark (puan). */
function MetricCard({ title, percent, color, level, rows, comparisons, testID }: MetricCardProps) {
  return (
    <View style={styles.card}>
      <Donut percent={percent} label={title.toLocaleUpperCase('tr-TR')} color={color} status={level} size={DONUT_SIZE} thickness={18} testID={testID} />
      <View style={styles.cardBody}>
        {rows.map(row => (
          <View key={row.label} style={styles.row}>
            <Text style={styles.rowLabel}>{row.label}</Text>
            <Text style={styles.rowValue} numberOfLines={1}>{row.value}</Text>
          </View>
        ))}
        {comparisons.length > 0 && (
          <View style={styles.comparisons}>
            {comparisons.map(item => (
              <Comparison key={item.label} label={item.label} value={item.value} current={percent} />
            ))}
          </View>
        )}
      </View>
    </View>
  );
}

/** Kıyas: o dönemin değeri ve şimdiki değerle farkı (yukarı yeşil, aşağı kırmızı). */
function Comparison({ label, value, current }: { label: string; value: number | null; current: number | null }) {
  const diff = value !== null && current !== null ? Math.round(current - value) : null;
  const tone = diff === null || diff === 0 ? colors.muted : diff > 0 ? colors.success : colors.danger;
  return (
    <View style={styles.row}>
      <Text style={styles.rowLabel}>{label}</Text>
      <Text style={styles.rowValue}>
        {value === null ? '—' : `%${Math.round(value)}`}
        {diff !== null && diff !== 0 && (
          <Text style={[styles.diff, { color: tone }]}>  {diff > 0 ? '▲' : '▼'} {Math.abs(diff)} puan</Text>
        )}
      </Text>
    </View>
  );
}

const styles = StyleSheet.create({
  page: { flex: 1, padding: 16, gap: 12, backgroundColor: colors.background },
  header: { flexDirection: 'row', alignItems: 'center', gap: 12 },
  headerText: { flex: 1, gap: 2 },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  stale: { color: colors.warning, fontSize: 13, fontWeight: '600' },
  segmented: { flexDirection: 'row', borderWidth: 1, borderColor: colors.borderStrong, borderRadius: radius.control, overflow: 'hidden' },
  segment: { paddingVertical: 8, paddingHorizontal: 16, backgroundColor: colors.surface },
  segmentActive: { backgroundColor: colors.primary },
  segmentText: { color: colors.textSecondary, fontSize: 14, fontWeight: '600' },
  segmentTextActive: { color: colors.surface, fontWeight: '700' },
  cards: { flexDirection: 'row', gap: 12 },
  // Yeni dönemin verisi beklenirken eldeki görünüm soluk kalır.
  refreshing: { opacity: 0.5 },
  card: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 18,
    padding: 16,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
  },
  cardBody: { flex: 1, gap: 6 },
  row: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'baseline', gap: 8 },
  rowLabel: { color: colors.muted, fontSize: 13, fontWeight: '600' },
  rowValue: { color: colors.text, fontSize: 14, fontWeight: '700', fontVariant: ['tabular-nums'] },
  diff: { fontSize: 12, fontWeight: '700' },
  comparisons: { gap: 6, marginTop: 4, paddingTop: 8, borderTopWidth: 1, borderTopColor: colors.border },
  chartCard: {
    flex: 1,
    gap: 10,
    padding: 16,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
  },
  chartHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', gap: 12 },
  chartTitle: { color: colors.text, fontSize: 15, fontWeight: '700' },
  legend: { flexDirection: 'row', alignItems: 'center', gap: 6 },
  legendSwatch: { width: 10, height: 10, borderRadius: 2, marginLeft: 8 },
  legendTarget: { width: 16, borderTopWidth: 1, borderStyle: 'dashed', borderTopColor: colors.brandAccent, marginLeft: 8 },
  legendText: { color: colors.muted, fontSize: 12, fontWeight: '600' },
  empty: { flex: 1, alignItems: 'center', justifyContent: 'center', padding: 24 },
  emptyText: { color: colors.muted, fontSize: 15, textAlign: 'center' },
});
