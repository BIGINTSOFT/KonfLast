/**
 * Okuyucu aynı kartı kısa sürede birden çok kez bildirebilir. Bu filtre, aynı UID'nin
 * `windowMs` içindeki tekrarlarını yutar. Süre ileride backend konfigürasyonundan gelecek.
 *
 * Not: Bu yalnızca istemci tarafı korumadır; backend ayrıca idempotency kontrolü yapar.
 */
export function createReadDebouncer(windowMs: number, now: () => number = Date.now) {
  const lastSeen = new Map<string, number>();

  return function shouldAccept(uid: string): boolean {
    const time = now();
    const previous = lastSeen.get(uid);
    lastSeen.set(uid, time);

    // Süresi geçmiş kayıtlar birikmesin.
    for (const [key, seenAt] of lastSeen) {
      if (time - seenAt >= windowMs) {
        lastSeen.delete(key);
      }
    }

    return previous === undefined || time - previous >= windowMs;
  };
}
