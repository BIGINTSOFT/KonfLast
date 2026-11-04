import { ActivityIndicator, Pressable, ScrollView, StyleSheet, Text, View } from 'react-native';
import { Banner, BigButton } from './controls';
import type { DialogDetail, ReasonOption } from './Dialog';
import { colors, radius } from './theme';

interface PanelHeaderProps {
  caption: string;
  title: string;
  description?: string;
  onCancel: () => void;
  cancelTestID: string;
}

/** İş alanı panellerinin başlığı: ne yapıldığı ve "Vazgeç". */
function PanelHeader({ caption, title, description, onCancel, cancelTestID }: PanelHeaderProps) {
  return (
    <View style={styles.header}>
      <View style={styles.headerText}>
        <Text style={styles.caption}>{caption}</Text>
        <Text style={styles.title}>{title}</Text>
        {description && <Text style={styles.description}>{description}</Text>}
      </View>
      <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID={cancelTestID} />
    </View>
  );
}

interface ReasonPanelProps {
  caption: string;
  title: string;
  description?: string;
  reasons: ReasonOption[];
  /** `danger`: arıza gibi olumsuz bildirimler. */
  tone?: 'primary' | 'danger';
  busy?: boolean;
  /** Kutucuğa dokunmak seçer ve onaylar (ayrı onay düğmesi yok). */
  onSelect: (reasonId: number) => void;
  onCancel: () => void;
  /** Kutucuk ve düğme test kimliklerinin öneki: `{önek}-{kod}`, `{önek}-cancel`. */
  testIDPrefix: string;
}

/**
 * Neden seçimi iş alanında (pencere açılmadan): nedenler büyük kutucuklar hâlinde; dokunulan neden hemen bildirilir.
 * Ara ve arıza "Devam et" / "Arızayı kapat" ile geri alınabildiği için ayrı onay istenmez.
 */
export function ReasonPanel({
  caption, title, description, reasons, tone = 'primary', busy, onSelect, onCancel, testIDPrefix,
}: ReasonPanelProps) {
  const accent = tone === 'danger' ? colors.danger : colors.primary;
  const soft = tone === 'danger' ? colors.dangerSoft : colors.primarySoft;

  return (
    <View style={styles.panel} testID={`${testIDPrefix}-panel`}>
      <PanelHeader caption={caption} title={title} description={description} onCancel={onCancel} cancelTestID={`${testIDPrefix}-cancel`} />
      <ScrollView contentContainerStyle={styles.tiles}>
        {reasons.map(reason => (
          <Pressable
            key={reason.id}
            testID={`${testIDPrefix}-${reason.code}`}
            disabled={busy}
            onPress={() => onSelect(reason.id)}
            accessibilityRole="button"
            style={({ pressed }) => [
              styles.tile,
              { borderColor: colors.border },
              pressed && { backgroundColor: soft, borderColor: accent },
              busy && styles.tileBusy,
            ]}>
            <View style={[styles.tileMark, { backgroundColor: accent }]} />
            <Text style={styles.tileText} numberOfLines={2}>
              {reason.name}
            </Text>
          </Pressable>
        ))}
      </ScrollView>
      {busy && (
        <View style={styles.busy}>
          <ActivityIndicator color={accent} />
          <Text style={styles.busyText}>Bildiriliyor…</Text>
        </View>
      )}
    </View>
  );
}

interface AuthorizationPanelProps {
  title: string;
  /** Onaylanacak işlemin bilgileri (ör. makine, neden). */
  details?: DialogDetail[];
  /** Kartını okutabilecek görevler (ör. "Bant Şefi"). */
  roles: string[];
  /** Okutulan kart reddedildiyse sunucunun açıklaması. */
  error?: string | null;
  busy?: boolean;
  onCancel: () => void;
}

/** Yetkili onayı iş alanında: işlem, yetkili personelin kartı okutulana kadar bekler. */
export function AuthorizationPanel({ title, details = [], roles, error, busy, onCancel }: AuthorizationPanelProps) {
  const rows: DialogDetail[] = [...details, { label: 'Yetkili görev', value: roles.join(', ') }];

  return (
    <View style={styles.panel} testID="authorization-panel">
      <PanelHeader caption="YETKİLİ ONAYI" title={title} onCancel={onCancel} cancelTestID="authorization-cancel" />
      <View style={styles.authorization}>
        <View style={styles.cardGlyph} accessibilityElementsHidden>
          <View style={styles.cardGlyphPhoto} />
          <View style={styles.cardGlyphLines}>
            <View style={[styles.cardGlyphLine, styles.cardGlyphLineLong]} />
            <View style={styles.cardGlyphLine} />
          </View>
        </View>
        <Text style={styles.authorizationTitle} testID="authorization-prompt">
          {busy ? 'Kart doğrulanıyor…' : 'Yetkili kartını okutun'}
        </Text>

        <View style={styles.details}>
          {rows.map((row, index) => (
            <View key={row.label} style={[styles.detailRow, index > 0 && styles.detailDivider]}>
              <Text style={styles.detailLabel}>{row.label}</Text>
              <Text style={styles.detailValue}>{row.value}</Text>
            </View>
          ))}
        </View>

        {error && <Banner kind="error" text={error} testID="authorization-message" />}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  panel: { flex: 1, backgroundColor: colors.background },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 16,
    paddingVertical: 14,
    paddingHorizontal: 20,
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
  },
  headerText: { flex: 1, gap: 2 },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  title: { color: colors.text, fontSize: 20, fontWeight: '700' },
  description: { color: colors.muted, fontSize: 14 },
  // Dört sütun: uzun neden listesi (ör. 20+ arıza) çoğunlukla kaydırmadan görünür.
  tiles: { flexDirection: 'row', flexWrap: 'wrap', gap: 10, padding: 16 },
  tile: {
    width: '23.8%',
    minHeight: 68,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingVertical: 10,
    paddingHorizontal: 12,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderRadius: radius.card,
  },
  tileBusy: { opacity: 0.5 },
  tileMark: { width: 4, alignSelf: 'stretch', borderRadius: 2 },
  tileText: { flex: 1, color: colors.text, fontSize: 15, fontWeight: '700' },
  busy: { flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 10, paddingBottom: 20 },
  busyText: { color: colors.muted, fontSize: 15, fontWeight: '600' },
  authorization: { flex: 1, alignItems: 'center', justifyContent: 'center', gap: 18, padding: 24 },
  cardGlyph: {
    width: 120,
    height: 80,
    borderRadius: 10,
    borderWidth: 3,
    borderColor: colors.primary,
    backgroundColor: colors.primarySoft,
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 12,
    gap: 12,
  },
  cardGlyphPhoto: { width: 28, height: 36, borderRadius: 4, backgroundColor: colors.primary },
  cardGlyphLines: { flex: 1, gap: 8 },
  cardGlyphLine: { height: 6, width: '60%', borderRadius: 3, backgroundColor: colors.primary, opacity: 0.45 },
  cardGlyphLineLong: { width: '100%', opacity: 1 },
  authorizationTitle: { color: colors.text, fontSize: 22, fontWeight: '700' },
  details: {
    width: '100%',
    maxWidth: 560,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
  },
  detailRow: { flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 10, paddingHorizontal: 16 },
  detailDivider: { borderTopWidth: 1, borderTopColor: colors.border },
  detailLabel: { width: 130, color: colors.muted, fontSize: 14, fontWeight: '600' },
  detailValue: { flex: 1, color: colors.text, fontSize: 16, fontWeight: '600' },
});
