import { useEffect, useState } from 'react';
import { Keyboard } from 'react-native';

/**
 * Açık ekran klavyesinin yüksekliği (kapalıyken 0). Android 15'te uygulama kenardan kenara çizildiği için klavye
 * açılınca pencere küçülmez; klavyenin kapattığı alan bununla boş bırakılır.
 */
export function useKeyboardHeight(): number {
  const [height, setHeight] = useState(0);

  useEffect(() => {
    const show = Keyboard.addListener('keyboardDidShow', event => setHeight(event.endCoordinates.height));
    const hide = Keyboard.addListener('keyboardDidHide', () => setHeight(0));
    return () => {
      show.remove();
      hide.remove();
    };
  }, []);

  return height;
}
