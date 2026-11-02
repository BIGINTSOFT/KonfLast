import { useState } from 'react';
import { StyleSheet, Text, TextInput, View } from 'react-native';
import { SETTINGS_PASSWORD } from '../device/pairing';
import { Banner, BigButton } from '../ui/controls';
import { colors, radius, TOUCH_HEIGHT } from '../ui/theme';

interface SettingsPasswordScreenProps {
  onUnlock: (password: string) => void;
  /** Dönülecek bir ekran varsa. */
  onCancel?: () => void;
}

/** Sistem ayarlarının girişi: makine eşleştirmesi ve proje seçimi yönetici işlemidir. */
export function SettingsPasswordScreen({ onUnlock, onCancel }: SettingsPasswordScreenProps) {
  const [password, setPassword] = useState('');
  const [wrong, setWrong] = useState(false);

  const submit = () => {
    if (password === SETTINGS_PASSWORD) {
      onUnlock(password);
    } else {
      setWrong(true);
      setPassword('');
    }
  };

  return (
    <View style={styles.screen} testID="settings-password">
      <View style={styles.card}>
        <Text style={styles.title}>Sistem ayarları</Text>
        <Text style={styles.help}>Devam etmek için parolayı girin.</Text>

        <TextInput
          testID="settings-password-input"
          style={styles.input}
          value={password}
          onChangeText={value => {
            setPassword(value);
            setWrong(false);
          }}
          onSubmitEditing={submit}
          secureTextEntry
          autoFocus
          keyboardType="number-pad"
          placeholder="Parola"
          placeholderTextColor={colors.muted}
        />

        {wrong && <Banner kind="error" text="Parola hatalı." testID="settings-password-error" />}

        <View style={styles.actions}>
          {onCancel && <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID="settings-password-cancel" />}
          <BigButton label="Devam" size="compact" onPress={submit} disabled={password.length === 0} testID="settings-password-submit" />
        </View>
      </View>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, alignItems: 'center', paddingTop: 48, backgroundColor: colors.background },
  card: {
    width: 400,
    gap: 12,
    padding: 24,
    backgroundColor: colors.surface,
    borderWidth: 1,
    borderColor: colors.border,
    borderRadius: radius.card,
  },
  title: { color: colors.text, fontSize: 20, fontWeight: '700' },
  help: { color: colors.muted, fontSize: 14 },
  input: {
    minHeight: TOUCH_HEIGHT,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
    color: colors.text,
    fontSize: 16,
    paddingHorizontal: 12,
  },
  actions: { flexDirection: 'row', justifyContent: 'flex-end', gap: 8 },
});
