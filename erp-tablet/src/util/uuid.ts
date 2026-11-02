/**
 * Rastgele UUID (v4 biçiminde). İstek / okuma kimliği olarak kullanılır: amaç aynı isteğin tekrarını
 * sunucunun tanıyabilmesidir, gizlilik değil; bu yüzden kriptografik rastgelelik gerekmez.
 */
export function newUuid(random: () => number = Math.random): string {
  const hex = (count: number) =>
    Array.from({ length: count }, () => Math.floor(random() * 16).toString(16)).join('');

  const variant = (8 + Math.floor(random() * 4)).toString(16);
  return `${hex(8)}-${hex(4)}-4${hex(3)}-${variant}${hex(3)}-${hex(12)}`;
}
