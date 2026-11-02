import { Search } from 'lucide-react'
import { Fragment, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import {
  useGetIntegrationMessageQuery,
  useGetIntegrationMessagesQuery,
  useGetIntegrationOverviewQuery,
  useRetryIntegrationDeliveryMutation,
} from './integrationsApi'
import type { IntegrationDeliveryStatus, IntegrationOverview } from './integrationsApi'
import {
  channelEvents,
  deliveryStatusPresentation,
  entityLink,
  eventLabel,
  messageState,
} from './integrationsPresentation'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 25

function Channels({ overview }: { overview: IntegrationOverview }) {
  if (overview.channels.length === 0) {
    return (
      <p className="alert alert--info">
        Tanımlı kanal yok. Olaylar yine kaydedilir ve API&apos;den okunabilir; bir dış sisteme gönderilmesi için sunucu
        ayarlarına (<code>Integrations:Webhooks</code>) bir adres eklenmelidir.
      </p>
    )
  }

  return (
    <div className="cards">
      {overview.channels.map((channel) => (
        <article key={channel.key} className="card">
          <h2>{channel.name}</h2>
          <dl className="status-list">
            <div><dt>Hedef</dt><dd>{channel.target ?? '—'}</dd></div>
            <div><dt>Olaylar</dt><dd>{channelEvents(overview.eventTypes, channel.eventTypes)}</dd></div>
            <div><dt>Ulaşan</dt><dd>{channel.succeededCount}</dd></div>
            <div><dt>Kuyrukta</dt><dd>{channel.pendingCount}</dd></div>
            <div><dt>Gönderilemeyen</dt><dd>{channel.failedCount}</dd></div>
          </dl>
          {!channel.isConfigured && (
            <p className="alert alert--error" role="alert">
              Bu kanal sunucu ayarlarında tanımlı değil ya da kapalı; teslimleri gönderilemez.
            </p>
          )}
        </article>
      ))}
    </div>
  )
}

function MessageDetails({ id, canManage }: { id: number; canManage: boolean }) {
  const { data: message, error, isLoading } = useGetIntegrationMessageQuery(id)
  const [retry, retryState] = useRetryIntegrationDeliveryMutation()

  if (isLoading) {
    return <p role="status">Yükleniyor…</p>
  }

  if (error || !message) {
    return <ErrorAlert error={error} />
  }

  return (
    <div className="audit-details">
      <ErrorAlert error={retryState.error} />
      {message.deliveries.length === 0 ? (
        <p className="muted">Bu olay oluştuğunda onu dinleyen bir kanal tanımlı değildi; olay yalnızca kayıtlıdır.</p>
      ) : (
        <table className="table table--compact">
          <thead>
            <tr>
              <th>Kanal</th>
              <th>Durum</th>
              <th className="table__number">Deneme</th>
              <th>Son deneme</th>
              <th>Sıradaki deneme</th>
              <th>Son hata</th>
              {canManage && <th aria-label="İşlem" />}
            </tr>
          </thead>
          <tbody>
            {message.deliveries.map((delivery) => {
              const status = deliveryStatusPresentation[delivery.status]
              return (
                <tr key={delivery.id}>
                  <td>{delivery.channelName}</td>
                  <td>
                    <span className={`badge badge--${status.tone}`}>
                      <span aria-hidden="true">{status.icon}</span> {status.label}
                    </span>
                  </td>
                  <td className="table__number">{delivery.attemptCount}</td>
                  <td>{delivery.lastAttemptAt ? formatDateTime(delivery.lastAttemptAt) : '—'}</td>
                  <td>{delivery.nextAttemptAt ? formatDateTime(delivery.nextAttemptAt) : '—'}</td>
                  <td>{delivery.lastError ?? '—'}</td>
                  {canManage && (
                    <td className="table__actions">
                      {delivery.canRetry && (
                        <button
                          type="button"
                          className="button"
                          disabled={retryState.isLoading}
                          onClick={() => {
                            if (
                              delivery.status !== 'SUCCEEDED'
                              || window.confirm('Bu olay hedefe daha önce ulaştı. Yeniden gönderilsin mi?')
                            ) {
                              void retry(delivery.id)
                            }
                          }}
                        >
                          Yeniden gönder
                        </button>
                      )}
                    </td>
                  )}
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
      <details>
        <summary>Gönderilen veri</summary>
        <pre className="preserve-lines">{JSON.stringify(message.data, null, 2)}</pre>
      </details>
      <p className="muted">Olay kimliği: <code>{message.eventId}</code> · Oluşturan: {message.createdBy}</p>
    </div>
  )
}

/** Dış sistemlere duyurulan iş olayları ve kanallara teslim durumu. */
export function IntegrationsPage() {
  const canManage = usePermission(Permissions.IntegrationManage)

  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [deliveryStatus, setDeliveryStatus] = useState<IntegrationDeliveryStatus | ''>('')
  const [expandedId, setExpandedId] = useState<number | null>(null)

  const overview = useGetIntegrationOverviewQuery()
  const messages = useGetIntegrationMessagesQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    deliveryStatus: deliveryStatus || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setExpandedId(null)
    setSearch(searchInput.trim())
  }

  const resetPage = <T,>(setter: (value: T) => void) => (value: T) => {
    setPage(1)
    setExpandedId(null)
    setter(value)
  }

  const data = messages.data

  // Teslim durumu kanallara göre hesaplandığı için sütundan süzülemez; araç çubuğundaki seçimle süzülür.
  const columns: GridColumn[] = [
    { key: 'id', label: 'Sıra', numeric: true },
    { key: 'occurredAt', label: 'Zaman', filter: false },
    {
      key: 'eventType',
      label: 'Olay',
      filter: (overview.data?.eventTypes ?? []).map((type) => ({ value: `=${type.type}`, label: type.label })),
    },
    { key: 'reference', label: 'Kayıt' },
    { label: 'Teslim' },
    { label: '', ariaLabel: 'Ayrıntı' },
  ]

  return (
    <section className="page">
      <h1 className="visually-hidden">Entegrasyonlar</h1>

      <ErrorAlert error={overview.error} />
      {overview.data && !overview.data.dispatchEnabled && (
        <p className="alert alert--error" role="alert">
          Arka plan gönderimi sunucu ayarlarında kapalı (<code>Integrations:DispatchEnabled</code>); olaylar kaydediliyor
          ama gönderilmiyor.
        </p>
      )}
      {overview.data && <Channels overview={overview.data} />}

      <h2 className="section-title">Olaylar</h2>
      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Sevkiyat, sipariş ya da irsaliye no"
          aria-label="Olay ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <select
          aria-label="Teslim durumu filtresi"
          value={deliveryStatus}
          onChange={(event) => resetPage(setDeliveryStatus)(event.target.value as IntegrationDeliveryStatus | '')}
        >
          <option value="">Tüm durumlar</option>
          {Object.entries(deliveryStatusPresentation).map(([value, { label }]) => (
            <option key={value} value={value}>{label}</option>
          ))}
        </select>
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          <button
            type="button"
            className="button"
            disabled={messages.isFetching || overview.isFetching}
            onClick={() => {
              void overview.refetch()
              void messages.refetch()
            }}
          >
            Yenile
          </button>
        </div>
      </form>

      {messages.isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={messages.error} />

      {data && (
        <>
          <div className="table-wrap" aria-busy={messages.isFetching}>
            <table className="table">
              <GridHead columns={columns} grid={grid} />
              <tbody>
                {data.items.map((message) => {
                  const state = messageState(message)
                  const link = entityLink(message.entityName, message.entityId)
                  const reference = message.reference ?? `${message.entityName} ${message.entityId}`
                  const expanded = expandedId === message.id
                  return (
                    <Fragment key={message.id}>
                      <tr>
                        <td className="table__number">{message.id}</td>
                        <td>{formatDateTime(message.occurredAt)}</td>
                        <td>{eventLabel(overview.data?.eventTypes, message.eventType)}</td>
                        <td>{link ? <Link to={link}>{reference}</Link> : reference}</td>
                        <td>
                          <span className={`badge badge--${state.tone}`}>
                            <span aria-hidden="true">{state.icon}</span> {state.label}
                          </span>
                        </td>
                        <td className="table__actions">
                          <button
                            type="button"
                            className="button"
                            aria-expanded={expanded}
                            onClick={() => setExpandedId(expanded ? null : message.id)}
                          >
                            {expanded ? 'Gizle' : 'Ayrıntı'}
                          </button>
                        </td>
                      </tr>
                      {expanded && (
                        <tr>
                          <td colSpan={6}><MessageDetails id={message.id} canManage={canManage} /></td>
                        </tr>
                      )}
                    </Fragment>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={6} className="table__empty">Olay bulunamadı.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
          <Pagination
            page={data.page}
            totalPages={data.totalPages}
            totalCount={data.totalCount}
            onPageChange={(next) => {
              setExpandedId(null)
              setPage(next)
            }}
          />
        </>
      )}
    </section>
  )
}
