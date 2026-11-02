const pad = (value: number) => String(value).padStart(2, '0');

/** Cihazın yerel saatiyle "14:05". */
export function formatClock(date: Date): string {
  return `${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** Cihazın yerel tarihiyle "09.10.2026". */
export function formatDate(date: Date): string {
  return `${pad(date.getDate())}.${pad(date.getMonth() + 1)}.${date.getFullYear()}`;
}

/** Geçen süre: "45 dk", "2 sa 05 dk". Negatif süre (saat farkı) sıfır sayılır. */
export function formatElapsed(milliseconds: number): string {
  const minutes = Math.max(0, Math.floor(milliseconds / 60_000));
  const hours = Math.floor(minutes / 60);
  return hours === 0 ? `${minutes} dk` : `${hours} sa ${pad(minutes % 60)} dk`;
}

/** Sayaç süresi: "06:40", bir saati aşınca "1:02:05". Negatif süre sıfır sayılır. */
export function formatTimer(seconds: number): string {
  const total = Math.max(0, Math.floor(seconds));
  const hours = Math.floor(total / 3600);
  const rest = `${pad(Math.floor((total % 3600) / 60))}:${pad(total % 60)}`;
  return hours === 0 ? rest : `${hours}:${rest}`;
}

/** Ad soyadın baş harfleri (en fazla iki): "Utku Can Uzun" → "UU". */
export function initialsOf(fullName: string): string {
  const words = fullName.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) {
    return '';
  }

  const letters = words.length === 1 ? [words[0][0]] : [words[0][0], words[words.length - 1][0]];
  return letters.join('').toLocaleUpperCase('tr-TR');
}
