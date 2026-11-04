import { StyleSheet, Text, View } from 'react-native';
import { colors } from './theme';

interface DonutProps {
  /** Yüzde; null ise henüz hesaplanamadı ("—"). %100'ü aşarsa halka dolu kalır, ortada gerçek değer yazar. */
  percent: number | null;
  label: string;
  /** Halkanın rengi: her ölçünün kendi rengi (yan yana halkalar birbirinden ayrılsın). */
  color: string;
  /** Ortada, etiketin altında küçük durum (ör. eşiğe göre "İyi" / "Düşük"). */
  status?: { label: string; color: string; background: string } | null;
  size?: number;
  thickness?: number;
  testID?: string;
}

/**
 * Ortası boş halka grafik. Ek kütüphane kullanmaz: halka iki yarıdan çizilir; her yarıda, kenarlığının yarısı
 * renkli bir daire döndürülerek doluluk gösterilir (sağ yarı 0–180°, sol yarı 180–360°).
 */
export function Donut({ percent, label, color, status, size = 220, thickness = 22, testID }: DonutProps) {
  const fill = percent === null ? 0 : Math.max(0, Math.min(100, percent)) / 100;
  const right = Math.min(fill, 0.5) * 360;
  const left = Math.max(0, fill - 0.5) * 360;

  const ring = (rotation: number) => (
    <View
      style={[
        styles.ring,
        {
          width: size,
          height: size,
          borderRadius: size / 2,
          borderWidth: thickness,
          borderTopColor: color,
          borderLeftColor: color,
          transform: [{ rotate: `${rotation}deg` }],
        },
      ]}
    />
  );

  return (
    <View style={[styles.donut, { width: size, height: size }]} testID={testID} accessibilityLabel={`${label}: ${percent === null ? 'yok' : `%${Math.round(percent)}`}`}>
      <View style={[styles.track, { width: size, height: size, borderRadius: size / 2, borderWidth: thickness }]} />

      {/* Sağ yarı: sol yarısı renkli halka (−45°), dolulukla döndükçe sağ yarıya girer. */}
      {fill > 0 && (
        <View style={[styles.half, { left: size / 2, width: size / 2, height: size }]}>
          <View style={{ position: 'absolute', left: -size / 2 }}>{ring(right - 45)}</View>
        </View>
      )}
      {/* Sol yarı: sağ yarısı renkli halka (135°), %50'den sonra dönerek sol yarıya girer. */}
      {fill > 0.5 && (
        <View style={[styles.half, { left: 0, width: size / 2, height: size }]}>
          <View style={{ position: 'absolute', left: 0 }}>{ring(135 + left)}</View>
        </View>
      )}

      <View style={styles.center}>
        <Text style={[styles.value, { fontSize: size * 0.22 }]}>
          {percent === null ? '—' : `%${Math.round(percent)}`}
        </Text>
        <Text style={styles.label}>{label}</Text>
        {status && (
          <View style={[styles.status, { backgroundColor: status.background }]}>
            <Text style={[styles.statusText, { color: status.color }]}>{status.label}</Text>
          </View>
        )}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  donut: { alignItems: 'center', justifyContent: 'center' },
  track: { position: 'absolute', top: 0, left: 0, borderColor: colors.neutralSoft },
  half: { position: 'absolute', top: 0, overflow: 'hidden' },
  ring: { borderRightColor: 'transparent', borderBottomColor: 'transparent' },
  center: { alignItems: 'center', gap: 2 },
  value: { color: colors.text, fontWeight: '800', fontVariant: ['tabular-nums'] },
  label: { color: colors.muted, fontSize: 14, fontWeight: '700', letterSpacing: 1 },
  status: { marginTop: 4, paddingVertical: 2, paddingHorizontal: 10, borderRadius: 999 },
  statusText: { fontSize: 12, fontWeight: '700' },
});
