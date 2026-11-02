import { ChevronDown, ChevronRight, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { normalizeSearch } from '../../layouts/navigation'
import type { Permission, Role } from '../../types/api'
import { Permissions } from '../../types/permissions'
import { useGetPermissionsQuery, useGetRolesQuery, useUpdateRolePermissionsMutation } from './rolesApi'
import { inProject, useActiveProject } from '../system/activeProject'

// Yönetici rolü her zaman tüm yetkilere sahiptir; backend de değiştirmeye izin vermez.
const LOCKED_ROLE = 'ADMIN'

/** Yetki gruplarının ekrandaki adları; listede olmayan grup kendi koduyla gösterilir. */
const moduleLabels: Record<string, string> = {
  Dashboard: 'Canlı pano',
  ProductionOrder: 'Üretim siparişleri',
  Bundle: 'Demetler',
  Production: 'Üretim takibi',
  WorkSession: 'Çalışma oturumları',
  Quality: 'Kalite',
  ProductionLine: 'Bantlar ve planlama',
  LinePlan: 'Bant planları',
  ProductionRoute: 'Operasyon rotaları',
  Report: 'Raporlar',
  Payroll: 'Ücret ve prim',
  Costing: 'Maliyet',
  SalesOrder: 'Müşteri siparişleri',
  Pricing: 'Fiyat ve kur',
  ExchangeRate: 'Döviz kurları',
  Purchase: 'Satın alma',
  Stock: 'Stok ve depo',
  Warehouse: 'Depo tanımları',
  Shipment: 'Sevkiyat',
  MasterData: 'Ana veriler',
  Machine: 'Makineler',
  Operator: 'Personel',
  Tablet: 'Tabletler',
  RfidCard: 'RFID kartları',
  Floor: 'Tablet (saha) işlemleri',
  User: 'Kullanıcılar',
  Role: 'Roller ve yetkiler',
  ApprovalFlow: 'Onay akışları',
  AuditLog: 'İşlem kayıtları',
  Integration: 'Entegrasyonlar',
}

const moduleLabel = (module: string) => moduleLabels[module] ?? module

/** Rolün yetkili olduğu modüller, menü sırasıyla (yetki kodunun ilk bölümü modüldür). */
function modulesOf(permissions: string[]): string[] {
  const modules = new Set(permissions.map((code) => code.split('.')[0]))
  const order = Object.keys(moduleLabels)
  return [...modules].sort((a, b) => (order.indexOf(a) + 1 || 999) - (order.indexOf(b) + 1 || 999))
}

/** Yetkiler modüllere gruplanır; modüller menü sırasıyla, her modülde görüntüleme yetkisi önce gelir. */
function groupByModule(permissions: Permission[]): [string, Permission[]][] {
  const groups = new Map<string, Permission[]>()
  for (const permission of permissions) {
    groups.set(permission.module, [...(groups.get(permission.module) ?? []), permission])
  }
  const order = Object.keys(moduleLabels)
  const rank = (module: string) => (order.indexOf(module) + 1 || order.length + 1)
  const viewFirst = (permission: Permission) => (permission.code.endsWith('.View') ? 0 : 1)
  return [...groups.entries()]
    .sort(([a], [b]) => rank(a) - rank(b))
    .map(([module, items]) => [module, [...items].sort((a, b) => viewFirst(a) - viewFirst(b))])
}

/** Arama: yetkinin açıklamasında, kodunda ya da modülünün adında geçen her sözcük aranır. */
function matchesSearch(permission: Permission, words: string[]): boolean {
  const text = normalizeSearch(`${permission.description} ${permission.code} ${moduleLabel(permission.module)}`)
  return words.every((word) => text.includes(word))
}

function RolePermissionsDialog({ role, onClose }: { role: Role; onClose: () => void }) {
  const { data: permissions, isLoading: isLoadingPermissions, error: loadError } = useGetPermissionsQuery()
  const [updatePermissions, { isLoading, error }] = useUpdateRolePermissionsMutation()
  const [selected, setSelected] = useState<string[]>(role.permissions)
  const [search, setSearch] = useState('')
  // Gruplar kapalı başlar: rolün hangi modüllerde ne kadar yetkisi olduğu sayılardan tek bakışta görülür.
  const [expanded, setExpanded] = useState<string[]>([])

  const words = normalizeSearch(search).split(/\s+/).filter(Boolean)
  const searching = words.length > 0
  // Başka projenin yetkileri gösterilmez; kaydederken korunur (seçim listesinde kalır).
  const { project } = useActiveProject()
  const projectPermissions = permissions?.filter((permission) => inProject(permission.moduleKey, project))
  const groups = projectPermissions
    ? groupByModule(projectPermissions)
        .map(([module, items]) => [module, items, searching ? items.filter((item) => matchesSearch(item, words)) : items] as const)
        .filter(([, , visible]) => visible.length > 0)
    : []

  const toggle = (code: string) =>
    setSelected((current) => (current.includes(code) ? current.filter((c) => c !== code) : [...current, code]))

  // Modül satırındaki kutu, modülün (arama yapılıyorsa yalnızca bulunan) yetkilerini birlikte verir ya da kaldırır.
  const toggleModule = (items: readonly Permission[], on: boolean) => {
    const codes = items.map((permission) => permission.code)
    setSelected((current) => (on ? [...new Set([...current, ...codes])] : current.filter((c) => !codes.includes(c))))
  }

  const toggleExpanded = (module: string) =>
    setExpanded((current) => (current.includes(module) ? current.filter((m) => m !== module) : [...current, module]))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await updatePermissions({ id: role.id, permissions: selected })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Yetkileri düzenle: ${role.name}`} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={(event) => void handleSubmit(event)}>
        {isLoadingPermissions && <p role="status">Yükleniyor…</p>}
        <ErrorAlert error={loadError} />

        {permissions && (
          <>
            <div className="permission-search">
              <Search aria-hidden="true" />
              <input
                type="search"
                placeholder="Yetki ya da modül ara"
                aria-label="Yetki ara"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                autoFocus
              />
            </div>

            <div className="permission-bar">
              <span>
                <strong>{projectPermissions?.filter((permission) => selected.includes(permission.code)).length}</strong> / {projectPermissions?.length} yetki seçili
              </span>
              {!searching && (
                <>
                  <button type="button" className="button button--tiny" onClick={() => setExpanded(groups.map(([module]) => module))}>
                    Tümünü aç
                  </button>
                  <button type="button" className="button button--tiny" onClick={() => setExpanded([])}>
                    Tümünü kapat
                  </button>
                </>
              )}
              <button type="button" className="button button--tiny" onClick={() => setSelected(permissions.map((p) => p.code))}>
                Tümünü seç
              </button>
              <button type="button" className="button button--tiny" onClick={() => setSelected([])}>
                Temizle
              </button>
            </div>

            <div className="permission-list">
              {groups.length === 0 && <p className="permission-list__empty muted">Aramaya uyan yetki yok.</p>}
              {groups.map(([module, items, visible]) => {
                const count = items.filter((permission) => selected.includes(permission.code)).length
                const visibleCount = visible.filter((permission) => selected.includes(permission.code)).length
                // Arama sırasında bulunan yetkiler hep görünür; aramadan çıkınca grup eski açık / kapalı haline döner.
                const open = searching || expanded.includes(module)
                const Chevron = open ? ChevronDown : ChevronRight
                const panelId = `permission-group-${module}`
                return (
                  <section key={module} className="permission-group" aria-label={moduleLabel(module)}>
                    <div className="permission-group__head">
                      <button
                        type="button"
                        className="permission-group__toggle"
                        aria-expanded={open}
                        aria-controls={panelId}
                        disabled={searching}
                        onClick={() => toggleExpanded(module)}
                        title={open ? 'Daralt' : 'Genişlet'}
                      >
                        <Chevron aria-hidden="true" />
                      </button>
                      <input
                        type="checkbox"
                        aria-label={`${moduleLabel(module)}: tümü`}
                        checked={visibleCount === visible.length}
                        ref={(input) => {
                          if (input) {
                            input.indeterminate = visibleCount > 0 && visibleCount < visible.length
                          }
                        }}
                        onChange={(event) => toggleModule(visible, event.target.checked)}
                      />
                      <button
                        type="button"
                        className="permission-group__title"
                        disabled={searching}
                        onClick={() => toggleExpanded(module)}
                      >
                        {moduleLabel(module)}
                      </button>
                      <span className="permission-group__count">{count} / {items.length}</span>
                    </div>
                    {open && (
                      <ul id={panelId} className="permission-group__items">
                        {visible.map((permission) => (
                          <li key={permission.code}>
                            <label className="permission-item" title={permission.code}>
                              <input
                                type="checkbox"
                                checked={selected.includes(permission.code)}
                                onChange={() => toggle(permission.code)}
                              />
                              <span>{permission.description}</span>
                            </label>
                          </li>
                        ))}
                      </ul>
                    )}
                  </section>
                )
              })}
            </div>
            <p className="muted">Değişiklik, bu role sahip kullanıcılar için hemen geçerli olur.</p>
          </>
        )}

        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading || !permissions}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

export function RolesPage() {
  const canManage = usePermission(Permissions.RoleManage)
  const { data: allRoles, isLoading, error } = useGetRolesQuery()
  const { data: permissions } = useGetPermissionsQuery()
  const { project } = useActiveProject()
  const [editing, setEditing] = useState<Role | null>(null)
  const roles = allRoles?.filter((role) => inProject(role.moduleKey, project))
  // Roldeki sayılar ve modüller yalnızca seçili projenin ve çekirdeğin yetkilerini gösterir.
  const shown = new Set(permissions?.filter((permission) => inProject(permission.moduleKey, project)).map((permission) => permission.code))
  const shownPermissions = (role: Role) => role.permissions.filter((code) => shown.has(code))

  return (
    <section className="page">
      <h1 className="visually-hidden">Roller ve Yetkiler</h1>

      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />

      {roles && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Rol</th>
                <th>Kod</th>
                <th>Yetkili modüller</th>
                <th className="table__number">Yetki sayısı</th>
              </tr>
            </thead>
            <tbody>
              {roles.map((role) => {
                // Yönetici rolü her zaman tüm yetkilere sahiptir; düzenlenemez.
                const editable = canManage && role.code !== LOCKED_ROLE
                return (
                  <tr
                    key={role.id}
                    className={editable ? 'table__row--link' : undefined}
                    onClick={() => editable && setEditing(role)}
                  >
                    <td>
                      {editable ? (
                        <button
                          type="button"
                          className="table__open"
                          onClick={(event) => {
                            event.stopPropagation()
                            setEditing(role)
                          }}
                        >
                          {role.name}
                        </button>
                      ) : (
                        role.name
                      )}
                    </td>
                    <td><code>{role.code}</code></td>
                    <td>
                      {role.code === LOCKED_ROLE ? (
                        <span className="muted">Tüm modüller</span>
                      ) : (
                        <div className="chips">
                          {modulesOf(shownPermissions(role)).map((module) => (
                            <span key={module} className="chip">{moduleLabel(module)}</span>
                          ))}
                        </div>
                      )}
                    </td>
                    <td className="table__number">{shownPermissions(role).length}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {editing && <RolePermissionsDialog role={editing} onClose={() => setEditing(null)} />}
    </section>
  )
}
