import type { ReactNode } from 'react';
import { Fragment, useEffect, useRef } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { Avatar } from './Avatar';
import { StatusPill } from './controls';
import { colors, radius } from './theme';

const SIDEBAR_WIDTH = 288;
const AVATAR_SIZE = 76;
const SETTINGS_HOLD_MS = 1500;
const BRAND_HIT_SLOP = { top: 12, left: 12, right: 12, bottom: 4 };
const BRAND_RETENTION = { top: 40, left: 40, right: 40, bottom: 40 };

interface StationSidebarFrameProps {
  /** Tabletin açık olan modülü; markanın altında yazar. */
  moduleName: string;
  /** Makine kodu ya da (makine yoksa) tablet kodu. */
  title: string;
  subtitle?: string;
  meta: string;
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
  moduleName, title, subtitle, meta, status, statusReason, onOpenSetup, personnelCaption = 'PERSONEL', children, footer,
}: StationSidebarFrameProps) {
  return (
    <View style={styles.sidebar}>
      <BrandHold moduleName={moduleName} onOpenSetup={onOpenSetup} />

      <View style={styles.section}>
        <Text style={styles.caption}>İSTASYON</Text>
        <Text style={styles.machineCode} testID="station-title" numberOfLines={1}>
          {title}
        </Text>
        {subtitle && (
          <Text style={styles.operation} testID="station-operation" numberOfLines={2}>
            {subtitle}
          </Text>
        )}
        <Text style={styles.stationMeta} numberOfLines={1}>
          {meta}
        </Text>
        {status && <StatusPill label={status.label} color={status.color} onDark testID="station-machine-status" />}
        {statusReason && (
          <Text style={styles.statusReason} testID="station-machine-reason" numberOfLines={2}>
            {statusReason}
          </Text>
        )}
      </View>

      {/* Personel alanı kalan yüksekliği alır; alttaki düğme küçük ekranlarda da görünür kalır. */}
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
}

export function SidebarOperator({ name, role, code, status, photoUri, facts }: SidebarOperatorProps) {
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
                <Text style={styles.factLabel}>{fact.label}</Text>
                <Text style={styles.factValue} testID={fact.testID}>
                  {fact.value}
                </Text>
              </View>
            </Fragment>
          ))}
        </View>
      )}
    </View>
  );
}

/** Arızalı durumun rengi; istasyon kartındaki neden yazısı da bu renktedir. */
const BREAKDOWN_COLOR = '#ff8a80';

const styles = StyleSheet.create({
  sidebar: { width: SIDEBAR_WIDTH, backgroundColor: colors.header, paddingHorizontal: 18, paddingVertical: 16, gap: 14 },
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
  caption: { color: colors.headerMuted, fontSize: 11, fontWeight: '700', letterSpacing: 1.4, marginBottom: 2 },
  machineCode: { color: colors.headerText, fontSize: 22, fontWeight: '700' },
  operation: { color: colors.headerText, fontSize: 15, fontWeight: '600' },
  stationMeta: { color: colors.headerMuted, fontSize: 12, marginBottom: 8 },
  statusReason: { color: BREAKDOWN_COLOR, fontSize: 13, fontWeight: '600', marginTop: 6 },
  operator: { alignItems: 'center', gap: 2, paddingTop: 8 },
  operatorName: { color: colors.headerText, fontSize: 18, fontWeight: '700', textAlign: 'center', marginTop: 10 },
  operatorRole: { color: colors.headerText, fontSize: 13, fontWeight: '600' },
  operatorCode: { color: colors.headerMuted, fontSize: 13 },
  operatorStatus: { marginTop: 8, alignItems: 'center' },
  facts: {
    flexDirection: 'row',
    alignSelf: 'stretch',
    marginTop: 14,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: colors.headerBorder,
  },
  fact: { flex: 1, alignItems: 'center', gap: 2 },
  factDivider: { width: 1, backgroundColor: colors.headerBorder },
  factLabel: { color: colors.headerMuted, fontSize: 11, fontWeight: '500' },
  factValue: { color: colors.headerText, fontSize: 16, fontWeight: '700' },
  signedOut: { color: colors.headerText, fontSize: 17, fontWeight: '700', marginTop: 12 },
  signedOutHint: { color: colors.headerMuted, fontSize: 13, lineHeight: 19, textAlign: 'center' },
});
