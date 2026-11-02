import { Download, History, Save } from 'lucide-react'
import { useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, todayIsoDate } from '../../utils/format'
import { parseDecimal } from '../../utils/money'
import {
  useFetchExchangeRatesMutation,
  useGetExchangeRateHistoryQuery,
  useGetExchangeRatesQuery,
  useSaveExchangeRatesMutation,
} from './financeApi'
import type { ExchangeRateRow, FetchExchangeRatesResult } from './financeApi'

const rateFormat = new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 4, maximumFractionDigits: 6 })
const formatRate = (value: number | null) => (value === null ? '' : rateFormat.format(value))

type RateField = 'forexBuying' | 'forexSelling' | 'banknoteBuying' | 'banknoteSelling'

const rateFields: { key: RateField; label: string }[] = [
  { key: 'forexBuying', label: 'Döviz alış' },
  { key: 'forexSelling', label: 'Döviz satış' },
  { key: 'banknoteBuying', label: 'Efektif alış' },
  { key: 'banknoteSelling', label: 'Efektif satış' },
]

type Draft = Record<number, Partial<Record<RateField, string>>>

const shiftDate = (iso: string, days: number) => {
  const date = new Date(`${iso}T00:00:00`)
  date.setDate(date.getDate() + days)
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function HistoryDialog({ row, date, onClose }: { row: ExchangeRateRow; date: string; onClose: () => void }) {
  const { data, isLoading, error } = useGetExchangeRateHistoryQuery({ currencyId: row.currencyId, from: shiftDate(date, -90), to: date })
  return (
    <Modal title={`${row.code} · son 90 gün`} onClose={onClose} variant="drawer">
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <div className="table-wrap">
            <table className="table table--compact">
              <thead>
                <tr>
                  <th>Tarih</th>
                  {rateFields.map((field) => <th key={field.key} className="table__number">{field.label}</th>)}
                  <th>Kaynak</th>
                </tr>
              </thead>
              <tbody>
                {data.map((item) => (
                  <tr key={item.id}>
                    <td>{formatDate(item.rateDate)}</td>
                    {rateFields.map((field) => <td key={field.key} className="table__number">{formatRate(item[field.key]) || '—'}</td>)}
                    <td>{item.source === 'TCMB' ? 'TCMB' : 'Elle'}</td>
                  </tr>
                ))}
                {data.length === 0 && (
                  <tr><td colSpan={6} className="table__empty">Bu aralıkta kur yok.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}

function fetchSummary(result: FetchExchangeRatesResult): string {
  if (result.bulletinDate === null) {
    return 'Seçilen gün için TCMB bülteni yok (hafta sonu ya da tatil). Önceki günün kuru kullanılır.'
  }
  const parts = [`${formatDate(result.bulletinDate)} bülteninden ${result.imported} döviz kuru alındı.`]
  if (result.skippedManual.length > 0) {
    parts.push(`Elle girildiği için korunanlar: ${result.skippedManual.join(', ')}.`)
  }
  if (result.notListed.length > 0) {
    parts.push(`TCMB listesinde olmayanlar: ${result.notListed.join(', ')} (elle girin).`)
  }
  return parts.join(' ')
}

/**
 * Döviz kurları: aktif dövizlerin seçilen gündeki kuru (1 birim döviz = X TRY). Kur yoksa önceki son kur
 * gösterilir. Yönetim yetkisiyle TCMB'den çekilir ya da hücrelerde elle düzeltilir.
 */
export function ExchangeRatesPage() {
  const canManage = usePermission(Permissions.ExchangeRateManage)
  const [date, setDate] = useState(todayIsoDate())
  const { data, isLoading, isFetching, error } = useGetExchangeRatesQuery(date)
  const [save, saving] = useSaveExchangeRatesMutation()
  const [fetchRates, fetching] = useFetchExchangeRatesMutation()
  const [draft, setDraft] = useState<Draft>({})
  const [message, setMessage] = useState<string | null>(null)
  const [history, setHistory] = useState<ExchangeRateRow | null>(null)
  const [skipped, setSkipped] = useState(false)

  const changeDate = (next: string) => {
    setDate(next)
    setDraft({})
    setMessage(null)
    setSkipped(false)
  }

  const valueOf = (row: ExchangeRateRow, field: RateField) =>
    draft[row.currencyId]?.[field] ?? (row.isCarriedOver ? '' : formatRate(row[field]))

  const edit = (row: ExchangeRateRow, field: RateField, value: string) =>
    setDraft((current) => ({ ...current, [row.currencyId]: { ...current[row.currencyId], [field]: value } }))

  // "41,8521" ya da "1.234,5" (Türkçe yazım) ve "41.8521" kabul edilir.
  const parse = (text: string) => parseDecimal(text.includes(',') ? text.replace(/\./g, '') : text)

  const handleSave = async () => {
    if (!data) {
      return
    }
    const rates = data.rates
      .filter((row) => draft[row.currencyId])
      .map((row) => ({
        currencyId: row.currencyId,
        // Boş ya da hatalı değer 0 gönderilir; sunucu hangi kuralın bozulduğunu söyler.
        forexBuying: parse(valueOf(row, 'forexBuying')) ?? 0,
        forexSelling: parse(valueOf(row, 'forexSelling')) ?? 0,
        banknoteBuying: parse(valueOf(row, 'banknoteBuying')),
        banknoteSelling: parse(valueOf(row, 'banknoteSelling')),
      }))
    const result = await save({ date, rates })
    if (!('error' in result)) {
      setDraft({})
      setMessage(`${rates.length} döviz kuru kaydedildi.`)
    }
  }

  const handleFetch = async (overwriteManual: boolean) => {
    setMessage(null)
    const result = await fetchRates({ date, overwriteManual })
    if ('data' in result && result.data) {
      setDraft({})
      setMessage(fetchSummary(result.data))
      setSkipped(result.data.skippedManual.length > 0)
    }
  }

  const dirty = Object.keys(draft).length > 0

  return (
    <section className="page">
      <h1 className="visually-hidden">Döviz Kurları</h1>

      <div className="toolbar">
        <label className="rate-toolbar__date">
          <span>Tarih</span>
          <input type="date" value={date} max={todayIsoDate()} onChange={(event) => event.target.value && changeDate(event.target.value)} />
        </label>
        <button type="button" className="button" onClick={() => changeDate(shiftDate(date, -1))}>Önceki gün</button>
        <button type="button" className="button" onClick={() => changeDate(todayIsoDate())} disabled={date === todayIsoDate()}>Bugün</button>
        {canManage && (
          <div className="toolbar__actions">
            <button type="button" className="button" onClick={() => void handleFetch(false)} disabled={fetching.isLoading}>
              <Download aria-hidden="true" />
              {fetching.isLoading ? 'Getiriliyor…' : "TCMB'den getir"}
            </button>
            <button type="button" className="button button--primary" onClick={() => void handleSave()} disabled={!dirty || saving.isLoading}>
              <Save aria-hidden="true" />
              {saving.isLoading ? 'Kaydediliyor…' : 'Kaydet'}
            </button>
          </div>
        )}
      </div>

      {message && (
        <p className="alert alert--info" role="status">
          {message}
          {skipped && canManage && (
            <button type="button" className="button button--ghost" onClick={() => void handleFetch(true)}>
              Elle girilenlerin de üzerine yaz
            </button>
          )}
        </p>
      )}
      <ErrorAlert error={error ?? saving.error ?? fetching.error} fields={['rates', 'date']} />
      {isLoading && <p role="status">Yükleniyor…</p>}

      {data && (
        <div className="table-wrap rate-table" aria-busy={isFetching}>
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Döviz</th>
                <th>Kur tarihi</th>
                {rateFields.map((field) => <th key={field.key} className="table__number">{field.label} ({data.localCurrency})</th>)}
                <th>Kaynak</th>
                <th>Güncelleyen</th>
                <th aria-label="Geçmiş" />
              </tr>
            </thead>
            <tbody>
              {data.rates.map((row) => (
                <tr key={row.currencyId} className={draft[row.currencyId] ? 'rate-table__row--dirty' : undefined}>
                  <td><code>{row.code}</code> {row.name}</td>
                  <td>
                    {row.rateDate ? formatDate(row.rateDate) : '—'}
                    {row.isCarriedOver && <span className="badge badge--neutral rate-table__badge">önceki kur</span>}
                  </td>
                  {rateFields.map((field) => (
                    <td key={field.key} className="table__number">
                      {canManage ? (
                        <input
                          aria-label={`${row.code} ${field.label}`}
                          inputMode="decimal"
                          value={valueOf(row, field.key)}
                          placeholder={row.isCarriedOver ? formatRate(row[field.key]) : undefined}
                          onChange={(event) => edit(row, field.key, event.target.value)}
                        />
                      ) : (
                        formatRate(row[field.key]) || '—'
                      )}
                    </td>
                  ))}
                  <td>
                    {row.source === null ? '—' : row.source === 'TCMB' ? 'TCMB' : <span className="badge badge--info">Elle</span>}
                  </td>
                  <td className="muted">{row.updatedBy ? `${row.updatedBy} · ${formatDateTime(row.updatedAt)}` : '—'}</td>
                  <td className="table__actions">
                    <button type="button" className="icon-button" aria-label={`${row.code} kur geçmişi`} title="Kur geçmişi" onClick={() => setHistory(row)}>
                      <History aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
              {data.rates.length === 0 && (
                <tr><td colSpan={9} className="table__empty">Yerel para dışında aktif para birimi yok. Para Birimleri ekranından ekleyin.</td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}
      <p className="muted form__note">
        Kurlar 1 birim dövizin {data?.localCurrency ?? 'TRY'} karşılığıdır. Seçilen günde kur yoksa önceki son kur kullanılır ve
        &quot;önceki kur&quot; olarak gösterilir. Elle düzeltilen kur, TCMB&apos;den yeniden getirmede korunur.
      </p>

      {history && <HistoryDialog row={history} date={date} onClose={() => setHistory(null)} />}
    </section>
  )
}
