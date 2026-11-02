import { ClipboardCheck, LogOut, Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
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
import { formatMoney } from '../../utils/money'
import { LookupSelect } from '../masterData/LookupSelect'
import { dokumaMaterialTypeLabels, unitLabel } from './dokumaPresentation'
import {
  dokumaMaterialMovementLabels,
  useAdjustDokumaMaterialLotMutation,
  useCountDokumaMaterialMutation,
  useDeleteDokumaMaterialLotMutation,
  useGetDokumaMaterialLotsQuery,
  useGetDokumaMaterialMovementsQuery,
  useGetDokumaMaterialStockQuery,
  useIssueDokumaMaterialMutation,
  useSaveDokumaMaterialLotMutation,
} from './dokumaStockApi'
import type { DokumaMaterialLot, DokumaMaterialStockRow } from './dokumaStockApi'

const PAGE_SIZE = 20

/** Malzeme girişi (lot): elle giriş ya da satın almadan gelen lotun düzeltilmesi. */
function MaterialLotDialog({ lot, materialId, canManage, onClose }: { lot: DokumaMaterialLot | null; materialId?: number; canManage: boolean; onClose: () => void }) {
  const [save, { isLoading, error }] = useSaveDokumaMaterialLotMutation()
  const [remove, removeState] = useDeleteDokumaMaterialLotMutation()
  const [selectedMaterial, setSelectedMaterial] = useState<number | null>(lot?.materialId ?? materialId ?? null)
  const [receiptDate, setReceiptDate] = useState(lot?.receiptDate ?? todayIsoDate())
  const [quantity, setQuantity] = useState(lot ? String(lot.quantity) : '')
  const [supplierId, setSupplierId] = useState<number | null>(lot?.supplierId ?? null)
  const [supplierLotNo, setSupplierLotNo] = useState(lot?.supplierLotNo ?? '')
  const [despatchNo, setDespatchNo] = useState(lot?.despatchNo ?? '')
  const [unitPrice, setUnitPrice] = useState(lot?.unitPrice === null || lot?.unitPrice === undefined ? '' : String(lot.unitPrice))
  const [currency, setCurrency] = useState(lot?.currency ?? '')
  const [notes, setNotes] = useState(lot?.notes ?? '')
  const [confirmDelete, setConfirmDelete] = useState(false)

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await save({
      id: lot?.id,
      body: {
        materialId: selectedMaterial ?? 0,
        receiptDate,
        quantity: Number(quantity),
        supplierId,
        supplierLotNo: supplierLotNo.trim() || null,
        despatchNo: despatchNo.trim() || null,
        unitPrice: unitPrice.trim() === '' ? null : Number(unitPrice),
        currency: currency.trim() || null,
        notes: notes.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  const handleDelete = async () => {
    if (lot && !('error' in await remove(lot.id))) {
      onClose()
    }
  }

  return (
    <Modal title={lot ? `Malzeme lotu: ${lot.lotNo}` : 'Yeni malzeme girişi'} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        {lot?.isUsed && <div className="alert alert--info">Bu lottan çıkış ya da düzeltme yapıldı: malzemesi ve miktarı değiştirilemez, lot silinemez. Kalanı düzeltmek için lot düzeltmesi yapın.</div>}
        {lot?.purchaseOrderNo && (
          <p className="muted">Satın alma: <Link to={`/dokuma/purchase-orders/${lot.purchaseOrderId}`}>{lot.purchaseOrderNo}</Link></p>
        )}
        <fieldset disabled={!canManage} className="form">
          <LookupSelect resource="dokuma-materials" label="Malzeme" required value={selectedMaterial} onChange={setSelectedMaterial} disabled={lot !== null && (lot.isUsed || lot.purchaseOrderId !== null)} />
          <div className="form__pair">
            <label className="field"><span>Giriş tarihi</span><input type="date" required value={receiptDate} onChange={(event) => setReceiptDate(event.target.value)} /></label>
            <label className="field"><span>Miktar</span><input type="number" min="0" step="0.001" required disabled={lot?.isUsed} value={quantity} onChange={(event) => setQuantity(event.target.value)} /></label>
          </div>
          <LookupSelect resource="dokuma-suppliers" label="Tedarikçi" value={supplierId} onChange={setSupplierId} placeholder="İsteğe bağlı" />
          <div className="form__pair">
            <label className="field"><span>Tedarikçi lot no</span><input maxLength={50} value={supplierLotNo} onChange={(event) => setSupplierLotNo(event.target.value)} /></label>
            <label className="field"><span>İrsaliye no</span><input maxLength={50} value={despatchNo} onChange={(event) => setDespatchNo(event.target.value)} /></label>
          </div>
          <div className="form__pair">
            <label className="field"><span>Birim fiyat</span><input type="number" min="0" step="0.0001" value={unitPrice} onChange={(event) => setUnitPrice(event.target.value)} /></label>
            <label className="field"><span>Para birimi</span><input maxLength={3} placeholder="TRY" value={currency} onChange={(event) => setCurrency(event.target.value.toUpperCase())} /></label>
          </div>
          <label className="field"><span>Not</span><textarea rows={2} maxLength={500} value={notes} onChange={(event) => setNotes(event.target.value)} /></label>
        </fieldset>
        <ErrorAlert error={error ?? removeState.error} />
        <footer className="form__actions">
          {canManage && lot && !lot.isUsed && !confirmDelete && <button type="button" className="button button--danger-ghost" onClick={() => setConfirmDelete(true)}>Sil</button>}
          {canManage && lot && confirmDelete && <button type="button" className="button button--danger-ghost" disabled={removeState.isLoading} onClick={handleDelete}>Silmeyi onayla</button>}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          {canManage && <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>}
        </footer>
      </form>
    </Modal>
  )
}

/** Lotun kalanını sayılan miktara düzeltir; fark gerekçesiyle hareket olarak yazılır. */
function AdjustLotDialog({ lot, onClose }: { lot: DokumaMaterialLot; onClose: () => void }) {
  const [adjust, { isLoading, error }] = useAdjustDokumaMaterialLotMutation()
  const [counted, setCounted] = useState(String(lot.remainingQuantity))
  const [reason, setReason] = useState('')
  const [date, setDate] = useState(todayIsoDate())

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await adjust({ id: lot.id, countedQuantity: Number(counted), reason: reason.trim(), date })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Lot düzeltmesi: ${lot.lotNo}`} onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <p className="muted">{lot.materialCode} — {lot.materialName} · Kalan {formatQuantity(lot.remainingQuantity)} {unitLabel(lot.unit)}</p>
        <div className="form__pair">
          <label className="field"><span>Sayılan miktar</span><input type="number" min="0" step="0.001" required value={counted} onChange={(event) => setCounted(event.target.value)} /></label>
          <label className="field"><span>Tarih</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
        </div>
        <label className="field"><span>Gerekçe</span><input required maxLength={200} value={reason} onChange={(event) => setReason(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Tezgâha ya da bölüme çıkış: en eski lottan başlayarak düşülür. */
function IssueDialog({ material, onClose }: { material: DokumaMaterialStockRow | null; onClose: () => void }) {
  const [issue, { isLoading, error }] = useIssueDokumaMaterialMutation()
  const [materialId, setMaterialId] = useState<number | null>(material?.materialId ?? null)
  const [quantity, setQuantity] = useState('')
  const [date, setDate] = useState(todayIsoDate())
  const [loomId, setLoomId] = useState<number | null>(null)
  const [department, setDepartment] = useState('')
  const [reason, setReason] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await issue({ materialId: materialId ?? 0, quantity: Number(quantity), date, reason: reason.trim(), loomId, department: department.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Malzeme çıkışı" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="dokuma-materials" label="Malzeme" required value={materialId} onChange={setMaterialId} />
        {material && material.materialId === materialId && (
          <p className="muted">Stok: {formatQuantity(material.quantity)} {unitLabel(material.unit)}</p>
        )}
        <div className="form__pair">
          <label className="field"><span>Miktar</span><input type="number" min="0" step="0.001" required value={quantity} onChange={(event) => setQuantity(event.target.value)} /></label>
          <label className="field"><span>Tarih</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
        </div>
        <div className="form__pair">
          <LookupSelect resource="dokuma-looms" label="Tezgâh" value={loomId} onChange={setLoomId} placeholder="Yedek parçada isteğe bağlı" />
          <label className="field"><span>Bölüm</span><input maxLength={100} placeholder="Örn. hazırlık, bakım" value={department} onChange={(event) => setDepartment(event.target.value)} /></label>
        </div>
        <label className="field"><span>Neden</span><input required maxLength={200} placeholder="Örn. arıza değişimi, periyodik bakım" value={reason} onChange={(event) => setReason(event.target.value)} /></label>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Çıkış yap</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Malzeme sayımı: sayılan toplam ile stok arasındaki fark lotlara yansıtılır. */
function CountDialog({ material, onClose }: { material: DokumaMaterialStockRow | null; onClose: () => void }) {
  const [count, { isLoading, error }] = useCountDokumaMaterialMutation()
  const [materialId, setMaterialId] = useState<number | null>(material?.materialId ?? null)
  const [counted, setCounted] = useState(material ? String(material.quantity) : '')
  const [date, setDate] = useState(todayIsoDate())
  const [reason, setReason] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await count({ materialId: materialId ?? 0, countedQuantity: Number(counted), date, reason: reason.trim() || null })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Malzeme sayımı" onClose={onClose}>
      <form className="form" onSubmit={handleSubmit}>
        <LookupSelect resource="dokuma-materials" label="Malzeme" required value={materialId} onChange={setMaterialId} />
        {material && material.materialId === materialId && (
          <p className="muted">Sistemdeki stok: {formatQuantity(material.quantity)} {unitLabel(material.unit)}</p>
        )}
        <div className="form__pair">
          <label className="field"><span>Sayılan miktar</span><input type="number" min="0" step="0.001" required value={counted} onChange={(event) => setCounted(event.target.value)} /></label>
          <label className="field"><span>Sayım tarihi</span><input type="date" required value={date} onChange={(event) => setDate(event.target.value)} /></label>
        </div>
        <label className="field"><span>Not</span><input maxLength={200} placeholder="Boşsa “Sayım”" value={reason} onChange={(event) => setReason(event.target.value)} /></label>
        <p className="muted">Eksik en eski lottan düşülür; fazla en son lota eklenir.</p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>Kaydet</button>
        </footer>
      </form>
    </Modal>
  )
}

/** Malzemenin lotları: kalanı olan ve tükenen lotlar; lot açılır, düzeltilir. */
function MaterialLotsDrawer({ material, canManage, onClose }: { material: DokumaMaterialStockRow; canManage: boolean; onClose: () => void }) {
  const [openOnly, setOpenOnly] = useState(true)
  const [page, setPage] = useState(1)
  const { data, error, isFetching } = useGetDokumaMaterialLotsQuery({ materialId: material.materialId, openOnly, page, pageSize: PAGE_SIZE })
  const [editing, setEditing] = useState<DokumaMaterialLot | null | undefined>(undefined)
  const [adjusting, setAdjusting] = useState<DokumaMaterialLot | null>(null)
  const unit = unitLabel(material.unit)

  return (
    <Modal title={`${material.code} — ${material.name}`} onClose={onClose} variant="drawer">
      <div className="toolbar">
        <span className="muted">Stok {formatQuantity(material.quantity)} {unit}{material.minimumStock !== null && ` · Kritik ${formatQuantity(material.minimumStock)} ${unit}`}</span>
        <label className="checkbox"><input type="checkbox" checked={openOnly} onChange={(event) => { setOpenOnly(event.target.checked); setPage(1) }} />Yalnızca kalanı olanlar</label>
        <div className="toolbar__actions">
          {canManage && <button type="button" className="button button--primary" onClick={() => setEditing(null)}><Plus aria-hidden="true" />Giriş</button>}
          <Link className="button" to={`/dokuma/material-movements?materialId=${material.materialId}`}>Hareketler</Link>
        </div>
      </div>
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <thead><tr><th>Lot no</th><th>Tarih</th><th>Tedarikçi</th><th>Tedarikçi lot</th><th className="table__number">Giriş</th><th className="table__number">Kalan</th><th className="table__number">Birim fiyat</th><th aria-label="İşlem" /></tr></thead>
              <tbody>
                {data.items.map((lot) => (
                  <tr key={lot.id} className="table__row--link" onClick={() => setEditing(lot)}>
                    <td><code>{lot.lotNo}</code></td>
                    <td>{formatDate(lot.receiptDate)}</td>
                    <td>{lot.supplierName ?? '—'}{lot.purchaseOrderNo && <span className="muted"> · {lot.purchaseOrderNo}</span>}</td>
                    <td>{lot.supplierLotNo ?? '—'}</td>
                    <td className="table__number">{formatQuantity(lot.quantity)}</td>
                    <td className="table__number">{formatQuantity(lot.remainingQuantity)}</td>
                    <td className="table__number">{lot.unitPrice === null ? '—' : formatMoney(lot.unitPrice, lot.currency)}</td>
                    <td className="table__actions">
                      {canManage && (
                        <button type="button" className="button" onClick={(event) => { event.stopPropagation(); setAdjusting(lot) }}>Düzelt</button>
                      )}
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={8} className="table__empty">Lot bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
      {editing !== undefined && <MaterialLotDialog lot={editing} materialId={material.materialId} canManage={canManage} onClose={() => setEditing(undefined)} />}
      {adjusting && <AdjustLotDialog lot={adjusting} onClose={() => setAdjusting(null)} />}
    </Modal>
  )
}

/** Malzeme stoğu: malzeme başına kalan, siparişte bekleyen ve kritik seviye. */
export function DokumaMaterialStockPage() {
  const canManage = usePermission(Permissions.DokumaStockManage)
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [type, setType] = useState('')
  const [criticalOnly, setCriticalOnly] = useState(false)
  const [includeInactive, setIncludeInactive] = useState(false)
  const [selected, setSelected] = useState<DokumaMaterialStockRow | null>(null)
  const [dialog, setDialog] = useState<'receipt' | 'issue' | 'count' | null>(null)
  const { data, error, isLoading, isFetching } = useGetDokumaMaterialStockQuery({ search: search || undefined, criticalOnly, includeInactive })
  const rows = (data ?? []).filter((row) => type === '' || row.type === type)
  const criticalCount = (data ?? []).filter((row) => row.isCritical && row.isActive).length

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Malzeme Stoğu</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Malzeme kodu ya da adı" aria-label="Malzeme ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <select aria-label="Malzeme türü" value={type} onChange={(event) => setType(event.target.value)}>
          <option value="">Bütün türler</option>
          {Object.entries(dokumaMaterialTypeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <label className="checkbox"><input type="checkbox" checked={criticalOnly} onChange={(event) => setCriticalOnly(event.target.checked)} />Kritik stok altındakiler{criticalCount > 0 && !criticalOnly && ` (${criticalCount})`}</label>
        <label className="checkbox"><input type="checkbox" checked={includeInactive} onChange={(event) => setIncludeInactive(event.target.checked)} />Pasifler</label>
        <div className="toolbar__actions">
          {canManage && (
            <>
              <button type="button" className="button button--primary" onClick={() => setDialog('receipt')}><Plus aria-hidden="true" />Giriş</button>
              <button type="button" className="button" onClick={() => setDialog('issue')}><LogOut aria-hidden="true" />Çıkış</button>
              <button type="button" className="button" onClick={() => setDialog('count')}><ClipboardCheck aria-hidden="true" />Sayım</button>
            </>
          )}
        </div>
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <div className="table-wrap" aria-busy={isFetching}>
          <table className="table">
            <thead>
              <tr>
                <th>Kod</th><th>Ad</th><th>Tür</th><th className="table__number">Stok</th><th className="table__number">Kritik stok</th>
                <th className="table__number">Siparişte</th><th className="table__number">Lot</th><th>Tercihli tedarikçi</th><th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const unit = unitLabel(row.unit)
                return (
                  <tr key={row.materialId} className="table__row--link" onClick={() => setSelected(row)}>
                    <td><code>{row.code}</code></td>
                    <td>{row.name}{!row.isActive && <span className="muted"> (pasif)</span>}</td>
                    <td>{dokumaMaterialTypeLabels[row.type] ?? row.type}</td>
                    <td className="table__number">{formatQuantity(row.quantity)} {unit}</td>
                    <td className="table__number">{row.minimumStock === null ? '—' : `${formatQuantity(row.minimumStock)} ${unit}`}</td>
                    <td className="table__number">{row.onOrderQuantity > 0 ? `${formatQuantity(row.onOrderQuantity)} ${unit}` : '—'}</td>
                    <td className="table__number">{row.lotCount}</td>
                    <td>{row.preferredSupplierName ?? '—'}</td>
                    <td>
                      {row.isCritical
                        ? <span className="badge badge--danger">Kritik</span>
                        : row.quantity <= 0 ? <span className="badge badge--warning">Stok yok</span> : <span className="badge badge--ok">Yeterli</span>}
                    </td>
                  </tr>
                )
              })}
              {rows.length === 0 && <tr><td colSpan={9} className="table__empty">Malzeme bulunamadı. Malzeme kartları Tanımlar menüsünden açılır.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
      {selected && <MaterialLotsDrawer material={selected} canManage={canManage} onClose={() => setSelected(null)} />}
      {dialog === 'receipt' && <MaterialLotDialog lot={null} canManage={canManage} onClose={() => setDialog(null)} />}
      {dialog === 'issue' && <IssueDialog material={null} onClose={() => setDialog(null)} />}
      {dialog === 'count' && <CountDialog material={null} onClose={() => setDialog(null)} />}
    </section>
  )
}

/** Malzeme hareketleri: giriş, çıkış, haşıl tüketimi, düzeltme ve sayım farkları. */
export function DokumaMaterialMovementsPage() {
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchParams] = useSearchParams()
  const [materialId, setMaterialId] = useState<number | null>(Number(searchParams.get('materialId')) || null)
  const [input, setInput] = useState('')
  const [search, setSearch] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const { data, error, isLoading, isFetching } = useGetDokumaMaterialMovementsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    materialId: materialId ?? undefined,
    from: from || undefined,
    to: to || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'movementDate', label: 'Tarih', filter: false },
    { key: 'type', label: 'Hareket', filter: Object.entries(dokumaMaterialMovementLabels).map(([value, label]) => ({ value, label })) },
    { key: 'materialCode', label: 'Malzeme' },
    { key: 'lotNo', label: 'Lot' },
    { key: 'quantity', label: 'Miktar', numeric: true, filter: false },
    { key: 'loomCode', label: 'Tezgâh' },
    { key: 'department', label: 'Bölüm' },
    { key: 'reason', label: 'Neden' },
    { key: 'reference', label: 'Belge' },
    { key: 'createdBy', label: 'Kaydeden' },
  ]

  const submit = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(input.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Malzeme Hareketleri</h1>
      <form className="toolbar" onSubmit={submit} role="search">
        <input type="search" placeholder="Malzeme, lot, belge, bölüm ya da neden" aria-label="Hareket ara" value={input} onChange={(event) => setInput(event.target.value)} />
        <div style={{ minWidth: '16rem' }}>
          <LookupSelect resource="dokuma-materials" label="Malzeme" hideLabel value={materialId} onChange={(value) => { setMaterialId(value); setPage(1) }} placeholder="Bütün malzemeler" />
        </div>
        <input type="date" aria-label="Başlangıç" value={from} onChange={(event) => { setFrom(event.target.value); setPage(1) }} />
        <input type="date" aria-label="Bitiş" value={to} onChange={(event) => { setTo(event.target.value); setPage(1) }} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
      </form>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((movement) => (
                  <tr key={movement.id}>
                    <td title={formatDateTime(movement.createdAt)}>{formatDate(movement.movementDate)}</td>
                    <td>{dokumaMaterialMovementLabels[movement.type]}</td>
                    <td><code>{movement.materialCode}</code> {movement.materialName}</td>
                    <td><code>{movement.lotNo}</code></td>
                    <td className="table__number">
                      {movement.quantity > 0 ? '+' : ''}{formatQuantity(movement.quantity)} {unitLabel(movement.unit)}
                    </td>
                    <td>{movement.loomCode ?? '—'}</td>
                    <td>{movement.department ?? '—'}</td>
                    <td>{movement.reason ?? '—'}</td>
                    <td>{movement.beamId ? <Link to={`/dokuma/beams/${movement.beamId}/sizing-document`}>{movement.beamNo}</Link> : movement.reference ?? '—'}</td>
                    <td>{movement.createdBy ?? '—'}</td>
                  </tr>
                ))}
                {data.items.length === 0 && <tr><td colSpan={columns.length} className="table__empty">Hareket bulunamadı.</td></tr>}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </section>
  )
}
