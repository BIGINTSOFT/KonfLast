import { useCallback, useState } from 'react'

export type Theme = 'light' | 'dark'

/** index.html'deki açılış betiği de aynı anahtarı okur (sayfa yanlış temayla bir an bile görünmesin diye). */
export const THEME_STORAGE_KEY = 'erp-theme'

function currentTheme(): Theme {
  return document.documentElement.dataset.theme === 'dark' ? 'dark' : 'light'
}

/** Etkin tema ve değiştirme işlevi. Seçim bu tarayıcıda hatırlanır. */
export function useTheme(): [Theme, () => void] {
  const [theme, setTheme] = useState<Theme>(currentTheme)

  const toggle = useCallback(() => {
    const next: Theme = currentTheme() === 'dark' ? 'light' : 'dark'
    document.documentElement.dataset.theme = next
    try {
      localStorage.setItem(THEME_STORAGE_KEY, next)
    } catch {
      // Depolama kapalıysa (gizli pencere vb.) tema yalnızca bu oturumda geçerli olur.
    }
    setTheme(next)
  }, [])

  return [theme, toggle]
}
