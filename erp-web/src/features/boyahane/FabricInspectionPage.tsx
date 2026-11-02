import { ScanLine, Search, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDate, formatDateTime, formatQuantity, todayIsoDate } from '../../utils/format'
import { LookupSelect } from '../masterData/LookupSelect'
import {
  customerDecisionChannelLabels,
  useApproveRemainingRollsMutation,
  useFindFabricRollMutation,
  useGetFabricRollQuery,
  useGetFabricRollsQuery,
  useInspectFabricRollMutation,
  useRecordRollCustomerDecisionMutation,
} from './boyahaneInspectionApi'
import type { CustomerDecisionChannel, FabricRollDetail } from './boyahaneInspectionApi'
import { fabricRollStatusPresentation } from './boyahaneReceiptsApi'
import type { FabricRollStatus } from './boyahaneReceiptsApi'

const PAGE_SIZE = 20
const PENDING: FabricRollStatus = 'PENDING_INSPECTION'

const numberOrNull = (value: string) => (value.trim() === '' ? null : Number(value))
const text = (value: number | null) => (value === null ? '' : String(value))

interface DefectDraft {
  key: number
  defectTypeId: number | null
  quantity: string
  atMeter: string
  note: string
}

/** Kontrol kararı: ölçüler, hatalar ve uygun / şartlı / red. */
function InspectionForm({ roll, onDone }: { roll: FabricRollDetail; onDone: () => void }) {
  const [inspect, { isLoading, error }] = useInspectFabricRollMutation()
  const [approveRemaining, { isLoading: approving, error: approveError }] = useApproveRemainingRollsMutation()
  // Ölçümden hesaplanan karşılık forma dolmaz; yeniden kaydedilirse ölçülmüş sayılırdı.
  const [meters, setMeters] = useState(roll.metersMeasured ? text(roll.inspectedMeters) : '')
  const [kilograms, setKilograms] = useState(roll.kilogramsMeasured ? text(roll.inspectedKilograms) : '')
  const [width, setWidth] = useState(text(roll.inspectedWidthCm))
  const [weight, setWeight] = useState(text(roll.inspectedWeightGsm))
  const [note, setNote] = useState(roll.inspectionNote ?? '')
  const [nextKey, setNextKey] = useState(roll.defects.length)
  const [defects, setDefects] = useState<DefectDraft[]>(roll.defects.map((defect, index) => ({
    key: index,
    defectTypeId: defect.defectTypeId,
    quantity: String(defect.quantity),
    atMeter: text(defect.atMeter),
    note: defect.note ?? '',
  })))

  const updateDefect = (key: number, change: Partial<DefectDraft>) =>
    setDefects(defects.map((defect) => (defect.key === key ? { ...defect, ...change } : defect)))

  const addDefect = () => {
    setDefects([...defects, { key: nextKey, defectTypeId: null, quantity: '1', atMeter: '', note: '' }])
    setNextKey(nextKey + 1)
  }

  const decide = async (decision: FabricRollStatus) => {
    const result = await inspect({
      id: roll.id,
      body: {
        decision,
        inspectedMeters: numberOrNull(meters),
        inspectedKilograms: numberOrNull(kilograms),
        inspectedWidthCm: numberOrNull(width),
        inspectedWeightGsm: numberOrNull(weight),
        note: note.trim() || null,
        defects: defects.map((defect) => ({
          defectTypeId: defect.defectTypeId ?? 0,
          quantity: Number(defect.quantity),
          atMeter: numberOrNull(defect.atMeter),
          note: defect.note.trim() || null,
        })),
      },
    })
    if (!('error' in result)) {
      onDone()
    }
  }

  const handleApproveRemaining = async () => {
    const result = await approveRemaining(roll.lotId)
    if (!('error' in result)) {
      onDone()
    }
  }

  return (
    <div className="form">
      <fieldset className="form__section">
        <legend>Ölçüm</legend>
        <p className="muted form__note">
          Kabul: {formatQuantity(roll.receivedMeters)} m · {formatQuantity(roll.receivedKilograms)} kg. Kart: {formatQuantity(roll.rawWidthCm)} cm · {roll.rawWeightGsm} g/m². Ölçmediğiniz alanı boş bırakın.
        </p>
        <div className="form__pair">
          <label className="field"><span>Metre</span><input type="number" min="0" step="0.01" value={meters} onChange={(event) => setMeters(event.target.value)} /></label>
          <label className="field"><span>Kilo</span><input type="number" min="0" step="0.01" value={kilograms} onChange={(event) => setKilograms(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <label className="field"><span>En (cm)</span><input type="number" min="0" step="0.01" value={width} onChange={(event) => setWidth(event.target.value)} /></label>
          <label className="field"><span>Gramaj (g/m²)</span><input type="number" min="0" step="1" value={weight} onChange={(event) => setWeight(event.target.value)} /></label>
        </div>
      </fieldset>

      <fieldset className="form__section">
        <legend>Hatalar</legend>
        {defects.length > 0 && (
          <div className="table-wrap recipe-table">
            <table className="table table--compact">
              <colgroup>
                <col className="defect-table__type" />
                <col className="defect-table__number" />
                <col className="defect-table__number" />
                <col />
                <col className="recipe-table__remove" />
              </colgroup>
              <thead>
                <tr>
                  <th>Hata türü</th>
                  <th>Adet</th>
                  <th>Metre</th>
                  <th>Not</th>
                  <th aria-label="Satırı kaldır" />
                </tr>
              </thead>
              <tbody>
                {defects.map((defect) => (
                  <tr key={defect.key}>
                    <td>
                      <LookupSelect
                        resource="boyahane-defect-types"
                        label="Hata türü"
                        hideLabel
                        idSuffix={String(defect.key)}
                        value={defect.defectTypeId}
                        onChange={(value) => updateDefect(defect.key, { defectTypeId: value })}
                      />
                    </td>
                    <td><input aria-label="Adet" type="number" min="1" step="1" value={defect.quantity} onChange={(event) => updateDefect(defect.key, { quantity: event.target.value })} /></td>
                    <td><input aria-label="Hatanın metresi" type="number" min="0" step="0.01" value={defect.atMeter} onChange={(event) => updateDefect(defect.key, { atMeter: event.target.value })} /></td>
                    <td><input aria-label="Hata notu" maxLength={200} value={defect.note} onChange={(event) => updateDefect(defect.key, { note: event.target.value })} /></td>
                    <td className="recipe-table__remove-cell">
                      <button type="button" className="icon-button" aria-label="Satırı kaldır" title="Satırı kaldır" onClick={() => setDefects(defects.filter((item) => item.key !== defect.key))}>
                        <X aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <div><button type="button" className="button" onClick={addDefect}>Hata ekle</button></div>
      </fieldset>

      <label className="field">
        <span>Not</span>
        <textarea rows={2} maxLength={1000} value={note} onChange={(event) => setNote(event.target.value)} />
      </label>

      <ErrorAlert error={error ?? approveError} />
      <footer className="form__actions">
        {roll.status === PENDING && roll.pendingRollsInLot > 0 && (
          <button type="button" className="button" disabled={approving} onClick={handleApproveRemaining}>
            Lotun kalanını uygun yap ({roll.pendingRollsInLot})
          </button>
        )}
        <button type="button" className="button button--danger-ghost" disabled={isLoading} onClick={() => decide('REJECTED')}>Red</button>
        <button type="button" className="button" disabled={isLoading} onClick={() => decide('CONDITIONAL')}>Şartlı</button>
        {roll.status !== 'CONDITIONAL' && (
          <button type="button" className="button button--primary" disabled={isLoading} onClick={() => decide('APPROVED')}>Uygun</button>
        )}
      </footer>
    </div>
  )
}

/** Şartlı top için müşterinin kararı; kontrolden ayrı bir yetkidir. */
function CustomerDecisionForm({ roll, onDone }: { roll: FabricRollDetail; onDone: () => void }) {
  const [record, { isLoading, error }] = useRecordRollCustomerDecisionMutation()
  const [decidedBy, setDecidedBy] = useState('')
  const [channel, setChannel] = useState<CustomerDecisionChannel>('EMAIL')
  const [decidedOn, setDecidedOn] = useState(todayIsoDate())
  const [note, setNote] = useState('')

  const decide = async (accepted: boolean) => {
    const result = await record({ id: roll.id, body: { accepted, decidedBy, channel, decidedOn, note: note.trim() || null } })
    if (!('error' in result)) {
      onDone()
    }
  }

  return (
    <fieldset className="form__section">
      <legend>{roll.customerName ? 'Müşteri kararı' : 'Yetkili kararı'}</legend>
      <div className="form__pair">
        <label className="field"><span>Kararı veren</span><input maxLength={100} value={decidedBy} onChange={(event) => setDecidedBy(event.target.value)} /></label>
        <label className="field"><span>Tarih</span><input type="date" value={decidedOn} onChange={(event) => setDecidedOn(event.target.value)} /></label>
      </div>
      <label className="field">
        <span>İletişim yolu</span>
        <select value={channel} onChange={(event) => setChannel(event.target.value as CustomerDecisionChannel)}>
          {Object.entries(customerDecisionChannelLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
      </label>
      <label className="field"><span>Not</span><input maxLength={500} value={note} onChange={(event) => setNote(event.target.value)} /></label>
      <ErrorAlert error={error} />
      <footer className="form__actions">
        <button type="button" className="button button--danger-ghost" disabled={isLoading} onClick={() => decide(false)}>Kabul etmedi (red)</button>
        <button type="button" className="button button--primary" disabled={isLoading} onClick={() => decide(true)}>Kabul etti</button>
      </footer>
    </fieldset>
  )
}

function RollDialog({ id, onClose }: { id: number; onClose: () => void }) {
  const { data: roll, error, isLoading } = useGetFabricRollQuery(id)
  const canInspect = usePermission(Permissions.BoyahaneInspectionManage)
  const canDecide = usePermission(Permissions.BoyahaneInspectionCustomerDecision)

  return (
    <Modal title={roll ? `Top ${roll.rollNo}` : 'Top'} onClose={onClose} variant="drawer">
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {roll && (
        <>
          <p>
            <strong>{roll.fabricCode}</strong> — {roll.fabricName}
            <br />
            <span className="muted">
              {roll.customerName ?? 'İşletme malı'} · Lot {roll.lotNo}{roll.customerRollNo && ` · Müşteri top no ${roll.customerRollNo}`}
            </span>
          </p>
          <p>
            <span className={`badge badge--${fabricRollStatusPresentation[roll.status].tone}`}>{fabricRollStatusPresentation[roll.status].label}</span>
            {roll.isConditionallyApproved && <> <span className="badge badge--info">Şartlı onaylı</span></>}
            {roll.inspectedAt && <span className="muted"> {formatDateTime(roll.inspectedAt)} · {roll.inspectedBy}</span>}
          </p>

          {roll.customerDecisionAccepted !== null && (
            <div className="alert alert--info">
              Karar: {roll.customerDecisionAccepted ? 'Kabul edildi' : 'Kabul edilmedi'} · {roll.customerDecisionBy}
              {roll.customerDecisionChannel && ` · ${customerDecisionChannelLabels[roll.customerDecisionChannel]}`}
              {roll.customerDecisionOn && ` · ${formatDate(roll.customerDecisionOn)}`}
              {roll.customerDecisionNote && ` · ${roll.customerDecisionNote}`}
              <br />
              Kaydeden: {roll.customerDecisionRecordedBy}
            </div>
          )}

          {!roll.canInspect && roll.defects.length > 0 && (
            <ul>
              {roll.defects.map((defect) => (
                <li key={defect.id}>
                  {defect.defectName} × {defect.quantity}
                  {defect.atMeter !== null && ` · ${formatQuantity(defect.atMeter)}. metre`}
                  {defect.note && ` · ${defect.note}`}
                </li>
              ))}
            </ul>
          )}

          {roll.status === 'CONDITIONAL' && canDecide && <CustomerDecisionForm roll={roll} onDone={onClose} />}
          {roll.canInspect && canInspect && <InspectionForm key={roll.id} roll={roll} onDone={onClose} />}
        </>
      )}
    </Modal>
  )
}

/** Ham kontrol: top barkodla ya da listeden açılır. */
export function FabricInspectionPage() {
  const grid = useGrid(undefined, { status: PENDING })
  const { page, setPage } = grid
  const scan = useRef<HTMLInputElement>(null)
  const [scanValue, setScanValue] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [openId, setOpenId] = useState<number | null>(null)
  const [find, { error: findError }] = useFindFabricRollMutation()

  const { data, error, isLoading, isFetching } = useGetFabricRollsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  // Barkod okuyucu klavye gibi yazar: pencere kapanınca okutma kutusu yeniden hazır olur.
  useEffect(() => {
    if (openId === null) {
      scan.current?.focus()
    }
  }, [openId])

  const columns: GridColumn[] = [
    { key: 'rollNo', label: 'Top no' },
    { key: 'customerRollNo', label: 'Müşteri top no' },
    { key: 'customerName', label: 'Malın sahibi' },
    { key: 'fabricCode', label: 'Kumaş' },
    { key: 'lotNo', label: 'Lot' },
    { key: 'meters', label: 'Metre', numeric: true },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'defectCount', label: 'Hata', numeric: true },
    {
      key: 'status',
      label: 'Durum',
      filter: Object.entries(fabricRollStatusPresentation).map(([value, { label }]) => ({ value, label })),
    },
  ]

  const handleScan = async (event: FormEvent) => {
    event.preventDefault()
    const rollNo = scanValue.trim()
    if (rollNo === '') {
      return
    }
    const result = await find(rollNo)
    setScanValue('')
    if ('data' in result && result.data) {
      setOpenId(result.data.id)
    }
  }

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Ham Kontrol</h1>

      <div className="toolbar">
        <form onSubmit={handleScan} role="search" className="toolbar__group">
          <input
            ref={scan}
            placeholder="Top barkodunu okutun"
            aria-label="Top barkodu"
            autoComplete="off"
            value={scanValue}
            onChange={(event) => setScanValue(event.target.value)}
          />
          <button type="submit" className="button"><ScanLine aria-hidden="true" />Aç</button>
        </form>
        <form onSubmit={handleSearch} role="search" className="toolbar__group">
          <input
            type="search"
            placeholder="Top, lot ya da müşteri"
            aria-label="Top ara"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
          <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
          <ClearFiltersButton grid={grid} />
        </form>
      </div>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? findError} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((roll) => {
                  const status = fabricRollStatusPresentation[roll.status]
                  return (
                    <tr key={roll.id} className="table__row--link" onClick={() => setOpenId(roll.id)}>
                      <td><code>{roll.rollNo}</code></td>
                      <td>{roll.customerRollNo ?? '—'}</td>
                      <td>{roll.customerName ?? <span className="muted">İşletme malı</span>}</td>
                      <td>{roll.fabricCode}</td>
                      <td>{roll.lotNo}</td>
                      <td className="table__number">{formatQuantity(roll.meters)}</td>
                      <td className="table__number">{formatQuantity(roll.kilograms)}</td>
                      <td className="table__number">{roll.defectCount}</td>
                      <td>
                        <span className={`badge badge--${status.tone}`}>{status.label}</span>
                        {roll.isConditionallyApproved && <> <span className="badge badge--info">Şartlı onaylı</span></>}
                      </td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr><td colSpan={columns.length} className="table__empty">Top bulunamadı.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {openId !== null && <RollDialog id={openId} onClose={() => setOpenId(null)} />}
    </section>
  )
}
