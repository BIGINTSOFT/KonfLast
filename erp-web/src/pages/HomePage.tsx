import { ExternalLink, LayoutDashboard, Pencil, Plus, StarOff } from 'lucide-react'
import { useState } from 'react'
import type { CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { ContextMenu } from '../components/ContextMenu'
import { ErrorAlert } from '../components/FormControls'
import { ProjectTasks, TaskList } from '../features/workspace/ProjectTasks'
import { ShortcutsDialog } from '../features/workspace/ShortcutsDialog'
import { useShortcutActions } from '../features/workspace/useShortcutActions'
import { useGetShortcutsQuery, useGetWorkTasksQuery } from '../features/workspace/workspaceApi'
import { useActiveProject } from '../features/system/activeProject'
import { usePermission } from '../hooks/usePermission'
import { moduleOf, navGroups, projectNavGroups, visibleNavGroups } from '../layouts/navigation'
import type { NavItem } from '../layouts/navigation'
import { moduleStyle, navIcon } from '../layouts/navPresentation'
import { resolveShortcuts } from '../layouts/tabs'
import { useWorkTabs } from '../layouts/workTabs'
import { useAppSelector } from '../store/hooks'
import { Permissions } from '../types/permissions'

/**
 * Ana sayfa: kullanıcının kendi seçtiği kısayollar, yetkili olduğu bekleyen işler (tıklayınca işin yapılacağı
 * ekran açılır). Kısayola sağ tıklanınca ekran yeni sekmede açılabilir.
 */
export function HomePage() {
  const user = useAppSelector((state) => state.auth.user)
  const workTabs = useWorkTabs()
  const canViewDashboard = usePermission(Permissions.DashboardView)
  const canViewDokuma = usePermission(Permissions.DokumaView)
  const canViewOrme = usePermission(Permissions.OrmeView)
  const canViewBoyahane = usePermission(Permissions.BoyahaneBatchView)
  const canCreateOrder = usePermission(Permissions.ProductionOrderCreate)
  // Sipariş düğmesi Konfeksiyon projesinindir; diğer projelerin işleri panolarından gelir.
  const { project } = useActiveProject()
  const konfeksiyon = project === 'Konfeksiyon'
  const tasks = useGetWorkTasksQuery(undefined, { pollingInterval: 60_000, skip: !konfeksiyon })
  const shortcuts = useGetShortcutsQuery()
  const [editingShortcuts, setEditingShortcuts] = useState(false)
  const [menu, setMenu] = useState<{ x: number; y: number; item: NavItem } | null>(null)
  const shortcutActions = useShortcutActions(workTabs.notify)
  // Yalnızca seçili projenin ve çekirdeğin ekranlarına giden kısayollar gösterilir ve düzenlenir; başka projenin
  // kısayolları kaydederken korunur.
  const groups = visibleNavGroups(navGroups, user?.permissions ?? [])
  const projectGroups = visibleNavGroups(projectNavGroups(navGroups, project), user?.permissions ?? [])
  const shortcutItems = resolveShortcuts(projectGroups, shortcuts.data ?? [])
  const shortcutPaths = shortcutItems.map((item) => item.path)
  const otherProjectPaths = resolveShortcuts(groups, shortcuts.data ?? []).map((item) => item.path).filter((path) => !shortcutPaths.includes(path))
  const moduleOfPath = (path: string) => moduleOf(groups.find((group) => group.items.some((item) => item.path === path)) ?? groups[0])
  const dashboardPath = project === 'Konfeksiyon' && canViewDashboard ? '/dashboard'
    : project === 'Dokuma' && canViewDokuma ? '/dokuma/dashboard'
    : project === 'Orme' && canViewOrme ? '/orme/dashboard'
    : project === 'Boyahane' && canViewBoyahane ? '/boyahane/dashboard'
    : null

  return (
    <section className="page">
      <header className="page__header">
        <div>
          <h1>Hoş geldiniz, {user?.fullName}</h1>
          <p className="muted">Kısayollarınız ve bekleyen işleriniz aşağıda. Bir ekranı ayrı sekmede açmak için sağ tıklayın.</p>
        </div>
        <div className="page__actions">
          {konfeksiyon && canCreateOrder && <Link to="/production-orders/new" className="button"><Plus aria-hidden="true" />Yeni iş emri</Link>}
          {dashboardPath && <Link to={dashboardPath} className="button button--primary"><LayoutDashboard aria-hidden="true" />Canlı Pano</Link>}
        </div>
      </header>

      <h2 className="section-title section-title--with-action">
        Kısayollarım
        <button type="button" className="button button--ghost" onClick={() => setEditingShortcuts(true)} disabled={!shortcuts.data}>
          <Pencil aria-hidden="true" />
          Düzenle
        </button>
      </h2>
      <ErrorAlert error={shortcuts.error} />
      {shortcuts.data && shortcutItems.length === 0 && (
        <p className="muted">
          {shortcuts.data.length === 0
            ? "Henüz kısayol eklemediniz. Sık kullandığınız ekranları buraya eklemek için Düzenle'ye tıklayın."
            : "Bu projenin ekranlarına kısayolunuz yok. Eklemek için Düzenle'ye tıklayın."}
        </p>
      )}
      {shortcutItems.length > 0 && (
        <nav className="shortcuts" aria-label="Kısayollarım">
          {shortcutItems.map((item) => {
            const ShortcutIcon = navIcon(item.path)
            return (
              <Link
                key={item.path}
                to={item.path}
                className="shortcut"
                onContextMenu={(event) => {
                  event.preventDefault()
                  setMenu({ x: event.clientX, y: event.clientY, item })
                }}
                style={{ '--module': moduleStyle(moduleOfPath(item.path)).color } as CSSProperties}
              >
                <span className="shortcut__icon"><ShortcutIcon aria-hidden="true" /></span>
                <span className="shortcut__label">{item.label}</span>
              </Link>
            )
          })}
        </nav>
      )}
      {menu && (
        <ContextMenu
          x={menu.x}
          y={menu.y}
          label={menu.item.label}
          onClose={() => setMenu(null)}
          items={[
            {
              key: 'new-tab', label: 'Yeni sekmede aç', icon: ExternalLink,
              disabled: !workTabs.canOpenInNewTab, onSelect: () => workTabs.openInNewTab(menu.item.path),
            },
            {
              key: 'remove', label: 'Kısayolu kaldır', icon: StarOff, separated: true,
              disabled: !shortcutActions.ready, onSelect: () => void shortcutActions.remove(menu.item),
            },
          ]}
        />
      )}
      {editingShortcuts && shortcuts.data && (
        <ShortcutsDialog groups={projectGroups} paths={shortcuts.data} keptPaths={otherProjectPaths} onClose={() => setEditingShortcuts(false)} />
      )}

      {konfeksiyon && <h2 className="section-title">Yapılacaklar</h2>}
      {konfeksiyon && <ErrorAlert error={tasks.error} />}
      {konfeksiyon && tasks.isLoading && <p role="status">Yükleniyor…</p>}
      {konfeksiyon && tasks.data && <TaskList tasks={tasks.data} />}
      <ProjectTasks project={project} />
    </section>
  )
}

export function NotFoundPage() {
  return (
    <section className="page">
      <h1>Sayfa bulunamadı</h1>
      <p className="muted">Aradığınız ekran mevcut değil veya taşınmış olabilir.</p>
    </section>
  )
}
