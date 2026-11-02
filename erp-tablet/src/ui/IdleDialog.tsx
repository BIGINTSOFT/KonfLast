import { useEffect, useState } from 'react';
import { StyleSheet, Text } from 'react-native';
import { BigButton } from './controls';
import { DialogFrame } from './Dialog';
import { colors } from './theme';

/**
 * Boşta sorusu (Konfeksiyon ile aynı): operatörün elinde iş yokken süre dolunca sorulur. "Kapat" sayacı baştan başlatır.
 * `limitMinutes` 0 ise sorulmaz.
 */
export function useIdlePrompt(idle: boolean, limitMinutes: number, now: Date) {
  const [since, setSince] = useState<number | null>(null);

  useEffect(() => {
    setSince(idle ? Date.now() : null);
  }, [idle]);

  const minutes = since === null ? 0 : Math.floor((now.getTime() - since) / 60_000);
  return {
    minutes,
    due: limitMinutes > 0 && since !== null && minutes >= limitMinutes,
    acknowledge: () => setSince(Date.now()),
  };
}

interface IdleDialogProps {
  minutes: number;
  /** Ne bekleniyor ("Parti bekliyorum", "Levent bekliyorum"). */
  waitingLabel: string;
  description: string;
  help: string;
  canPause: boolean;
  busy: boolean;
  onDismiss: () => void;
  onPause: () => void;
  onWait: () => void;
}

export function IdleDialog({ minutes, waitingLabel, description, help, canPause, busy, onDismiss, onPause, onWait }: IdleDialogProps) {
  return (
    <DialogFrame
      title="Süren iş yok"
      description={`${minutes} dakikadır ${description}`}
      onClose={onDismiss}
      testID="idle-dialog"
      footer={
        <>
          <BigButton label="Kapat" tone="neutral" size="compact" onPress={onDismiss} testID="idle-dismiss" />
          {canPause && (
            <BigButton
              label="Ara ver"
              tone="neutral"
              size="compact"
              onPress={() => {
                onDismiss();
                onPause();
              }}
              testID="idle-pause"
            />
          )}
          <BigButton label={waitingLabel} size="compact" busy={busy} onPress={onWait} testID="idle-waiting" />
        </>
      }>
      <Text style={styles.text}>{help}</Text>
    </DialogFrame>
  );
}

const styles = StyleSheet.create({
  text: { color: colors.textSecondary, fontSize: 15, lineHeight: 22 },
});
