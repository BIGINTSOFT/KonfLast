import { useState } from 'react'
import type { FormEvent } from 'react'
import { fieldErrors } from '../../api/envelope'
import { ErrorAlert, TextField } from '../../components/FormControls'
import { useChangePasswordMutation } from './authApi'

export function ChangePasswordPage() {
  const [changePassword, { isLoading, error }] = useChangePasswordMutation()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    // Başarılı olursa oturum kapanır ve giriş ekranına dönülür.
    void changePassword({ currentPassword, newPassword })
  }

  return (
    <section className="page page--narrow">
      <h1>Parola değiştir</h1>
      <p className="muted">Parolanız değişince tüm cihazlardaki oturumlarınız kapanır.</p>

      <form className="form card" onSubmit={handleSubmit} noValidate>
        <TextField
          label="Mevcut parola"
          type="password"
          autoComplete="current-password"
          value={currentPassword}
          onChange={(event) => setCurrentPassword(event.target.value)}
          errors={fieldErrors(error, 'currentPassword')}
        />
        <TextField
          label="Yeni parola"
          type="password"
          autoComplete="new-password"
          value={newPassword}
          onChange={(event) => setNewPassword(event.target.value)}
          errors={fieldErrors(error, 'newPassword')}
        />
        <ErrorAlert error={error} fields={['currentPassword', 'newPassword']} />
        <footer className="form__actions">
          <button type="submit" className="button button--primary" disabled={isLoading}>
            {isLoading ? 'Kaydediliyor…' : 'Parolayı değiştir'}
          </button>
        </footer>
      </form>
    </section>
  )
}
