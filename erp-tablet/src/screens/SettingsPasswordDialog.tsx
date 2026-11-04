import { useEffect, useRef, useState } from 'react';
import type { ComponentRef } from 'react';
import { StyleSheet, Text, TextInput } from 'react-native';
import { SETTINGS_PASSWORD } from '../device/pairing';
import { Banner, BigButton } from '../ui/controls';
import { DialogFrame } from '../ui/Dialog';
import { colors, radius, TOUCH_HEIGHT_COMPACT } from '../ui/theme';

interface SettingsPasswordDialogProps {
  onUnlock: (password: string) => void;
  /** Dönülecek bir ekran varsa; yoksa pencere kapatılamaz. */
  onCancel?: () => void;
}

/**
 * Sistem ayarlarının girişi: makine eşleştirmesi ve proje seçimi yönetici işlemidir. Açık ekranın üzerinde
 * pencere olarak açılır; vazgeçilince istasyon kaldığı yerden devam eder.
 */
export function SettingsPasswordDialog({ onUnlock, onCancel }: SettingsPasswordDialogProps) {
  const [password, setPassword] = useState('');
  const [wrong, setWrong] = useState(false);
  const input = useRef<ComponentRef<typeof TextInput>>(null);

  // Pencere açılırken klavyeyi kapatır (DialogFrame); parola alanı ondan sonra odaklanır, sayısal klavye açılır.
  useEffect(() => {
    input.current?.focus();
  }, []);

  const submit = () => {
    if (password.length === 0) {
      return;
    }

    if (password === SETTINGS_PASSWORD) {
      onUnlock(password);
    } else {
      setWrong(true);
      setPassword('');
    }
  };

  return (
    <DialogFrame
      title="Sistem ayarları"
      onClose={onCancel}
      testID="settings-password"
      footer={
        <>
          {onCancel && (
            <BigButton label="Vazgeç" tone="neutral" size="compact" onPress={onCancel} testID="settings-password-cancel" />
          )}
          <BigButton label="Devam" size="compact" onPress={submit} disabled={password.length === 0} testID="settings-password-submit" />
        </>
      }>
      <Text style={styles.fieldLabel}>PAROLA</Text>
      <TextInput
        ref={input}
        testID="settings-password-input"
        style={[styles.input, wrong && styles.inputError]}
        value={password}
        onChangeText={value => {
          setPassword(value);
          setWrong(false);
        }}
        onSubmitEditing={submit}
        secureTextEntry
        keyboardType="number-pad"
        placeholder="Yönetici parolası"
        placeholderTextColor={colors.muted}
      />

      {wrong && <Banner kind="error" text="Parola hatalı." testID="settings-password-error" />}
    </DialogFrame>
  );
}

const styles = StyleSheet.create({
  fieldLabel: { color: colors.muted, fontSize: 11, fontWeight: '700', letterSpacing: 1.1 },
  input: {
    height: TOUCH_HEIGHT_COMPACT + 4,
    borderWidth: 1,
    borderColor: colors.borderStrong,
    borderRadius: radius.control,
    backgroundColor: colors.surface,
    color: colors.text,
    fontSize: 16,
    letterSpacing: 2,
    paddingHorizontal: 12,
  },
  inputError: { borderColor: colors.danger },
});
