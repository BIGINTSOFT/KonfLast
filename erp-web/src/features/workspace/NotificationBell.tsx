import { Bell } from 'lucide-react'
import { createElement, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { navIcon } from '../../layouts/navPresentation'
import { formatDateTime } from '../../utils/format'
import { useActiveProject } from '../system/activeProject'
import { useGetWorkTasksQuery } from './workspaceApi'

/** Sunucu yalnızca kullanıcının yetkili olduğu işleri döner; liste dakikada bir ve ilgili kayıtlar değiştikçe yenilenir. */
const POLL_MS = 60_000

/**
 * Üst çubuktaki bildirim zili: kullanıcının yapması beklenen işler. Onay bekleyen siparişler tek tek listelenir ve
 * tıklanınca o siparişin sayfası açılır; diğer işler (kart atanacak demetler, teslim alınacak mallar ...) adetleriyle
 * gösterilir ve işin yapılacağı ekranı açar. Rozet bekleyen toplam kayıt sayısıdır. Ana sayfadaki "Yapılacaklar"
 * ile aynı veridir.
 */
export function NotificationBell() {
  // Bekleyen işler Konfeksiyon modülünün kayıtlarıdır; modül kurulumda yoksa sorulmaz.
  const konfeksiyon = useActiveProject().project === 'Konfeksiyon'
  const { data: tasks = [], isError } = useGetWorkTasksQuery(undefined, { pollingInterval: POLL_MS, skip: !konfeksiyon })
  const [open, setOpen] = useState(false)
  const container = useRef<HTMLDivElement>(null)
  const total = tasks.reduce((sum, task) => sum + task.count, 0)
  // Kayıt bazında bildirimler üstte, adetle gösterilen işler altta.
  const records = tasks.filter((task) => (task.items?.length ?? 0) > 0)
  const groups = tasks.filter((task) => (task.items?.length ?? 0) === 0)

  // Dışarı tıklanınca ya da Esc ile kapanır.
  useEffect(() => {
    if (!open) {
      return
    }

    const close = (event: Event) => {
      if (event instanceof KeyboardEvent ? event.key === 'Escape' : !container.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    document.addEventListener('pointerdown', close)
    document.addEventListener('keydown', close)
    return () => {
      document.removeEventListener('pointerdown', close)
      document.removeEventListener('keydown', close)
    }
  }, [open])

  return (
    <div className="notifications" ref={container}>
      <button
        type="button"
        className="appbar__icon notifications__button"
        aria-label={total > 0 ? `Bekleyen işler: ${total}` : 'Bekleyen iş yok'}
        aria-haspopup="true"
        aria-expanded={open}
        title="Bekleyen işler"
        onClick={() => setOpen((current) => !current)}
      >
        <Bell aria-hidden="true" />
        {total > 0 && <span className="notifications__badge" aria-hidden="true">{total > 99 ? '99+' : total}</span>}
      </button>

      {open && (
        <div className="notifications__panel" role="dialog" aria-label="Bekleyen işler">
          <header className="notifications__header">
            Bekleyen işler
            {total > 0 && <span className="tabs__count">{total}</span>}
          </header>
          {isError && <p className="notifications__empty">Bekleyen işler yüklenemedi.</p>}
          {!isError && tasks.length === 0 && <p className="notifications__empty">Şu an bekleyen iş yok.</p>}
          {records.map((task) => (
            <div key={task.key} role="group" aria-label={task.title}>
              {task.items!.map((item) => (
                <Link key={item.path} to={item.path} className="notifications__item" onClick={() => setOpen(false)}>
                  <span className="notifications__icon">{createElement(navIcon(task.path.split('?')[0]), { 'aria-hidden': true })}</span>
                  <span className="notifications__body">
                    <strong>{item.title}</strong>
                    <span>
                      {[item.subtitle, item.by, item.at ? formatDateTime(item.at) : null].filter(Boolean).join(' · ')}
                    </span>
                  </span>
                </Link>
              ))}
              {task.count > task.items!.length && (
                <Link to={task.path} className="notifications__more" onClick={() => setOpen(false)}>
                  {task.title}: tümünü gör ({task.count})
                </Link>
              )}
            </div>
          ))}
          {records.length > 0 && groups.length > 0 && <p className="notifications__divider">Diğer bekleyen işler</p>}
          {groups.map((task) => (
            <Link key={task.key} to={task.path} className="notifications__item" onClick={() => setOpen(false)}>
              <span className="notifications__icon">{createElement(navIcon(task.path.split('?')[0]), { 'aria-hidden': true })}</span>
              <span className="notifications__body">
                <strong>{task.title}</strong>
                <span>{task.description}</span>
              </span>
              <span className="notifications__count">{task.count}</span>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
