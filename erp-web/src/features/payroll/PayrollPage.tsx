import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { formatMoney, formatRate, parseDecimal } from '../../utils/money'
import { dayRangeToUtc, formatPercent } from '../productionTracking/trackingPresentation'
import { formatDuration } from '../workSessions/workSessionPresentation'
import {
  useGetEarningsQuery,
  useGetOperatorEarningsQuery,
  useGetPayrollSettingsQuery,
  useSavePayrollSettingsMutation,
} from './payrollApi'
import type { OperatorEarnings, PayrollSettings } from './payrollApi'

interface TierDraft {
  key: number
  minEfficiencyPercent: string
  bonusPercent: string
}

function SettingsDialog({ settings, onClose }: { settings: PayrollSettings; onClose: () => void }) {
  const [save, { isLoading, error }] = useSavePayrollSettingsMutation()
  const [minuteRate, setMinuteRate] = useState(String(settings.minuteRate))
  const [currency, setCurrency] = useState(settings.currency)
  const [tiers, setTiers] = useState<TierDraft[]>(() =>
    settings.bonusTiers.map((tier, index) => ({
      key: index,
      minEfficiencyPercent: String(tier.minEfficiencyPercent),
      bonusPercent: String(tier.bonusPercent),
    })),
  )
  const [nextKey, setNextKey] = useState(settings.bonusTiers.length)

  const setTier = (key: number, change: Partial<TierDraft>) =>
    setTiers((current) => current.map((tier) => (tier.key === key ? { ...tier, ...change } : tier)))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      minuteRate: parseDecimal(minuteRate) ?? -1,
      currency,
      // Boş bırakılan satır geçersiz değerle gönderilir; sunucu hangi kuralın bozulduğunu söyler.
      bonusTiers: tiers.map((tier) => ({
        minEfficiencyPercent: parseDecimal(tier.minEfficiencyPercent) ?? 0,
        bonusPercent: parseDecimal(tier.bonusPercent) ?? 0,
      })),
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Ücret ve prim ayarları" onClose={onClose} variant="drawer">
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className="muted">
          Operasyona birim ücret girilmişse o kullanılır; girilmemişse ücret, operasyonun standart süresi (SAM) ile
          dakika ücretinin çarpımıdır. Yeni ücretler yalnızca bundan sonra açılan demetlere uygulanır.
        </p>
        <div className="form__grid">
          <TextField
            label="Dakika ücreti"
            inputMode="decimal"
            value={minuteRate}
            onChange={(event) => setMinuteRate(event.target.value)}
            errors={fieldErrors(error, 'minuteRate')}
            hint="0 girilirse SAM üzerinden ücret hesaplanmaz."
            autoFocus
          />
          <TextField
            label="Para birimi"
            maxLength={3}
            value={currency}
            onChange={(event) => setCurrency(event.target.value.toUpperCase())}
            errors={fieldErrors(error, 'currency')}
            hint="ISO kodu, örn. TRY."
          />
        </div>

        <fieldset className="checkbox-list">
          <legend>Prim kademeleri</legend>
          <p className="muted">
            Operatörün günlük verimi bir kademenin eşiğine ulaşırsa o günün parça başı kazancına kademenin oranında
            prim eklenir. Birden fazla kademeye ulaşılırsa en yükseği geçerlidir.
          </p>
          {tiers.length > 0 && (
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Günlük verim en az (%)</th>
                    <th>Prim (%)</th>
                    <th aria-label="İşlemler" />
                  </tr>
                </thead>
                <tbody>
                  {tiers.map((tier) => (
                    <tr key={tier.key}>
                      <td>
                        <input
                          aria-label="Verim eşiği"
                          inputMode="decimal"
                          value={tier.minEfficiencyPercent}
                          onChange={(event) => setTier(tier.key, { minEfficiencyPercent: event.target.value })}
                        />
                      </td>
                      <td>
                        <input
                          aria-label="Prim oranı"
                          inputMode="decimal"
                          value={tier.bonusPercent}
                          onChange={(event) => setTier(tier.key, { bonusPercent: event.target.value })}
                        />
                      </td>
                      <td className="table__actions">
                        <button
                          type="button"
                          className="button"
                          onClick={() => setTiers((current) => current.filter((item) => item.key !== tier.key))}
                        >
                          Kaldır
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          <p>
            <button
              type="button"
              className="button"
              onClick={() => {
                setTiers((current) => [...current, { key: nextKey, minEfficiencyPercent: '', bonusPercent: '' }])
                setNextKey(nextKey + 1)
              }}
            >
              Kademe ekle
            </button>
          </p>
          {fieldErrors(error, 'bonusTiers')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </fieldset>

        <ErrorAlert error={error} fields={['minuteRate', 'currency', 'bonusTiers']} />
        {settings.updatedAt && (
          <p className="muted">
            Son değişiklik: {settings.updatedBy ?? 'sistem'}, {formatDateTime(settings.updatedAt)}
          </p>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

function DetailDialog({ row, range, onClose }: { row: OperatorEarnings; range: { from: string; to: string }; onClose: () => void }) {
  const { data, isLoading, error } = useGetOperatorEarningsQuery({ operatorId: row.operatorId, ...range })

  return (
    <Modal title={`${row.operatorName} · kazanç dökümü`} onClose={onClose} variant="drawer">
      <div className="form">
        {isLoading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={error} />
        {data && (
          <>
            <h3 className="section-title">Günler</h3>
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Tarih</th>
                    <th className="table__number">Üretilen</th>
                    <th className="table__number">Verim</th>
                    <th className="table__number">Parça başı</th>
                    <th className="table__number">Prim</th>
                    <th className="table__number">Toplam</th>
                  </tr>
                </thead>
                <tbody>
                  {data.days.map((day) => (
                    <tr key={day.day}>
                      <td>{formatDate(day.day)}</td>
                      <td className="table__number">{formatQuantity(day.produced)}</td>
                      <td className="table__number">{formatPercent(day.efficiencyPercent)}</td>
                      <td className="table__number">{formatMoney(day.pieceEarnings, data.currency)}</td>
                      <td className="table__number">
                        {day.bonusPercent > 0 ? `${formatMoney(day.bonusAmount, data.currency)} (%${day.bonusPercent})` : '—'}
                      </td>
                      <td className="table__number"><strong>{formatMoney(day.totalEarnings, data.currency)}</strong></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <h3 className="section-title">Operasyonlar</h3>
            <div className="table-wrap">
              <table className="table table--compact">
                <thead>
                  <tr>
                    <th>Operasyon</th>
                    <th className="table__number">Üretilen</th>
                    <th className="table__number">Birim ücret</th>
                    <th className="table__number">Tutar</th>
                  </tr>
                </thead>
                <tbody>
                  {data.operations.map((operation) => (
                    <tr key={operation.operationId}>
                      <td><code>{operation.operationCode}</code> {operation.operationName}</td>
                      <td className="table__number">{formatQuantity(operation.produced)}</td>
                      <td className="table__number">
                        {operation.unitRate === null ? (
                          <span className="badge badge--warning">Ücret tanımsız</span>
                        ) : (
                          formatRate(operation.unitRate, data.currency)
                        )}
                      </td>
                      <td className="table__number">{formatMoney(operation.amount, data.currency)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </>
        )}
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Kapat</button>
        </footer>
      </div>
    </Modal>
  )
}

function EarningsRow({
  row, currency, total = false, onOpen,
}: { row: OperatorEarnings; currency: string; total?: boolean; onOpen?: () => void }) {
  const Cell = total ? 'th' : 'td'
  return (
    <tr>
      <Cell scope={total ? 'row' : undefined}>
        {total ? 'Toplam' : (
          <button type="button" className="button button--ghost" onClick={onOpen}>
            <code>{row.operatorCode}</code> {row.operatorName}
          </button>
        )}
      </Cell>
      <td className="table__number">{total ? '' : row.daysWorked}</td>
      <td className="table__number">{formatQuantity(row.produced)}</td>
      <td className="table__number">{formatDuration(row.earnedMinutes)}</td>
      <td className="table__number">{formatPercent(row.efficiencyPercent)}</td>
      <td className="table__number">{formatMoney(row.pieceEarnings, currency)}</td>
      <td className="table__number">{formatMoney(row.bonusAmount, currency)}</td>
      <td className="table__number"><strong>{formatMoney(row.totalEarnings, currency)}</strong></td>
    </tr>
  )
}

/** Operatörlerin parça başı kazancı ve verim primi. Tüm tutarları sunucu hesaplar. */
export function PayrollPage() {
  const canManage = usePermission(Permissions.PayrollManage)
  const [fromDate, setFromDate] = useState(() => `${todayIsoDate().slice(0, 8)}01`)
  const [toDate, setToDate] = useState(todayIsoDate)
  const [editing, setEditing] = useState(false)
  const [detail, setDetail] = useState<OperatorEarnings | null>(null)
  const range = dayRangeToUtc(fromDate, toDate)

  const settings = useGetPayrollSettingsQuery()
  const { data, error, isLoading, isFetching } = useGetEarningsQuery(range ?? { from: '', to: '' }, { skip: !range })

  const unpriced = data?.total.unpricedQuantity ?? 0
  const noRates = settings.data !== undefined && settings.data.minuteRate === 0

  return (
    <section className="page">
      <h1 className="visually-hidden">Ücret ve Prim</h1>

      {settings.data && (
        <dl className="status-list status-list--inline">
          <div>
            <dt>Dakika ücreti</dt>
            <dd>{noRates ? 'Tanımlı değil' : formatRate(settings.data.minuteRate, settings.data.currency)}</dd>
          </div>
          <div>
            <dt>Prim kademeleri</dt>
            <dd>
              {settings.data.bonusTiers.length === 0
                ? 'Yok'
                : settings.data.bonusTiers
                    .map((tier) => `%${tier.minEfficiencyPercent} verim → %${tier.bonusPercent}`)
                    .join(' · ')}
            </dd>
          </div>
        </dl>
      )}

      <div className="toolbar toolbar--filters">
        <label>
          Başlangıç
          <input type="date" value={fromDate} max={toDate} onChange={(event) => setFromDate(event.target.value)} />
        </label>
        <label>
          Bitiş
          <input type="date" value={toDate} min={fromDate} onChange={(event) => setToDate(event.target.value)} />
        </label>
        <div className="toolbar__actions">
          {canManage && settings.data && (
            <button type="button" className="button" onClick={() => setEditing(true)}>Ayarlar</button>
          )}
        </div>
      </div>

      {!range && <p className="alert alert--error" role="alert">Bitiş tarihi başlangıçtan önce olamaz.</p>}
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? settings.error} />

      {unpriced > 0 && (
        <p className="alert alert--info">
          {formatQuantity(unpriced)} adet, ücreti tanımlı olmayan operasyonlarda üretildi ve kazanca girmedi.
          Operasyonlara birim ücret ya da ayarlardan dakika ücreti tanımlayın.
        </p>
      )}

      {range && data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th>Operatör</th>
                <th className="table__number">Gün</th>
                <th className="table__number">Üretilen</th>
                <th className="table__number">Kazanılan süre</th>
                <th className="table__number">Verim</th>
                <th className="table__number">Parça başı</th>
                <th className="table__number">Prim</th>
                <th className="table__number">Toplam</th>
              </tr>
            </thead>
            <tbody>
              {data.rows.map((row) => (
                <EarningsRow key={row.operatorId} row={row} currency={data.currency} onOpen={() => setDetail(row)} />
              ))}
              {data.rows.length === 0 && (
                <tr>
                  <td colSpan={8} className="table__empty">Bu aralıkta üretim bildirimi yok.</td>
                </tr>
              )}
            </tbody>
            {data.rows.length > 0 && (
              <tfoot>
                <EarningsRow row={data.total} currency={data.currency} total />
              </tfoot>
            )}
          </table>
        </div>
      )}

      {editing && settings.data && <SettingsDialog settings={settings.data} onClose={() => setEditing(false)} />}
      {detail && range && <DetailDialog row={detail} range={range} onClose={() => setDetail(null)} />}
    </section>
  )
}
