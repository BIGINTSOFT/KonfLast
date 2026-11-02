import { Plus, Search } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { ErrorAlert } from '../../components/FormControls'
import { GridHead } from '../../components/grid/GridHead'
import type { GridColumn } from '../../components/grid/GridHead'
import { useGrid } from '../../components/grid/useGrid'
import { Pagination } from '../../components/Pagination'
import { usePermission } from '../../hooks/usePermission'
import { useAppSelector } from '../../store/hooks'
import type { User } from '../../types/api'
import { Permissions } from '../../types/permissions'
import { formatDateTime } from '../../utils/format'
import { useGetRolesQuery } from '../roles/rolesApi'
import { inProject, useActiveProject } from '../system/activeProject'
import { DeleteUserDialog, ResetPasswordDialog, UserFormDialog } from './UserDialogs'
import { useGetUsersQuery } from './usersApi'
import { ClearFiltersButton } from '../../components/grid/ClearFiltersButton'

const PAGE_SIZE = 20

// Durum sütunu etkin / pasif alanına göre süzülür; kilitli kullanıcılar etkin sayılır.
const columns: GridColumn[] = [
  { key: 'userName', label: 'Kullanıcı adı' },
  { key: 'fullName', label: 'Ad soyad' },
  { label: 'Roller' },
  {
    key: 'isActive',
    label: 'Durum',
    filter: [
      { value: 'true', label: 'Aktif' },
      { value: 'false', label: 'Pasif' },
    ],
  },
  { key: 'lastLoginAt', label: 'Son giriş', filter: false },
]

// Parola sıfırlama ve silme onayı düzenleme panelinin üstünde açılır.
type Dialog =
  | { kind: 'create' }
  | { kind: 'edit'; user: User; overlay?: 'password' | 'delete' }

function statusOf(user: User): { label: string; tone: string } {
  if (!user.isActive) {
    return { label: 'Pasif', tone: 'neutral' }
  }
  return user.isLockedOut ? { label: 'Kilitli', tone: 'danger' } : { label: 'Aktif', tone: 'ok' }
}

export function UsersPage() {
  const { project } = useActiveProject()
  const { data: allRoles } = useGetRolesQuery()
  const foreignRoles = new Set(allRoles?.filter((role) => !inProject(role.moduleKey, project)).map((role) => role.code))
  const canManage = usePermission(Permissions.UserManage)
  const canResetPassword = usePermission(Permissions.UserResetPassword)
  const currentUserId = useAppSelector((state) => state.auth.user?.id)

  const grid = useGrid()
  const { page, setPage } = grid
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [dialog, setDialog] = useState<Dialog | null>(null)

  const { data, isLoading, isFetching, error } = useGetUsersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    ...grid.params,
  })

  const handleSearch = (event: FormEvent) => {
    event.preventDefault()
    setPage(1)
    setSearch(searchInput.trim())
  }

  return (
    <section className="page">
      <h1 className="visually-hidden">Kullanıcılar</h1>

      <form className="toolbar" onSubmit={handleSearch} role="search">
        <input
          type="search"
          placeholder="Kullanıcı adı veya ad soyad"
          aria-label="Kullanıcı ara"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
        />
        <button type="submit" className="button"><Search aria-hidden="true" />Ara</button>
        <ClearFiltersButton grid={grid} />
        <div className="toolbar__actions">
          {canManage && (
            <button type="button" className="button button--primary" onClick={() => setDialog({ kind: 'create' })}>
              <Plus aria-hidden="true" />
              Yeni kullanıcı
            </button>
          )}
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
                {data.items.map((user) => {
                  const status = statusOf(user)
                  return (
                    <tr
                      key={user.id}
                      className={canManage ? 'table__row--link' : undefined}
                      onClick={() => canManage && setDialog({ kind: 'edit', user })}
                    >
                      <td>
                        {canManage ? (
                          <button
                            type="button"
                            className="table__open"
                            onClick={(event) => {
                              event.stopPropagation()
                              setDialog({ kind: 'edit', user })
                            }}
                          >
                            <code>{user.userName}</code>
                          </button>
                        ) : (
                          <code>{user.userName}</code>
                        )}
                      </td>
                      <td>
                        {user.fullName}
                        {user.email && <div className="muted">{user.email}</div>}
                      </td>
                      <td>
                        <div className="chips">
                          {user.roles.filter((role) => !foreignRoles.has(role)).map((role) => (
                            <span key={role} className="chip">{role}</span>
                          ))}
                        </div>
                      </td>
                      <td><span className={`badge badge--${status.tone}`}>{status.label}</span></td>
                      <td>{formatDateTime(user.lastLoginAt)}</td>
                    </tr>
                  )
                })}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="table__empty">Kayıt bulunamadı.</td>
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

      {dialog?.kind === 'create' && <UserFormDialog onClose={() => setDialog(null)} />}
      {dialog?.kind === 'edit' && (
        <UserFormDialog
          user={dialog.user}
          onClose={() => setDialog(null)}
          onResetPassword={canResetPassword ? () => setDialog({ ...dialog, overlay: 'password' }) : undefined}
          // Kullanıcı kendi hesabını silemez.
          onDelete={dialog.user.id === currentUserId ? undefined : () => setDialog({ ...dialog, overlay: 'delete' })}
        />
      )}
      {dialog?.kind === 'edit' && dialog.overlay === 'password' && (
        <ResetPasswordDialog user={dialog.user} onClose={() => setDialog({ ...dialog, overlay: undefined })} />
      )}
      {dialog?.kind === 'edit' && dialog.overlay === 'delete' && (
        <DeleteUserDialog user={dialog.user} onClose={() => setDialog(null)} />
      )}
    </section>
  )
}
