import { FilterX, Printer, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { MovementDialog } from './MovementDialog'
import type { MovementKind } from './MovementDialog'
import { useGetStockLotsQuery, useUpdateStockLotMutation } from './stockApi'
import type { StockLotBalance } from './stockApi'
import { formatStockQuantity, parseQuantity } from './stockPresentation'
import { WarehouseFilter } from './WarehouseFilter'

const PAGE_SIZE = 25

/** Topun özelliklerini düzeltir; miktar ve yer yalnızca stok hareketiyle değişir. */
function LotEditDialog({ lot, onClose }: { lot: StockLotBalance; onClose: () => void }) {
  const [update, { isLoading, error }] = useUpdateStockLotMutation()
  const [batchNo, setBatchNo] = useState(lot.batchNo ?? '')
  const [shade, setShade] = useState(lot.shade ?? '')
  const [width, setWidth] = useState(lot.width === null ? '' : String(lot.width))
  const [supplierRollNo, setSupplierRollNo] = useState(lot.supplierRollNo ?? '')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await update({
      lotId: lot.lotId,
      body: {
        batchNo: batchNo.trim() || null,
        shade: shade.trim() || null,
        width: parseQuantity(width),
        supplierRollNo: supplierRollNo.trim() || null,
      },
    })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Top ${lot.lotNo}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p>
          <code>{lot.itemCode}</code> {lot.itemName}
        </p>
        <div className="form__pair">
          <TextField label="Parti no" maxLength={50} value={batchNo} onChange={(event) => setBatchNo(event.target.value)} errors={fieldErrors(error, 'batchNo')} />
          <TextField label="Tedarikçi top no" maxLength={50} value={supplierRollNo} onChange={(event) => setSupplierRollNo(event.target.value)} errors={fieldErrors(error, 'supplierRollNo')} />
        </div>
        <div className="form__pair">
          <TextField label="Ton" maxLength={30} value={shade} onChange={(event) => setShade(event.target.value)} errors={fieldErrors(error, 'shade')} />
          <TextField label="En (cm)" type="number" min={0} step="0.01" value={width} onChange={(event) => setWidth(event.target.value)} errors={fieldErrors(error, 'width')} />
        </div>
        <ErrorAlert error={error} fields={['batchNo', 'supplierRollNo', 'shade', 'width']} />
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

/**
 * Top / parti takipli kartların topları: her satır bir topun bir adresteki bakiyesidir. Çıkış, transfer,
 * adres değişimi ve sayım buradan, top seçilerek yapılır.
 */
export function StockLotsPage() {
  const canManage = usePermission(Permissions.StockManage)
  const canAdjust = usePermission(Permissions.StockAdjust)
  const canEditLot = usePermission(Permissions.StockEditLot)
  const [searchParams, setSearchParams] = useSearchParams()

  // Stok ekranından ya da bir hareketten gelindiğinde liste o kart / depo / top ile açılır.
  const stockItemId = Number(searchParams.get('stockItemId')) || undefined
  const lotId = Number(searchParams.get('lotId')) || undefined
  const purchaseOrderId = Number(searchParams.get('purchaseOrderId')) || undefined
  const linked = stockItemId !== undefined || lotId !== undefined || purchaseOrderId !== undefined

  const grid = useGrid()
  const { page, setPage } = grid
  const [warehouseId, setWarehouseId] = useState<number | ''>(Number(searchParams.get('warehouseId')) || '')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [includeZero, setIncludeZero] = useState(lotId !== undefined)
  const [dialog, setDialog] = useState<{ kind: MovementKind | 'edit'; lot: StockLotBalance } | null>(null)

  const { data, error, isLoading, isFetching } = useGetStockLotsQuery({
    page,
    pageSize: PAGE_SIZE,
    warehouseId: warehouseId || undefined,
    stockItemId,
    lotId,
    purchaseOrderId,
    search: search || undefined,
    includeZero: includeZero || undefined,
    ...grid.params,
  })

  const columns: GridColumn[] = [
    { key: 'lotNo', label: 'Top no' },
    { key: 'itemCode', label: 'Stok kartı' },
    { key: 'batchNo', label: 'Parti' },
    { key: 'shade', label: 'Ton' },
    { key: 'width', label: 'En (cm)', numeric: true },
    { key: 'warehouseCode', label: 'Depo' },
    { key: 'locationCode', label: 'Adres' },
    { key: 'quantity', label: 'Kalan', numeric: true },
    { key: 'receivedAt', label: 'Giriş', filter: false },
    { label: '', ariaLabel: 'İşlemler' },
  ]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  const labelQuery = new URLSearchParams({
    ...(warehouseId ? { warehouseId: String(warehouseId) } : {}),
    ...(stockItemId ? { stockItemId: String(stockItemId) } : {}),
    ...(purchaseOrderId ? { purchaseOrderId: String(purchaseOrderId) } : {}),
    ...(search ? { search } : {}),
  }).toString()

  return (
    <section className="page">
      <h1 className="visually-hidden">Toplar ve Partiler</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Top no, parti, tedarikçi top no veya kart"
          aria-label="Top ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <WarehouseFilter value={warehouseId} onChange={(value) => {
            setPage(1)
            setWarehouseId(value)
          }} />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        {(grid.hasFilters || linked) && (
          <button
            type="button"
            className="button button--ghost"
            onClick={() => {
              grid.clearFilters()
              setSearchParams({})
            }}
          >
            <FilterX aria-hidden="true" />
            Filtreleri temizle
          </button>
        )}
        <label className="checkbox">
          <input
            type="checkbox"
            checked={includeZero}
            onChange={(event) => {
              setPage(1)
              setIncludeZero(event.target.checked)
            }}
          />
          Tükenmiş topları da göster
        </label>
        <div className="toolbar__actions">
          <Link className="button" to={`/stock-lots/labels?${labelQuery}`}>
            <Printer aria-hidden="true" />
            Etiket yazdır
          </Link>
        </div>
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((lot) => (
                  <tr key={`${lot.lotId}-${lot.locationId}`}>
                    <td>
                      <code>{lot.lotNo}</code>
                      {lot.supplierRollNo && <div className="muted">Ted. {lot.supplierRollNo}</div>}
                    </td>
                    <td><code>{lot.itemCode}</code> {lot.itemName}</td>
                    <td>
                      {lot.batchNo ?? '—'}
                      {lot.purchaseOrderId !== null && (
                        <div><Link to={`/purchase-orders/${lot.purchaseOrderId}`}>{lot.purchaseOrderNo}</Link></div>
                      )}
                    </td>
                    <td>{lot.shade ?? '—'}</td>
                    <td className="table__number">{lot.width ?? '—'}</td>
                    <td>{lot.warehouseCode}</td>
                    <td><code>{lot.locationCode}</code></td>
                    <td className="table__number">
                      <strong>{formatStockQuantity(lot.quantity, lot.unit)}</strong>
                      <div className="muted">giriş {formatStockQuantity(lot.initialQuantity, lot.unit)}</div>
                    </td>
                    <td>{formatDateTime(lot.receivedAt)}</td>
                    <td className="table__actions">
                      {canManage && (
                        <>
                          <button type="button" className="button" disabled={lot.quantity <= 0} onClick={() => setDialog({ kind: 'issue', lot })}>
                            Çıkış
                          </button>
                          <button type="button" className="button" disabled={lot.quantity <= 0} onClick={() => setDialog({ kind: 'transfer', lot })}>
                            Transfer
                          </button>
                          <button type="button" className="button" disabled={lot.quantity <= 0} onClick={() => setDialog({ kind: 'relocation', lot })}>
                            Adres değiştir
                          </button>
                        </>
                      )}
                      {canAdjust && (
                        <button type="button" className="button" onClick={() => setDialog({ kind: 'adjustment', lot })}>
                          Sayım
                        </button>
                      )}
                      {canEditLot && (
                        <button type="button" className="button" onClick={() => setDialog({ kind: 'edit', lot })}>
                          Düzelt
                        </button>
                      )}
                      <Link className="button" to={`/stock-lots/labels?lotId=${lot.lotId}`}>Etiket</Link>
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Top bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {dialog?.kind === 'edit' && <LotEditDialog key={dialog.lot.lotId} lot={dialog.lot} onClose={() => setDialog(null)} />}
      {dialog && dialog.kind !== 'edit' && (
        <MovementDialog
          // key: başka top / işlem için açılınca form ve istek kimliği yenilensin.
          key={`${dialog.kind}-${dialog.lot.lotId}-${dialog.lot.locationId}`}
          kind={dialog.kind}
          balance={dialog.lot}
          onClose={() => setDialog(null)}
        />
      )}
    </section>
  )
}
