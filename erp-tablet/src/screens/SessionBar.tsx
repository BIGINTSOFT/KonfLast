import { StyleSheet, Text, View } from 'react-native';
import type { TabletSession } from '../api/types';
import { BigButton } from '../ui/controls';
import { colors } from '../ui/theme';

interface SessionBarProps {
  session: TabletSession;
  /** Makine arızalıysa kaydedilmiş arıza nedeni. */
  breakdownReason?: string | null;
  busy: boolean;
  onResume: () => void;
}

/** Duraklamanın operatöre gösterilen adı; türler birbirine karıştırılmaz. */
export function interruptionTitle(session: TabletSession, breakdownReason?: string | null): string {
  switch (session.status) {
    case 'BREAK':
      return session.currentPause?.breakName ? `Moladasınız · ${session.currentPause.breakName}` : 'Moladasınız';
    case 'PAUSED': {
      const reason = session.currentPause?.reasonName;
      return reason ? `Ara verdiniz · ${reason}` : 'Ara verdiniz';
    }
    case 'WAITING_FOR_BUNDLE':
      return 'Demet bekleniyor';
    case 'MACHINE_BREAKDOWN':
      return breakdownReason ? `Makine arızalı · ${breakdownReason}` : 'Makine arızalı';
    default:
      return '';
  }
}

/**
 * Duraklama şeridi: mola, ara, demet bekleme ya da arıza sürerken neden ve "Devam et". Çalışırken çizilmez;
 * mola, ara ve demet bekleme işlemleri üst çubuktadır.
 */
export function SessionBar({ session, breakdownReason, busy, onResume }: SessionBarProps) {
  if (session.status === 'ACTIVE') {
    return null;
  }

  return (
    <View style={[styles.bar, styles.interrupted]} accessibilityRole="alert">
      <View style={styles.text}>
        <Text style={styles.caption}>ÇALIŞMA DURAKLATILDI</Text>
        <Text style={styles.title} testID="session-status">
          {interruptionTitle(session, breakdownReason)}
        </Text>
      </View>
      {session.status !== 'MACHINE_BREAKDOWN' && (
        <BigButton label="Devam et" tone="success" size="compact" busy={busy} onPress={onResume} testID="session-resume" />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  bar: {
    flexDirection: 'row',
    alignItems: 'center',
    flexWrap: 'wrap',
    backgroundColor: colors.surface,
    borderBottomWidth: 1,
    borderBottomColor: colors.border,
    paddingVertical: 10,
    paddingHorizontal: 20,
    gap: 10,
  },
  interrupted: { borderLeftColor: colors.warning, borderLeftWidth: 4, backgroundColor: colors.warningSoft },
  text: { flex: 1, minWidth: 200, gap: 2 },
  caption: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.2 },
  title: { color: colors.text, fontSize: 16, fontWeight: '700' },
});
