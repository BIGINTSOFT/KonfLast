import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
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
import { formatDate, formatQuantity, todayIsoDate } from '../../utils/format'
import { useCreateDokumaYarnReturnMutation, useDeleteDokumaYarnReturnMutation, useGetDokumaYarnReturnsQuery } from './dokumaShippingApi'
import { useGetDokumaYarnLotsQuery } from './dokumaYarnLotsApi'

const PAGE_SIZE = 20

function ReturnDialog({ onClose }: { onClose: () => void }) {
  const [create, { isLoading, error }] = useCreateDokumaYarnReturnMutation()
  const { data: lots, isLoading: loading, error: loadError } = useGetDokumaYarnLotsQuery({ pageSize: 100 })
  const [lotId, setLotId] = useState<number | null>(null)
  const [returnDate, setReturnDate] = useState(todayIsoDate())
  const [kilograms, setKilograms] = useState('')
  const [notes, setNotes] = useState('')
  // Yalnızca müşterinin, kalanı olan lotları iade edilir.
  const candidates = (lots?.items ?? []).filter((lot) => lot.customerId !== null && lot.remainingKilograms > 0)
  const selected = candidates.find((lot) => lot.id === lotId)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    if (lotId === null) {
      return
    }
    const result = await create({ yarnLotId: lotId, returnDate, kilograms: Number(kilograms), notes: notes.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Yeni iplik iadesi" onClose={onClose} variant="drawer">
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">Fasonda müşterinin kalan ipliği geri verilir; kilo lottan düşer. En son girilen 100 lot listelenir.</p>
        {loading && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr><th aria-label="Seç" /><th>Lot no</th><th>Müşteri</th><th>İplik</th><th className="table__number">Kalan (kg)</th></tr>
            </thead>
            <tbody>
              {candidates.map((lot) => (
                <tr key={lot.id}>
                  <td>
                    <input type="radio" name="lot" aria-label={`${lot.lotNo} seç`} checked={lotId === lot.id}
                      onChange={() => { setLotId(lot.id); setKilograms(String(lot.remainingKilograms)) }} />
                  </td>
                  <td><code>{lot.lotNo}</code></td>
                  <td>{lot.customerName}</td>
                  <td>{lot.yarnCode} — {lot.yarnName}</td>
                  <td className="table__number">{formatQuantity(lot.remainingKilograms)}</td>
                </tr>
              ))}
              {candidates.length === 0 && !loading && <tr><td colSpan={5} className="table__empty">İade edilecek müşteri ipliği yok.</td></tr>}
            </tbody>
          </table>
        </div>
        <div className="form__pair">
          <label className="field"><span>İade tarihi</span><input type="date" required value={returnDate} onChange={(event) => setReturnDate(event.target.value)} /></label>
          <label className="field">
            <span>Kilo</span>
            <input type="number" min="0" step="0.01" max={selected?.remainingKilograms} required disabled={lotId === null} value={kilograms} onChange={(event) => setKilograms(event.target.value)} />
          </label>
        </div>
        <label className="field"><span>Not</span><input maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || lotId === null}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Fasonda müşteriye geri verilen iplikler. */
export function DokumaYarnReturnsPage() {
  const canManage = usePermission(Permissions.DokumaReceiptManage)
  const grid = useGrid()
  const { page, setPage } = grid
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [creating, setCreating] = useState(false)
  const [remove, removeState] = useDeleteDokumaYarnReturnMutation()
  const { data, error, isLoading, isFetching } = useGetDokumaYarnReturnsQuery({ page, pageSize: PAGE_SIZE, search: search || undefined, ...grid.params })

  const columns: GridColumn[] = [
    { key: 'returnNo', label: 'İade no' },
    { key: 'returnDate', label: 'Tarih', filter: false },
    { key: 'customerName', label: 'Müşteri' },
    { key: 'lotNo', label: 'Lot' },
    { key: 'yarnCode', label: 'İplik' },
    { key: 'kilograms', label: 'Kilo', numeric: true },
    { key: 'notes', label: 'Not' },
    ...(canManage ? [{ label: '', ariaLabel: 'İşlem', className: 'table__actions' }] : []),
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">İplik İadeleri</h1>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input type="search" placeholder="İade no, lot, iplik ya da müşteri" aria-label="İplik iadesi ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setCreating(true)}><Plus aria-hidden="true" />Yeni iplik iadesi</button>}
        </div>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error ?? removeState.error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((row) => (
                  <tr key={row.id}>
                    <td><code>{row.returnNo}</code></td>
                    <td>{formatDate(row.returnDate)}</td>
                    <td>{row.customerName}</td>
                    <td>{row.lotNo}{row.producerLotNo && <div className="muted">{row.producerLotNo}</div>}</td>
                    <td>{row.yarnCode} — {row.yarnName}</td>
                    <td className="table__number">{formatQuantity(row.kilograms)}</td>
                    <td>{row.notes ?? '—'}</td>
                    {canManage && (
                      <td className="table__actions">
                        <button type="button" className="button" title="İadeyi sil; iplik lota döner" disabled={removeState.isLoading} onClick={() => remove(row.id)}>Sil</button>
                      </td>
                    )}
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">İplik iadesi bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {creating && <ReturnDialog onClose={() => setCreating(false)} />}
    </section>
  )
}
