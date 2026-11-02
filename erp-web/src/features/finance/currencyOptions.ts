import { useGetLookupQuery } from '../masterData/masterDataApi'

export const LOCAL_CURRENCY = 'TRY'

export interface CurrencyOption {
  value: string
  label: string
}

/**
 * Tanımlı aktif para birimleri (Para Birimleri ekranından). Kayıtta seçili olan kod listede yoksa (pasif yapılmış
 * ya da tanımdan önce girilmiş) seçim kaybolmasın diye listeye eklenir.
 */
export function useCurrencyOptions(current?: string | null): CurrencyOption[] {
  const { data } = useGetLookupQuery({ resource: 'currencies', take: 100 })
  const options = (data ?? []).map((item) => ({ value: item.code, label: `${item.code} — ${item.name}` }))
  if (options.length === 0) {
    options.push({ value: LOCAL_CURRENCY, label: LOCAL_CURRENCY })
  }
  if (current && !options.some((option) => option.value === current)) {
    options.push({ value: current, label: current })
  }
  return options
}
