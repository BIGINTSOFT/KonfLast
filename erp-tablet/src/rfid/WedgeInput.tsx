import { useEffect, useRef } from 'react';
import { DeviceEventEmitter, Keyboard, TextInput } from 'react-native';
import type { KeyboardWedgeReader } from './KeyboardWedgeReader';

/** Android tarafının (MainActivity.dispatchKeyEvent) Enter'a kadar toplayıp gönderdiği satır. */
const WEDGE_LINE_EVENT = 'KeyboardWedgeLine';

interface WedgeInputProps {
  reader: KeyboardWedgeReader;
  /** Ekranda başka bir metin alanı açıkken okumalar yok sayılır. */
  paused?: boolean;
}

/**
 * Klavye emülasyonlu okuyucunun yazdığı satırları okuyucuya aktarır. Tuşları Android tarafı toplar (açık metin
 * alanı yoksa); gizli bir metin alanı kullanılmadığı için okutmada ekran klavyesi açılıp kapanmaz. Ekranda bir şey
 * çizmez.
 */
export function WedgeInput({ reader, paused = false }: WedgeInputProps) {
  const pausedNow = useRef(paused);
  pausedNow.current = paused;

  useEffect(() => {
    const subscription = DeviceEventEmitter.addListener(WEDGE_LINE_EVENT, (line: string) => {
      if (!pausedNow.current) {
        reader.feed(line);
      }
    });
    return () => subscription.remove();
  }, [reader]);

  // Ekran klavyesi kapanınca açık alan odağı bırakır: yoksa sonraki kart okuması o alana yazılır.
  useEffect(() => {
    const subscription = Keyboard.addListener('keyboardDidHide', () => {
      if (pausedNow.current) {
        TextInput.State.currentlyFocusedInput()?.blur();
      }
    });
    return () => subscription.remove();
  }, []);

  return null;
}
