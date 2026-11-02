import { StyleSheet, Text, View } from 'react-native';
import type { PauseType, TabletSession } from '../api/types';
import { BigButton } from '../ui/controls';
import { colors } from '../ui/theme';

interface SessionBarProps {
  session: TabletSession;
  /** Makine arızalıysa kaydedilmiş arıza nedeni. */
  breakdownReason?: string | null;
  /** Operatörün elinde süren iş var mı (demet bekleme bildirilemez). */
  hasJob: boolean;
  busy: boolean;
  /** Seçilebilecek ara nedeni var mı (yoksa "Ara ver" gösterilmez). */
  canPause: boolean;
  /** "Ara ver": neden seçimi penceresini açan taraf gösterir. */
  onRequestPause: () => void;
  onPause: (type: PauseType, reasonId: number | null) => void;
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
 * Operatörün çalışma durumu ve duraklama işlemleri: planlı mola (zamanı geldiyse), ara, demet bekleme
 * ve çalışmaya dönüş. Neyin ne zaman yapılabileceğine sunucu karar verir; burada yalnızca gösterilir.
 */
export function SessionBar({
  session,
  breakdownReason,
  hasJob,
  busy,
  canPause,
  onRequestPause,
  onPause,
  onResume,
}: SessionBarProps) {
  if (session.status !== 'ACTIVE') {
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

  const dueBreak = session.dueBreak;
  return (
    <View style={styles.bar}>
      <View style={styles.text}>
        {dueBreak ? (
          <>
            <Text style={styles.caption}>MOLA ZAMANI</Text>
            <Text style={styles.title} testID="session-due-break">
              {dueBreak.name} ({dueBreak.plannedMinutes} dk)
            </Text>
            {!dueBreak.canStartNow && dueBreak.hint && <Text style={styles.hint}>{dueBreak.hint}</Text>}
          </>
        ) : (
          <>
            <Text style={styles.caption}>ÇALIŞMA DURUMU</Text>
            <Text style={styles.title}>Çalışıyor</Text>
          </>
        )}
      </View>
      <View style={styles.actions}>
        {dueBreak && (
          <BigButton
            label="Molaya çık"
            size="compact"
            disabled={busy || !dueBreak.canStartNow}
            onPress={() => onPause('BREAK', null)}
            testID="session-break"
          />
        )}
        {canPause && (
          <BigButton
            label="Ara ver"
            tone="neutral"
            size="compact"
            disabled={busy}
            onPress={onRequestPause}
            testID="session-pause"
          />
        )}
        {!hasJob && (
          <BigButton
            label="Demet bekliyorum"
            tone="neutral"
            size="compact"
            disabled={busy}
            onPress={() => onPause('WAITING_FOR_BUNDLE', null)}
            testID="session-waiting"
          />
        )}
      </View>
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
  hint: { color: colors.muted, fontSize: 13, lineHeight: 19 },
  actions: { flexDirection: 'row', flexWrap: 'wrap', gap: 8 },
});
