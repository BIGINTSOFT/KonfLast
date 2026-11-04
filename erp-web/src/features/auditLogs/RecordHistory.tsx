import { Fragment, useState } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Pagination } from '../../components/Pagination'
import { formatDateTime } from '../../utils/format'
import { ChangeDetails, actionPresentation } from './AuditLogsPage'
import { useGetAuditLogsQuery } from './auditLogsApi'

const PAGE_SIZE = 15

/** Sık değişen alanların ekrandaki adları; listede olmayan alan kendi adıyla görünür. */
const fieldLabels: Record<string, string> = {
  Status: 'Durum', DueDate: 'Termin', OrderDate: 'Tarih', Notes: 'Not', CustomerId: 'Müşteri', ProductId: 'Ürün',
  CustomerOrderNo: 'Müşteri sipariş no', MaterialWarehouseId: 'Malzeme deposu', OperationSequence: 'Operasyon sırası',
  OrderNo: 'Numara', SalesOrderId: 'Müşteri siparişi', Quantity: 'Adet', WarehouseId: 'Depo', MachineId: 'Makine',
  OperatorId: 'Operatör', Name: 'Ad', Code: 'Kod', IsActive: 'Aktif', Price: 'Fiyat', Currency: 'Para birimi',
}

/** Kullanıcı için anlamı olmayan teknik alanlar özet sütununda gösterilmez (ayrıntıda görünür). */
const technicalFields = new Set(['Id', 'IsDeleted', 'DeletedAt', 'DeletedBy', 'RowVersion', 'CreatedAt', 'CreatedBy', 'UpdatedAt', 'UpdatedBy'])

/** Kayıt sayfasının 'Geçmiş' sekmesi: bu kaydın değişiklik kayıtları, yeniden eskiye; satıra tıklayınca ayrıntı. */
export function RecordHistory({ entityName, entityId }: { entityName: string; entityId: number }) {
  const [page, setPage] = useState(1)
  const [open, setOpen] = useState<number | null>(null)
  const { data, error, isLoading } = useGetAuditLogsQuery({
    page,
    pageSize: PAGE_SIZE,
    entityName,
    entityId: String(entityId),
  })

  return (
    <>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && (
        <>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Zaman</th>
                  <th>Kullanıcı</th>
                  <th>İşlem</th>
                  <th>Değişen alanlar</th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((log) => {
                  const action = actionPresentation[log.action]
                  const fields = [...new Set([...Object.keys(log.oldValues ?? {}), ...Object.keys(log.newValues ?? {})])]
                    .filter((field) => !technicalFields.has(field))
                    .map((field) => fieldLabels[field] ?? field)
                  return (
                    <Fragment key={log.id}>
                      <tr className="stock-row" onClick={() => setOpen(open === log.id ? null : log.id)}>
                        <td>{formatDateTime(log.createdAt)}</td>
                        <td>{log.userName}</td>
                        <td><span className={`badge badge--${action.tone}`}>{action.label}</span></td>
                        <td className="muted">{fields.join(', ') || '—'}</td>
                      </tr>
                      {open === log.id && (
                        <tr className="table__detail-row">
                          <td colSpan={4}><ChangeDetails log={log} /></td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={4} className="table__empty">Bu kayıt için işlem kaydı yok.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPageChange={setPage} />
        </>
      )}
    </>
  )
}
