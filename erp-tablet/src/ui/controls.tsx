import { ActivityIndicator, Pressable, StyleSheet, Text, View } from 'react-native';
import type { StyleProp, ViewStyle } from 'react-native';
import { colors, radius, TOUCH_HEIGHT, TOUCH_HEIGHT_COMPACT } from './theme';

/**
 * Düğme tonları:
 * - `primary` / `success` / `danger`: dolu, ekrandaki asıl işlem.
 * - `neutral`: çerçeveli ikincil işlem.
 * - `dangerOutline`: çerçeveli, olumsuz ama asıl olmayan işlem (ör. üst çubuktaki "Arıza bildir").
 * - `ghost`: koyu panel üzerinde çerçeveli düğme.
 */
type ButtonTone = 'primary' | 'neutral' | 'danger' | 'dangerOutline' | 'success' | 'ghost';

interface BigButtonProps {
  label: string;
  onPress: () => void;
  tone?: ButtonTone;
  /** `compact`: üst çubuk, panel içi ve pencere düğmeleri. */
  size?: 'large' | 'compact';
  disabled?: boolean;
  busy?: boolean;
  testID?: string;
  style?: StyleProp<ViewStyle>;
}

const tones: Record<ButtonTone, { background: string; border: string; text: string }> = {
  primary: { background: colors.primary, border: colors.primary, text: '#ffffff' },
  success: { background: colors.success, border: colors.success, text: '#ffffff' },
  danger: { background: colors.danger, border: colors.danger, text: '#ffffff' },
  neutral: { background: colors.surface, border: colors.borderStrong, text: colors.text },
  dangerOutline: { background: colors.surface, border: colors.danger, text: colors.danger },
  ghost: { background: 'transparent', border: colors.headerBorder, text: colors.headerText },
};

export function BigButton({ label, onPress, tone = 'primary', size = 'large', disabled, busy, testID, style }: BigButtonProps) {
  const inactive = disabled || busy;
  const { background, border, text } = tones[tone];
  return (
    <Pressable
      testID={testID}
      onPress={onPress}
      disabled={inactive}
      accessibilityRole="button"
      accessibilityState={{ disabled: Boolean(inactive), busy: Boolean(busy) }}
      style={({ pressed }) => [
        styles.button,
        size === 'compact' && styles.buttonCompact,
        { backgroundColor: background, borderColor: border },
        pressed && styles.buttonPressed,
        inactive && styles.buttonDisabled,
        style,
      ]}>
      {busy ? (
        <ActivityIndicator color={text} />
      ) : (
        <Text style={[styles.buttonText, size === 'compact' && styles.buttonTextCompact, { color: text }]}>{label}</Text>
      )}
    </Pressable>
  );
}

const messageStyle = {
  error: { icon: '!', color: colors.danger, soft: colors.dangerSoft },
  info: { icon: 'i', color: colors.info, soft: colors.primarySoft },
  success: { icon: '✓', color: colors.success, soft: colors.successSoft },
};

interface BannerProps {
  kind: 'error' | 'info' | 'success';
  text: string;
  onDismiss?: () => void;
  testID?: string;
}

/** Durum iletisi; anlam yalnızca renkle değil ikon ve metinle de verilir. */
export function Banner({ kind, text, onDismiss, testID }: BannerProps) {
  const { icon, color, soft } = messageStyle[kind];
  return (
    <View
      style={[styles.banner, { borderColor: color, backgroundColor: soft }]}
      accessibilityRole="alert"
      accessibilityLiveRegion="polite">
      <View style={[styles.bannerBadge, { backgroundColor: color }]} accessibilityElementsHidden>
        <Text style={styles.bannerBadgeText}>{icon}</Text>
      </View>
      <Text style={styles.bannerText} testID={testID}>
        {text}
      </Text>
      {onDismiss && (
        <Pressable onPress={onDismiss} accessibilityRole="button" accessibilityLabel="İletiyi kapat" style={styles.bannerClose}>
          <Text style={[styles.bannerCloseText, { color }]}>Kapat</Text>
        </Pressable>
      )}
    </View>
  );
}

interface StatusPillProps {
  label: string;
  color: string;
  /** Koyu panel üzerinde koyu zemin kullanılır. */
  onDark?: boolean;
  testID?: string;
}

/** Durum etiketi: renkli nokta + metin (anlam yalnızca renge bırakılmaz). */
export function StatusPill({ label, color, onDark, testID }: StatusPillProps) {
  return (
    <View style={[styles.pill, onDark ? styles.pillOnDark : styles.pillOnLight]}>
      <View style={[styles.pillDot, { backgroundColor: color }]} />
      <Text style={[styles.pillText, onDark && styles.pillTextOnDark]} testID={testID}>
        {label}
      </Text>
    </View>
  );
}

interface StepperProps {
  label: string;
  value: number;
  max: number;
  onChange: (value: number) => void;
  testID?: string;
}

/** Klavyesiz adet girişi: büyük − / + düğmeleri (uzun adımlar için ±10). */
export function QuantityStepper({ label, value, max, onChange, testID }: StepperProps) {
  const set = (next: number) => onChange(Math.max(0, Math.min(max, next)));
  const step = (delta: number, text: string) => (
    <Pressable
      onPress={() => set(value + delta)}
      disabled={delta < 0 ? value === 0 : value >= max}
      accessibilityRole="button"
      accessibilityLabel={`${label} ${delta > 0 ? 'artır' : 'azalt'} ${Math.abs(delta)}`}
      testID={testID ? `${testID}-${delta > 0 ? 'plus' : 'minus'}${Math.abs(delta)}` : undefined}
      style={({ pressed }) => [styles.stepButton, pressed && styles.stepButtonPressed]}>
      <Text style={styles.stepButtonText}>{text}</Text>
    </Pressable>
  );

  return (
    <View style={styles.stepper}>
      <Text style={styles.stepperLabel}>{label}</Text>
      <View style={styles.stepperRow}>
        {step(-10, '−10')}
        {step(-1, '−')}
        <Text style={styles.stepperValue} testID={testID} accessibilityLabel={`${label}: ${value}`}>
          {value}
        </Text>
        {step(1, '+')}
        {step(10, '+10')}
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  button: {
    minHeight: TOUCH_HEIGHT,
    minWidth: 150,
    alignItems: 'center',
    justifyContent: 'center',
    borderWidth: 1,
    borderRadius: radius.control,
    paddingHorizontal: 22,
  },
  buttonCompact: { minHeight: TOUCH_HEIGHT_COMPACT, minWidth: 0, paddingHorizontal: 16 },
  buttonPressed: { opacity: 0.8 },
  buttonDisabled: { opacity: 0.45 },
  buttonText: { fontSize: 16, fontWeight: '600', letterSpacing: 0.2 },
  buttonTextCompact: { fontSize: 14 },
  banner: {
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1,
    borderLeftWidth: 4,
    borderRadius: radius.control,
    paddingVertical: 6,
    paddingHorizontal: 14,
    gap: 12,
  },
  bannerBadge: { width: 22, height: 22, borderRadius: 11, alignItems: 'center', justifyContent: 'center' },
  bannerBadgeText: { color: '#ffffff', fontSize: 14, fontWeight: '700' },
  bannerText: { flex: 1, color: colors.text, fontSize: 15, lineHeight: 21 },
  bannerClose: { minHeight: 38, minWidth: 60, alignItems: 'center', justifyContent: 'center' },
  bannerCloseText: { fontSize: 14, fontWeight: '600' },
  pill: {
    flexDirection: 'row',
    alignItems: 'center',
    alignSelf: 'flex-start',
    gap: 8,
    borderRadius: radius.pill,
    paddingVertical: 4,
    paddingHorizontal: 11,
  },
  pillOnLight: { backgroundColor: colors.neutralSoft },
  pillOnDark: { backgroundColor: colors.headerSurface, borderWidth: 1, borderColor: colors.headerBorder },
  pillDot: { width: 7, height: 7, borderRadius: 4 },
  pillText: { color: colors.text, fontSize: 13, fontWeight: '600' },
  pillTextOnDark: { color: colors.headerText },
  stepper: { alignItems: 'center', gap: 6 },
  stepperLabel: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  stepperRow: { flexDirection: 'row', alignItems: 'center', gap: 8 },
  stepperValue: { minWidth: 90, textAlign: 'center', color: colors.text, fontSize: 38, fontWeight: '700' },
  stepButton: {
    minHeight: TOUCH_HEIGHT,
    minWidth: 62,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: radius.control,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    backgroundColor: colors.surface,
  },
  stepButtonPressed: { backgroundColor: colors.neutralSoft },
  stepButtonText: { color: colors.text, fontSize: 19, fontWeight: '600' },
});
