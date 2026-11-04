import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Modal } from '../../components/Modal'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTimeShort, formatQuantity } from '../../utils/format'
import { useGetProductionOrdersQuery } from '../productionOrders/productionOrdersApi'
import { CuttingPlanStatusBadge } from './CuttingBadges'
import { CuttingPlanFormDialog } from './CuttingPlanFormDialog'
import { useGetCuttingPlansQuery } from './cuttingApi'
import { cuttingPlanStatusPresentation, formatMeters } from './cuttingPresentation'

const PAGE_SIZE = 25

const columns: GridColumn[] = [
  { key: 'planNo', label: 'Plan no' },
  { key: 'orderNo', label: 'İş emri' },
  { key: 'modelCode', label: 'Model' },
  { key: 'colorName', label: 'Renk' },
  { key: 'markerLength', label: 'Pastal (m)', numeric: true },
  { key: 'plannedPlies', label: 'Kat', numeric: true },
  { key: 'plannedQuantity', label: 'Adet', numeric: true },
  { key: 'plannedFabric', label: 'Kumaş (m)', numeric: true },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(cuttingPlanStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
  { key: 'createdAt', label: 'Oluşturma', filter: false },
]

/** Yeni plan için onaylı ya da üretimdeki bir iş emri seçtirir. */
function OrderPickerDialog({ onPick, onClose }: { onPick: (orderId: number) => void; onClose: () => void }) {
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const released = useGetProductionOrdersQuery({ page: 1, pageSize: 50, search: search || undefined, status: 'RELEASED' })
  const inProduction = useGetProductionOrdersQuery({ page: 1, pageSize: 50, search: search || undefined, status: 'IN_PRODUCTION' })
  const orders = [...(inProduction.data?.items ?? []), ...(released.data?.items ?? [])]

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setSearch(searchInput.trim())
  }

  return (
    <Modal title="Kesim föyü: sipariş seçin" onClose={onClose}>
      <div className="form">
        <form className="toolbar" onSubmit={handleSearch} role="search">
          <input
            type="search"
            placeholder="Sipariş no, ürün veya müşteri"
            aria-label="Sipariş ara"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            autoFocus
          />
          <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        </form>
        <ErrorAlert error={released.error ?? inProduction.error} />
        <div className="table-wrap">
          <table className="table table--compact">
            <thead>
              <tr>
                <th>Sipariş</th>
                <th>Müşteri</th>
                <th>Ürün</th>
                <th className="table__number">Adet</th>
                <th aria-label="İşlemler" />
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.id}>
                  <td>{order.orderNo}</td>
                  <td>{order.customerName}</td>
                  <td><code>{order.productCode}</code> {order.modelCode}</td>
                  <td className="table__number">{formatQuantity(order.totalQuantity)}</td>
                  <td className="table__actions">
                    <button type="button" className="button button--inline" onClick={() => onPick(order.id)}>Seç</button>
                  </td>
                </tr>
              ))}
              {orders.length === 0 && !released.isFetching && !inProduction.isFetching && (
                <tr><td colSpan={5} className="table__empty">Onaylı ya da üretimde sipariş bulunamadı.</td></tr>
              )}
            </tbody>
          </table>
        </div>
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
        </footer>
      </div>
    </Modal>
  )
}

export function CuttingPlansPage() {
  // Sipariş sayfasından gelindiğinde liste o siparişe süzülür.
  const [searchParams, setSearchParams] = useSearchParams()
  const orderId = Number(searchParams.get('orderId')) || undefined
  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const canManage = usePermission(Permissions.CuttingPlanManage)
  const navigate = useNavigate()
  const [picking, setPicking] = useState(false)
  const [creatingFor, setCreatingFor] = useState<number | null>(null)

  const { data, isLoading, isFetching, error } = useGetCuttingPlansQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    productionOrderId: orderId,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Kesim Föyleri</h1>

      {orderId && (
        <p className="alert alert--info">
          Yalnızca seçili siparişin kesim föyleri gösteriliyor.{' '}
          <button
            type="button"
            className="button"
            onClick={() => {
              setPage(1)
              setSearchParams({})
            }}
          >
            Tüm planları göster
          </button>
        </p>
      )}

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Plan no, sipariş no veya ürün"
          aria-label="Kesim föyü ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        {canManage && (
          <button
            type="button"
            className="button button--primary"
            onClick={() => (orderId ? setCreatingFor(orderId) : setPicking(true))}
          >
            Yeni kesim föyü
          </button>
        )}
      </form>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((plan) => (
                  <tr key={plan.id}>
                    <td><Link to={`/cutting-plans/${plan.id}`}><code>{plan.planNo}</code></Link></td>
                    <td><Link to={`/production-orders/${plan.productionOrderId}`}>{plan.orderNo}</Link></td>
                    <td>{plan.modelCode}</td>
                    <td>{plan.colorName}</td>
                    <td className="table__number">{formatMeters(plan.markerLength)}</td>
                    <td className="table__number">
                      {plan.actualPlies !== null && plan.actualPlies !== plan.plannedPlies
                        ? `${plan.actualPlies} / ${plan.plannedPlies}`
                        : plan.plannedPlies}
                    </td>
                    <td className="table__number">{formatQuantity(plan.cutQuantity ?? plan.plannedQuantity)}</td>
                    <td className="table__number">{formatMeters(plan.plannedFabric)}</td>
                    <td><CuttingPlanStatusBadge status={plan.status} /></td>
                    <td className="table__nowrap">{formatDateTimeShort(plan.createdAt)}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={columns.length} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}

      {picking && (
        <OrderPickerDialog
          onClose={() => setPicking(false)}
          onPick={(id) => {
            setPicking(false)
            setCreatingFor(id)
          }}
        />
      )}
      {creatingFor !== null && (
        <CuttingPlanFormDialog
          orderId={creatingFor}
          onClose={() => setCreatingFor(null)}
          onSaved={(plan) => void navigate(`/cutting-plans/${plan.id}`)}
        />
      )}
    </section>
  )
}
