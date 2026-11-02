/**
 * Code 128 (B kümesi) barkod üretimi. Demet numarası gibi kısa, ASCII metinler için; ek kütüphane gerektirmez.
 * Her simge 11 modüldür (bitiş simgesi 13): desen, sırayla çubuk ve boşluk genişlikleridir.
 */
const PATTERNS = [
  '212222', '222122', '222221', '121223', '121322', '131222', '122213', '122312', '132212', '221213',
  '221312', '231212', '112232', '122132', '122231', '113222', '123122', '123221', '223211', '221132',
  '221231', '213212', '223112', '312131', '311222', '321122', '321221', '312212', '322112', '322211',
  '212123', '212321', '232121', '111323', '131123', '131321', '112313', '132113', '132311', '211313',
  '231113', '231311', '112133', '112331', '132131', '113123', '113321', '133121', '313121', '211331',
  '231131', '213113', '213311', '213131', '311123', '311321', '331121', '312113', '312311', '332111',
  '314111', '221411', '431111', '111224', '111422', '121124', '121421', '141122', '141221', '112214',
  '112412', '122114', '122411', '142112', '142211', '241211', '221114', '413111', '241112', '134111',
  '111242', '121142', '121241', '114212', '124112', '124211', '411212', '421112', '421211', '212141',
  '214121', '412121', '111143', '111341', '131141', '114113', '114311', '411113', '411311', '113141',
  '114131', '311141', '411131', '211412', '211214', '211232', '2331112',
] as const

const START_B = 104
const STOP = 106
const QUIET_ZONE = 10

/** Metnin B kümesindeki simge değerleri; kodlanamayan karakter varsa null. */
function code128Values(text: string): number[] | null {
  const values: number[] = []
  for (const character of text) {
    const code = character.charCodeAt(0)
    if (character.length !== 1 || code < 32 || code > 126) {
      return null
    }
    values.push(code - 32)
  }
  return values.length === 0 ? null : values
}

/** Kontrol simgesi: (başlangıç + Σ değer × konum) mod 103. */
function code128Checksum(values: number[]): number {
  return values.reduce((sum, value, index) => sum + value * (index + 1), START_B) % 103
}

export interface BarcodeBar {
  x: number
  width: number
}

export interface Barcode {
  /** Sessiz bölgeler dahil toplam genişlik (modül). */
  width: number
  bars: BarcodeBar[]
}

/** Metni çubuklara çevirir (modül biriminde). Kodlanamayan metinde null döner. */
export function code128(text: string): Barcode | null {
  const values = code128Values(text)
  if (!values) {
    return null
  }

  const symbols = [START_B, ...values, code128Checksum(values), STOP]
  const bars: BarcodeBar[] = []
  let x = QUIET_ZONE
  for (const symbol of symbols) {
    PATTERNS[symbol].split('').forEach((digit, index) => {
      const width = Number(digit)
      // Desen çubukla başlar; çift konumlar çubuk, tek konumlar boşluktur.
      if (index % 2 === 0) {
        bars.push({ x, width })
      }
      x += width
    })
  }

  return { width: x + QUIET_ZONE, bars }
}
