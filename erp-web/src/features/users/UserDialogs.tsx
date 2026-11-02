import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { CheckboxList, ErrorAlert, TextField } from '../../components/FormControls'
import { Modal } from '../../components/Modal'
import { usePermission } from '../../hooks/usePermission'
import { Permissions } from '../../types/permissions'
import type { User } from '../../types/api'
import { useGetRolesQuery } from '../roles/rolesApi'
import { inProject, useActiveProject } from '../system/activeProject'
import {
  useCreateUserMutation,
  useDeleteUserMutation,
  useResetUserPasswordMutation,
  useUpdateUserMutation,
} from './usersApi'

interface DialogProps {
  onClose: () => void
}

interface UserFormProps extends DialogProps {
  user?: User
  /** Düzenlemede formun alt çubuğundaki ek işlemler; verilmeyen düğme gösterilmez. */
  onResetPassword?: () => void
  onDelete?: () => void
}

/** Yeni kullanıcı (user verilmezse) veya mevcut kullanıcıyı düzenleme formu; sağdan açılan panelde. */
export function UserFormDialog({ user, onClose, onResetPassword, onDelete }: UserFormProps) {
  const { data: allRoles } = useGetRolesQuery()
  const { project } = useActiveProject()
  // Başka projenin rolleri listelenmez; kullanıcıda varsa kaydederken korunur (seçim listesinde kalır).
  const roles = allRoles?.filter((role) => inProject(role.moduleKey, project))
  // Rol atamak ayrı yetkidir; yetkisi olmayan kullanıcı rolleri görür ama değiştiremez.
  const canAssignRoles = usePermission(Permissions.UserAssignRoles)
  const [createUser, createState] = useCreateUserMutation()
  const [updateUser, updateState] = useUpdateUserMutation()
  const { isLoading, error } = user ? updateState : createState

  const [userName, setUserName] = useState('')
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [email, setEmail] = useState(user?.email ?? '')
  const [password, setPassword] = useState('')
  const [isActive, setIsActive] = useState(user?.isActive ?? true)
  const [selectedRoles, setSelectedRoles] = useState<string[]>(user?.roles ?? [])

  const toggleRole = (code: string) =>
    setSelectedRoles((current) => (current.includes(code) ? current.filter((c) => c !== code) : [...current, code]))

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const common = { fullName, email: email.trim() || null, roles: selectedRoles }
    const result = user
      ? await updateUser({ id: user.id, isActive, ...common })
      : await createUser({ userName, password, ...common })

    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={user ? `Kullanıcıyı düzenle: ${user.userName}` : 'Yeni kullanıcı'} onClose={onClose} variant="drawer">
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        {!user && (
          <TextField
            label="Kullanıcı adı"
            value={userName}
            onChange={(event) => setUserName(event.target.value)}
            errors={fieldErrors(error, 'userName')}
            autoComplete="off"
            autoFocus
          />
        )}
        <TextField
          label="Ad soyad"
          value={fullName}
          onChange={(event) => setFullName(event.target.value)}
          errors={fieldErrors(error, 'fullName')}
        />
        <TextField
          label="E-posta (isteğe bağlı)"
          type="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          errors={fieldErrors(error, 'email')}
        />
        {!user && (
          <TextField
            label="Parola"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            errors={fieldErrors(error, 'password')}
            autoComplete="new-password"
          />
        )}

        <CheckboxList legend="Roller">
          {roles?.map((role) => (
            <label key={role.code} className="checkbox">
              <input
                type="checkbox"
                checked={selectedRoles.includes(role.code)}
                disabled={!canAssignRoles}
                onChange={() => toggleRole(role.code)}
              />
              {role.name}
            </label>
          ))}
          {!canAssignRoles && <small className="field__hint">Rol atama yetkiniz yok.</small>}
          {fieldErrors(error, 'roles')?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </CheckboxList>

        {user && (
          <label className="checkbox">
            <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
            Hesap aktif
          </label>
        )}

        <ErrorAlert error={error} fields={['userName', 'fullName', 'email', 'password', 'roles']} />

        <footer className="form__actions">
          {onDelete && (
            <button type="button" className="button button--danger-ghost form__actions-start" onClick={onDelete}>
              Sil
            </button>
          )}
          {onResetPassword && (
            <button type="button" className={onDelete ? 'button' : 'button form__actions-start'} onClick={onResetPassword}>
              Parolayı sıfırla
            </button>
          )}
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Kaydet'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

export function ResetPasswordDialog({ user, onClose }: DialogProps & { user: User }) {
  const [resetPassword, { isLoading, error }] = useResetUserPasswordMutation()
  const [newPassword, setNewPassword] = useState('')

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault()
    const result = await resetPassword({ id: user.id, newPassword })
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title={`Parolayı sıfırla: ${user.userName}`} onClose={onClose}>
      <form className="form" onSubmit={(event) => void handleSubmit(event)} noValidate>
        <p className="muted">
          Kullanıcının tüm açık oturumları kapanır ve varsa hesap kilidi kalkar.
        </p>
        <TextField
          label="Yeni parola"
          type="password"
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
          errors={fieldErrors(error, 'newPassword')}
          autoComplete="new-password"
          autoFocus
        />
        <ErrorAlert error={error} fields={['newPassword']} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Parolayı sıfırla'}
          </button>
        </footer>
      </form>
    </Modal>
  )
}

export function DeleteUserDialog({ user, onClose }: DialogProps & { user: User }) {
  const [deleteUser, { isLoading, error }] = useDeleteUserMutation()

  const handleDelete = async () => {
    const result = await deleteUser(user.id)
    if (!('error' in result)) {
      onClose()
    }
  }

  return (
    <Modal title="Kullanıcıyı sil" onClose={onClose}>
      <div className="form">
        <p>
          <strong>{user.fullName}</strong> ({user.userName}) silinecek ve açık oturumları kapatılacak.
          Geçmiş işlem kayıtları korunur.
        </p>
        <ErrorAlert error={error} />
        <footer className="form__actions">
          <button type="button" className="button" onClick={onClose}>Vazgeç</button>
          <button type="button" className="button button--danger" disabled={isLoading} onClick={() => void handleDelete()}>
            {isLoading ? 'Siliniyor…' : 'Sil'}
          </button>
        </footer>
      </div>
    </Modal>
  )
}
