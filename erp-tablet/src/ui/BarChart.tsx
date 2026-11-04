import { StyleSheet, Text, View } from 'react-native';
import type { PerformanceBucket } from '../api/types';
import { colors } from './theme';

interface BarChartProps {
  buckets: PerformanceBucket[];
  /** Dilim başına hedef adet; verilirse kesikli çizgiyle gösterilir. */
  target?: number | null;
  /** Sağlam adet rengi. */
  color: string;
}

const MAX_BAR_WIDTH = 44;

/**
 * Sütun grafik (ek kütüphane yok): her dilimde sağlam adet, üstünde hatalı adet; içinde bulunulan dilim vurgulu.
 * Yükseklik bulunduğu alanı doldurur.
 */
export function BarChart({ buckets, target, color }: BarChartProps) {
  const peak = Math.max(1, target ?? 0, ...buckets.map(bucket => bucket.produced + bucket.rejected));
  const scale = peak * 1.15;
  const targetRatio = target ? target / scale : null;

  return (
    <View style={styles.chart} accessibilityRole="image">
      <View style={styles.plot}>
        {targetRatio !== null && (
          <View style={[styles.target, { bottom: `${targetRatio * 100}%` }]} pointerEvents="none">
            <Text style={styles.targetLabel}>Hedef {Math.round(target!)}</Text>
          </View>
        )}
        {buckets.map(bucket => {
          const total = bucket.produced + bucket.rejected;
          return (
            <View key={bucket.label} style={styles.column}>
              <Text style={[styles.value, bucket.current && styles.valueCurrent]}>{total > 0 ? total : ''}</Text>
              <View style={[styles.bar, { height: `${(total / scale) * 100}%` }, !bucket.current && styles.barPast]}>
                {bucket.rejected > 0 && (
                  <View style={[styles.rejected, { flex: bucket.rejected }]} />
                )}
                {bucket.produced > 0 && <View style={{ flex: bucket.produced, backgroundColor: color }} />}
              </View>
            </View>
          );
        })}
      </View>
      <View style={styles.labels}>
        {buckets.map(bucket => (
          <Text key={bucket.label} style={[styles.label, bucket.current && styles.labelCurrent]}>
            {bucket.label}
          </Text>
        ))}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  chart: { flex: 1, gap: 6 },
  plot: {
    flex: 1,
    flexDirection: 'row',
    alignItems: 'flex-end',
    gap: 8,
    borderBottomWidth: 1,
    borderBottomColor: colors.borderStrong,
  },
  column: { flex: 1, height: '100%', alignItems: 'center', justifyContent: 'flex-end', gap: 3 },
  bar: { width: '70%', maxWidth: MAX_BAR_WIDTH, minHeight: 2, borderTopLeftRadius: 4, borderTopRightRadius: 4, overflow: 'hidden' },
  // Geçmiş dilimler biraz soluk; içinde bulunulan dilim tam renk.
  barPast: { opacity: 0.75 },
  rejected: { backgroundColor: colors.danger },
  value: { color: colors.muted, fontSize: 11, fontWeight: '700', fontVariant: ['tabular-nums'] },
  valueCurrent: { color: colors.text },
  target: { position: 'absolute', left: 0, right: 0, borderTopWidth: 1, borderStyle: 'dashed', borderTopColor: colors.brandAccent },
  targetLabel: { position: 'absolute', right: 0, top: -16, color: colors.brandAccentText, fontSize: 11, fontWeight: '700' },
  labels: { flexDirection: 'row', gap: 8 },
  label: { flex: 1, textAlign: 'center', color: colors.muted, fontSize: 11, fontWeight: '600' },
  labelCurrent: { color: colors.primary, fontWeight: '800' },
});
