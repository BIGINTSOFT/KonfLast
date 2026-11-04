import { Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTimeShort, formatQuantity } from '../../utils/format'
import { AssignCardDialog } from '../rfid/CardDialogs'
import { BundleStatusBadge } from './BundleBadges'
import { bundleStatusPresentation } from './bundlePresentation'
import { useGetBundlesQuery } from './bundlesApi'
import type { BundleListItem, BundleStatus } from './bundlesApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

/** Kart atanamayan (kapanmış) demet durumları; son kararı sunucu verir. */
const CLOSED: BundleStatus[] = ['COMPLETED', 'CANCELLED', 'CLOSED']

const columns: GridColumn[] = [
  { key: 'bundleNo', label: 'Demet no' },
  { key: 'orderNo', label: 'İş emri' },
  { key: 'modelCode', label: 'Model' },
  { key: 'colorName', label: 'Renk' },
  { key: 'sizeName', label: 'Beden' },
  { key: 'quantity', label: 'Adet', numeric: true },
  { key: 'completedOperations', label: 'Operasyon', filter: 'number' },
  { key: 'rfidUid', label: 'Kart' },
  {
    key: 'status',
    label: 'Durum',
    filter: Object.entries(bundleStatusPresentation).map(([value, { label }]) => ({ value, label })),
  },
  { key: 'createdAt', label: 'Oluşturma', filter: false },
]

export function BundlesPage() {
  // Sipariş sayfasından "demetleri gör" ile gelindiğinde liste o siparişe süzülür.
  const [searchParams, setSearchParams] = useSearchParams()
  const orderId = Number(searchParams.get('orderId')) || undefined

  // Ana sayfadaki "yapılacaklar"dan gelinen durum, durum sütununun süzgeciyle açılır.
  const urlStatus = searchParams.get('status') ?? ''
  const grid = useGrid(undefined, urlStatus in bundleStatusPresentation ? { status: urlStatus } : undefined)
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const canManage = usePermission(Permissions.BundleCreate)
  const [assigning, setAssigning] = useState<BundleListItem | null>(null)

  const { data, isLoading, isFetching, error } = useGetBundlesQuery({
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
      <h1 className="visually-hidden">Demetler</h1>

      {orderId && (
        <p className="alert alert--info">
          Yalnızca seçili siparişin demetleri gösteriliyor.{' '}
          <Link to={`/bundles/labels?orderId=${orderId}`} className="button">Etiketleri yazdır</Link>{' '}
          <button
            type="button"
            className="button"
            onClick={() => {
              setPage(1)
              setSearchParams({})
            }}
          >
            Tüm demetleri göster
          </button>
        </p>
      )}

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Demet no veya sipariş no"
          aria-label="Demet ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
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
                {data.items.map((bundle) => (
                  <tr key={bundle.id}>
                    <td><Link to={`/bundles/${bundle.id}`}><code>{bundle.bundleNo}</code></Link></td>
                    <td><Link to={`/production-orders/${bundle.productionOrderId}`}>{bundle.orderNo}</Link></td>
                    <td>{bundle.modelCode}</td>
                    <td>{bundle.colorName}</td>
                    <td>{bundle.sizeName}</td>
                    <td className="table__number">{formatQuantity(bundle.quantity)}</td>
                    <td>{bundle.completedOperations} / {bundle.totalOperations}</td>
                    <td>
                      {bundle.rfidUid ? (
                        <code>{bundle.rfidUid}</code>
                      ) : canManage && !CLOSED.includes(bundle.status) ? (
                        <button type="button" className="button button--inline" onClick={() => setAssigning(bundle)}>Kart ata</button>
                      ) : (
                        '—'
                      )}
                    </td>
                    <td><BundleStatusBadge status={bundle.status} /></td>
                    <td className="table__nowrap">{formatDateTimeShort(bundle.createdAt)}</td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={10} className="table__empty">Kayıt bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={setPage}
          />
        </>
      )}

      {assigning && (
        <AssignCardDialog
          owner="bundles"
          id={assigning.id}
          subject={`Demet ${assigning.bundleNo}`}
          onClose={() => setAssigning(null)}
        />
      )}
    </section>
  )
}
