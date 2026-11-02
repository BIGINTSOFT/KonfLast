import { useEffect, useState } from 'react'

/** Değer `delayMs` boyunca değişmeden kalınca güncellenir (yazarken her tuşta istek atmamak için). */
export function useDebounced<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
