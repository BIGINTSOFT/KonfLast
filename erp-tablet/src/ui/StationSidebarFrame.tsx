import type { ReactNode } from 'react';
import { Fragment, useEffect, useRef } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Avatar } from './Avatar';
import { StatusPill } from './controls';
import { colors, radius } from './theme';

const SIDEBAR_WIDTH = 288;
// Personel kartında fotoğraf, kimlik, giriş bilgileri ve bugünkü performans birlikte sığar.
const AVATAR_SIZE = 84;
const SETTINGS_HOLD_MS = 1500;
const BRAND_HIT_SLOP = { top: 12, left: 12, right: 12, bottom: 4 };
const BRAND_RETENTION = { top: 40, left: 40, right: 40, bottom: 40 };

interface StationSidebarFrameProps {
  /** Tabletin açık olan modülü; markanın altında yazar. */
  moduleName: string;
  /** Makine kodu ya da (makine yoksa) tablet kodu. */
  title: string;
  subtitle?: string;
  /** İstasyon bilgileri (bant, makine, tablet); her biri ayrı satırda, kısaltılmadan yazılır. */
  details: { label: string; value: string }[];
  status?: { label: string; color: string };
  statusReason?: string | null;
  /** Markaya uzun basılınca sistem ayarları açılır (yönetici işlemi). */
  onOpenSetup: () => void;
  /** Personel alanının başlığı. */
  personnelCaption?: string;
  /** Personel alanının içeriği. */
  children: ReactNode;
  /** Altta kalan düğme. */
  footer?: ReactNode;
}

/** Sol panel: marka, istasyon ve personel alanı. */
export function StationSidebarFrame({
  moduleName, title, subtitle, details, status, statusReason, onOpenSetup, personnelCaption = 'PERSONEL', children, footer,
}: StationSidebarFrameProps) {
  return (
    <View style={styles.sidebar}>
      <BrandHold moduleName={moduleName} onOpenSetup={onOpenSetup} />

      {/* İstasyon kartı içeriği kadar yer kaplar (bilgiler kısaltılmadan yazılır); boyu oturumdan bağımsızdır. */}
      <View style={styles.section}>
        <Text style={styles.caption}>İSTASYON</Text>
        <Text style={styles.machineCode} testID="station-title" numberOfLines={1}>
          {title}
        </Text>
        {subtitle && (
          <Text style={styles.operation} testID="station-operation">
            {subtitle}
          </Text>
        )}
        <View style={styles.details}>
          {details.map(detail => (
            <View key={detail.label} style={styles.detail}>
              <Text style={styles.detailLabel}>{detail.label}</Text>
              <Text style={styles.detailValue}>{detail.value}</Text>
            </View>
          ))}
        </View>
        {status && <StatusPill label={status.label} color={status.color} onDark testID="station-machine-status" />}
        {statusReason && (
          <Text style={styles.statusReason} testID="station-machine-reason" numberOfLines={2}>
            {statusReason}
          </Text>
        )}
      </View>

      {/* Personel alanı kalan yüksekliği alır (oturum açık / kapalı aynı boy; alanlar kaymaz). */}
      <View style={[styles.section, styles.personnel]}>
        <Text style={styles.caption}>{personnelCaption}</Text>
        {children}
      </View>

      {footer}
    </View>
  );
}

/**
 * Marka alanı: basılı tutulunca sistem ayarları açılır. onLongPress parmak 10 birim oynayınca iptal olduğu için
 * süre burada tutulur; parmak alanın yakınında kaldıkça sayılır. Yönetici girişi olduğu için görsel iz bırakmaz.
 */
function BrandHold({ moduleName, onOpenSetup }: { moduleName: string; onOpenSetup: () => void }) {
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clear = () => {
    if (timer.current) {
      clearTimeout(timer.current);
      timer.current = null;
    }
  };

  useEffect(() => clear, []);

  return (
    <Pressable
      onPressIn={() => {
        clear();
        timer.current = setTimeout(() => {
          timer.current = null;
          onOpenSetup();
        }, SETTINGS_HOLD_MS);
      }}
      onPressOut={clear}
      hitSlop={BRAND_HIT_SLOP}
      pressRetentionOffset={BRAND_RETENTION}
      accessibilityLabel="Tekstil ERP"
      style={styles.brand}>
      <View style={styles.logo} accessibilityElementsHidden>
        <Text style={styles.logoText}>T</Text>
        <View style={styles.logoAccent} />
      </View>
      <View>
        <Text style={styles.brandName}>Tekstil ERP</Text>
        <Text style={styles.brandProduct} testID="station-module">{moduleName}</Text>
      </View>
    </Pressable>
  );
}

export function SidebarSignedOut({ hint }: { hint: string }) {
  return (
    <View style={styles.operator}>
      <Avatar size={AVATAR_SIZE} />
      <Text style={styles.signedOut}>Oturum kapalı</Text>
      <Text style={styles.signedOutHint}>{hint}</Text>
    </View>
  );
}

interface SidebarOperatorProps {
  name: string;
  role?: string;
  code: string;
  status: { label: string; color: string };
  photoUri?: string | null;
  facts?: { label: string; value: string; testID: string }[];
  /** Bilgilerin altında gösterilecek ek içerik (ör. bugünkü performans). */
  children?: ReactNode;
}

export function SidebarOperator({ name, role, code, status, photoUri, facts, children }: SidebarOperatorProps) {
  return (
    <View style={styles.operator}>
      <Avatar size={AVATAR_SIZE} name={name} photoUri={photoUri} ringColor={status.color} />
      <Text style={styles.operatorName} testID="operator-name" numberOfLines={2}>
        {name}
      </Text>
      {role && (
        <Text style={styles.operatorRole} testID="operator-role">
          {role}
        </Text>
      )}
      <Text style={styles.operatorCode} testID="operator-code">
        Sicil No · {code}
      </Text>
      <View style={styles.operatorStatus}>
        <StatusPill label={status.label} color={status.color} onDark testID="operator-status" />
      </View>

      {facts && (
        <View style={styles.facts}>
          {facts.map((fact, index) => (
            <Fragment key={fact.testID}>
              {index > 0 && <View style={styles.factDivider} />}
              <View style={styles.fact}>
                {/* Hep tek satır: uzun değer (ör. "1 sa 06 dk") yazıyı küçültür, kartı uzatıp alanları kaydırmaz. */}
                <Text style={styles.factLabel} numberOfLines={1} adjustsFontSizeToFit minimumFontScale={0.8}>
                  {fact.label}
                </Text>
                <Text style={styles.factValue} testID={fact.testID} numberOfLines={1} adjustsFontSizeToFit minimumFontScale={0.6}>
                  {fact.value}
                </Text>
              </View>
            </Fragment>
          ))}
        </View>
      )}

      {children}
    </View>
  );
}

/** Koyu panelde okunur performans renkleri: verim açık turkuaz, kalite kurumsal altın. */
const EFFICIENCY_BAR = '#4fc3d9';
const QUALITY_BAR = colors.brandAccent;

interface SidebarPerformanceProps {
  efficiencyPercent: number | null;
  qualityPercent: number | null;
  produced: number | null;
  targetQuantity: number | null;
}

/** Personel kartının altında bugünkü performans: verim ve kalite çubukları, üretilen / hedef adet. */
export function SidebarPerformance({ efficiencyPercent, qualityPercent, produced, targetQuantity }: SidebarPerformanceProps) {
  const bars = [
    { label: 'Verim', value: efficiencyPercent, color: EFFICIENCY_BAR, testID: 'operator-efficiency' },
    { label: 'Kalite', value: qualityPercent, color: QUALITY_BAR, testID: 'operator-quality' },
  ];

  return (
    <View style={styles.performance} testID="operator-performance">
      {/* Başlıkta üretilen / hedef adet; her ölçü tek satır: etiket, yatay çubuk, yüzde. */}
      <View style={styles.performanceHead}>
        <Text style={styles.performanceCaption}>BUGÜN</Text>
        <Text style={styles.performanceCount} testID="operator-produced" numberOfLines={1}>
          {produced === null ? '—' : produced} / {targetQuantity === null ? '—' : targetQuantity} adet
        </Text>
      </View>
      {bars.map(bar => (
        <View key={bar.label} style={styles.performanceRow}>
          <Text style={styles.performanceLabel}>{bar.label}</Text>
          <View style={styles.performanceTrack}>
            <View
              style={[styles.performanceFill, { width: `${Math.max(0, Math.min(100, bar.value ?? 0))}%`, backgroundColor: bar.color }]}
            />
          </View>
          <Text style={styles.performanceValue} testID={bar.testID}>
            {bar.value === null ? '—' : `%${Math.round(bar.value)}`}
          </Text>
        </View>
      ))}
    </View>
  );
}

/** Arızalı durumun rengi; istasyon kartındaki neden yazısı da bu renktedir. */
const BREAKDOWN_COLOR = '#ff8a80';

const styles = StyleSheet.create({
  // Alt boşluk yok: "Oturumu kapat" sağdaki beyaz iş alanıyla aynı çizgide biter; kalan yükseklik istasyon kartına geçer.
  sidebar: { width: SIDEBAR_WIDTH, backgroundColor: colors.header, paddingHorizontal: 18, paddingTop: 16, paddingBottom: 0, gap: 14 },
  brand: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: 12,
    paddingBottom: 14,
    borderBottomWidth: 1,
    borderBottomColor: colors.headerBorder,
  },
  logo: {
    width: 36,
    height: 36,
    borderRadius: 8,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: colors.primary,
    overflow: 'hidden',
  },
  logoText: { color: '#ffffff', fontSize: 19, fontWeight: '700' },
  logoAccent: { position: 'absolute', left: 0, right: 0, bottom: 0, height: 3, backgroundColor: colors.brandAccent },
  brandName: { color: colors.headerText, fontSize: 15, fontWeight: '700', letterSpacing: 0.2 },
  brandProduct: { color: colors.headerMuted, fontSize: 12 },
  section: {
    gap: 4,
    padding: 14,
    backgroundColor: colors.headerSurface,
    borderRadius: radius.card,
    borderWidth: 1,
    borderColor: colors.headerBorder,
  },
  personnel: { flex: 1, overflow: 'hidden' },
  details: { gap: 2, marginTop: 4, marginBottom: 8 },
  // Etiket solda, değer sağda; uzun değer kısaltılmaz, alt satıra kayar.
  detail: { flexDirection: 'row', gap: 8 },
  detailLabel: { width: 46, color: colors.headerMuted, fontSize: 11, fontWeight: '600', paddingTop: 1 },
  detailValue: { flex: 1, color: colors.headerText, fontSize: 12, fontWeight: '500' },
  caption: { color: colors.headerMuted, fontSize: 11, fontWeight: '700', letterSpacing: 1.4, marginBottom: 2 },
  machineCode: { color: colors.headerText, fontSize: 19, fontWeight: '700' },
  operation: { color: colors.headerText, fontSize: 14, fontWeight: '600' },
  statusReason: { color: BREAKDOWN_COLOR, fontSize: 13, fontWeight: '600', marginTop: 6 },
  // Kartın kalan yüksekliğini alır: bugünkü performans kartın altına yaslanır.
  operator: { flex: 1, alignItems: 'center', gap: 1, paddingTop: 2 },
  operatorName: { color: colors.headerText, fontSize: 17, fontWeight: '700', textAlign: 'center', marginTop: 6 },
  operatorRole: { color: colors.headerText, fontSize: 13, fontWeight: '600' },
  operatorCode: { color: colors.headerMuted, fontSize: 13 },
  operatorStatus: { marginTop: 6, alignItems: 'center' },
  performance: { alignSelf: 'stretch', gap: 6, marginTop: 'auto', paddingTop: 8, borderTopWidth: 1, borderTopColor: colors.headerBorder },
  performanceCaption: { color: colors.headerMuted, fontSize: 11, fontWeight: '700', letterSpacing: 1.4 },
  performanceRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  performanceHead: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'baseline' },
  performanceLabel: { width: 42, color: colors.headerMuted, fontSize: 12, fontWeight: '600' },
  performanceCount: { color: colors.headerText, fontSize: 12, fontWeight: '700', fontVariant: ['tabular-nums'] },
  performanceValue: { width: 38, textAlign: 'right', color: colors.headerText, fontSize: 13, fontWeight: '700', fontVariant: ['tabular-nums'] },
  performanceTrack: { flex: 1, height: 8, borderRadius: 4, backgroundColor: colors.headerBorder, overflow: 'hidden' },
  performanceFill: { height: 8, borderRadius: 4 },
  facts: {
    flexDirection: 'row',
    alignSelf: 'stretch',
    marginTop: 8,
    paddingTop: 6,
    borderTopWidth: 1,
    borderTopColor: colors.headerBorder,
  },
  fact: { flex: 1, alignItems: 'center', gap: 0 },
  factDivider: { width: 1, backgroundColor: colors.headerBorder },
  factLabel: { color: colors.headerMuted, fontSize: 10, fontWeight: '500' },
  factValue: { color: colors.headerText, fontSize: 14, fontWeight: '700' },
  signedOut: { color: colors.headerText, fontSize: 17, fontWeight: '700', marginTop: 12 },
  signedOutHint: { color: colors.headerMuted, fontSize: 13, lineHeight: 19, textAlign: 'center' },
});
