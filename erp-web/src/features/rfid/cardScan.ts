import type { ChangeEvent, FocusEvent, KeyboardEvent } from 'react'

/** Okuyucunun kart çekilince yazdığı satır; kart numarası değildir. */
const NO_CARD = 'NOCARD'

/** Değerin içinden okuyucunun "NOCARD" satırını atar (yapıştırma gibi tuş dışı girişler için). */
export function withoutNoCard(value: string): string {
  return value.replace(/NOCARD/gi, '')
}

interface CardScanOptions {
  /** Enter formu gönderir (okutunca hemen işlem yapan alanlar: kart ata, kart bul). Varsayılan: göndermez. */
  submitOnEnter?: boolean
}

/**
 * Kart / barkod okutma alanının olayları. Masaüstü okuyucu klavye gibi numarayı yazıp Enter gönderir; kart
 * çekilince de "NOCARD" + Enter yazar.
 *
 * - Alana girilince ya da Enter'dan sonra alan "yeniden okutma" kipindedir: yazılan ilk karakter eski değerin
 *   yerine geçer, sonraki okutma eskisinin sonuna eklenmez.
 * - Bu kipte "NOCARD"ın harfleri alana yazılmadan bekletilir; satır NOCARD çıkarsa yutulur, başka bir şey
 *   çıkarsa bekletilen harfler yazılır. Böylece 10 karakter sınırında yarım "NO" kalmaz.
 * - Ok, silme gibi düzenleme tuşları kipten çıkarır; numara elle düzeltilebilir.
 */
export function cardScanHandlers(setValue: (value: string) => void, { submitOnEnter = false }: CardScanOptions = {}) {
  return {
    onChange: (event: ChangeEvent<HTMLInputElement>) => setValue(withoutNoCard(event.target.value)),
    onFocus: (event: FocusEvent<HTMLInputElement>) => {
      event.target.dataset.rescan = 'true'
      event.target.dataset.pending = ''
      event.target.select()
    },
    onKeyDown: (event: KeyboardEvent<HTMLInputElement>) => {
      const data = event.currentTarget.dataset
      if (event.key === 'Enter') {
        if (!submitOnEnter) {
          event.preventDefault()
        }
        data.rescan = 'true'
        data.pending = ''
        return
      }

      if (event.ctrlKey || event.metaKey || event.altKey || event.key === 'Shift' || event.key === 'Tab') {
        return
      }

      if (event.key.length !== 1) {
        data.rescan = ''
        data.pending = ''
        return
      }

      if (data.rescan !== 'true') {
        return
      }

      event.preventDefault()
      const pending = (data.pending ?? '') + event.key
      if (NO_CARD.startsWith(pending.toUpperCase())) {
        data.pending = pending.length === NO_CARD.length ? '' : pending
        return
      }

      data.rescan = ''
      data.pending = ''
      setValue(pending)
    },
  }
}
