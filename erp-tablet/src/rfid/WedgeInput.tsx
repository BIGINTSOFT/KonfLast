import { useEffect, useRef } from 'react';
import type { ComponentRef } from 'react';
import { Keyboard, StyleSheet, TextInput } from 'react-native';
import type { KeyboardWedgeReader } from './KeyboardWedgeReader';

interface WedgeInputProps {
  reader: KeyboardWedgeReader;
  /** Ekranda başka bir metin alanı açıkken odak geri alınmaz. */
  paused?: boolean;
}

/**
 * Klavye emülasyonlu okuyucunun yazdığı UID'yi yakalayan görünmez giriş alanı. Odak hep bu alandadır;
 * ekran klavyesi açılmaz. Okuyucu satırı Enter ile bitirince satır okuyucuya aktarılır.
 */
export function WedgeInput({ reader, paused = false }: WedgeInputProps) {
  const input = useRef<ComponentRef<typeof TextInput>>(null);
  // Bu alan odağı kaybettiğinde diğer alanın onFocus'u henüz çalışmamıştır; karar gecikmeli, son değere göre verilir.
  const pausedNow = useRef(paused);
  pausedNow.current = paused;

  useEffect(() => {
    if (!paused) {
      input.current?.focus();
    }
  }, [paused]);

  // Ekran klavyesi kapanınca açık alan odağı bırakır: yoksa sonraki kart okuması o alana yazılır.
  useEffect(() => {
    const subscription = Keyboard.addListener('keyboardDidHide', () => {
      if (pausedNow.current) {
        TextInput.State.currentlyFocusedInput()?.blur();
      }
    });
    return () => subscription.remove();
  }, []);

  return (
    <TextInput
      ref={input}
      testID="wedge-input"
      style={styles.hidden}
      autoFocus={!paused}
      showSoftInputOnFocus={false}
      caretHidden
      autoCorrect={false}
      autoCapitalize="none"
      blurOnSubmit={false}
      importantForAccessibility="no-hide-descendants"
      accessibilityElementsHidden
      onBlur={() => {
        // Bir düğmeye basılınca odak kaybolur; sıradaki kart okuması için geri alınır.
        setTimeout(() => {
          if (!pausedNow.current) {
            input.current?.focus();
          }
        }, 100);
      }}
      onSubmitEditing={event => {
        reader.feed(event.nativeEvent.text);
        input.current?.clear();
      }}
    />
  );
}

const styles = StyleSheet.create({
  hidden: { position: 'absolute', width: 1, height: 1, opacity: 0 },
});
