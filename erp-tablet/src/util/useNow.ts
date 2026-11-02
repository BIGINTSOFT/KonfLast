import { useEffect, useState } from 'react';

/** Düzenli yenilenen "şimdi": ekrandaki saat ve geçen süre göstergeleri için. */
export function useNow(intervalMs = 15_000): Date {
  const [now, setNow] = useState(() => new Date());

  useEffect(() => {
    const timer = setInterval(() => setNow(new Date()), intervalMs);
    return () => clearInterval(timer);
  }, [intervalMs]);

  return now;
}
