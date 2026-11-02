import { useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { fieldErrors, isRequestError } from '../../api/envelope'
import { BrandMark } from '../../components/BrandMark'
import { useForgotPasswordMutation, useResetPasswordMutation } from './authApi'
import { LoginAside } from './LoginPage'

function AuthCard({ title, lead, children, onSubmit }: {
  title: string
  lead: string
  children: ReactNode
  onSubmit: (event: FormEvent) => void
}) {
  return (
    <main className="login">
      <LoginAside />
      <div className="login__panel">
        <form className="login__card" onSubmit={onSubmit} noValidate>
          <div className="login__brand">
            <BrandMark />
            <div>
              <h1>{title}</h1>
              <p>{lead}</p>
            </div>
          </div>
          {children}
        </form>
      </div>
    </main>
  )
}

/** "Şifremi unuttum": kullanıcı adı ya da e-posta girilir, kayıtlı adrese süreli sıfırlama bağlantısı gider. */
export function ForgotPasswordPage() {
  const [send, { isLoading, isSuccess, error }] = useForgotPasswordMutation()
  const [value, setValue] = useState('')
  const requestError = isRequestError(error) ? error : undefined

  return (
    <AuthCard
      title="Şifremi unuttum"
      lead="Kullanıcı adınızı ya da e-posta adresinizi girin."
      onSubmit={(event) => {
        event.preventDefault()
        void send({ userNameOrEmail: value.trim() })
      }}
    >
      {isSuccess ? (
        <p className="alert alert--info" role="status">
          Bilgiler bir hesapla eşleşiyorsa, hesaba kayıtlı e-posta adresine parola sıfırlama bağlantısı gönderildi.
          Bağlantı kısa süre geçerlidir. E-posta gelmezse sistem yöneticinize başvurun.
        </p>
      ) : (
        <>
          <label className="field">
            <span>Kullanıcı adı ya da e-posta</span>
            <input autoFocus autoComplete="username" value={value} onChange={(event) => setValue(event.target.value)}
              aria-invalid={Boolean(fieldErrors(error, 'userNameOrEmail'))} />
            {fieldErrors(error, 'userNameOrEmail')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
          {requestError && !requestError.details && <p className="alert alert--error" role="alert">{requestError.message}</p>}
          <button className="button button--primary" type="submit" disabled={isLoading || !value.trim()}>
            {isLoading ? 'Gönderiliyor…' : 'Sıfırlama bağlantısı gönder'}
          </button>
        </>
      )}
      <p className="login__footnote"><Link to="/login">← Giriş ekranına dön</Link></p>
    </AuthCard>
  )
}

/** E-postadaki bağlantının açtığı ekran: yeni parola belirlenir. */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const token = searchParams.get('token') ?? ''
  const [reset, { isLoading, isSuccess, error }] = useResetPasswordMutation()
  const [password, setPassword] = useState('')
  const [repeat, setRepeat] = useState('')
  const [attempted, setAttempted] = useState(false)
  const requestError = isRequestError(error) ? error : undefined
  const mismatch = attempted && password !== repeat

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    setAttempted(true)
    if (password === repeat) {
      void reset({ token, newPassword: password })
    }
  }

  return (
    <AuthCard title="Yeni parola" lead="Hesabınız için yeni bir parola belirleyin." onSubmit={handleSubmit}>
      {!token && <p className="alert alert--error" role="alert">Bağlantı eksik. E-postadaki bağlantıyı açın.</p>}
      {isSuccess ? (
        <p className="alert alert--info" role="status">
          Parolanız değiştirildi. Açık oturumlarınız kapatıldı; yeni parolanızla giriş yapabilirsiniz.
        </p>
      ) : (
        <>
          <label className="field">
            <span>Yeni parola</span>
            <input type="password" autoFocus autoComplete="new-password" value={password}
              onChange={(event) => setPassword(event.target.value)} aria-invalid={Boolean(fieldErrors(error, 'newPassword'))} />
            {fieldErrors(error, 'newPassword')?.map((message) => <small key={message} className="field__error">{message}</small>)}
          </label>
          <label className="field">
            <span>Yeni parola (tekrar)</span>
            <input type="password" autoComplete="new-password" value={repeat} onChange={(event) => setRepeat(event.target.value)}
              aria-invalid={mismatch} />
            {mismatch && <small className="field__error">Parolalar aynı değil.</small>}
          </label>
          {requestError && !requestError.details && <p className="alert alert--error" role="alert">{requestError.message}</p>}
          <button className="button button--primary" type="submit" disabled={isLoading || !token || !password}>
            {isLoading ? 'Kaydediliyor…' : 'Parolayı değiştir'}
          </button>
        </>
      )}
      <p className="login__footnote"><Link to="/login">← Giriş ekranına dön</Link></p>
    </AuthCard>
  )
}
