import { ClipboardList, Eye, EyeOff, Factory, FileChartColumn, LockKeyhole, LogIn, ShieldCheck, UserRound } from 'lucide-react'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { isRequestError } from '../../api/envelope'
import { BrandMark } from '../../components/BrandMark'
import { tokenStorage } from '../../services/tokenStorage'
import { useAppSelector } from '../../store/hooks'
import { useGetSystemModulesQuery } from '../system/systemApi'
import { useLoginMutation } from './authApi'
import './login.css'

const features = [
  { icon: ClipboardList, title: 'Sipariş ve planlama', text: 'Müşteri siparişinden üretim planına tek akış.' },
  { icon: Factory, title: 'Üretim ve saha', text: 'RFID kart ve tabletle anlık iş takibi, canlı makine panosu.' },
  { icon: ShieldCheck, title: 'Kalite ve depo', text: 'Kontrol kayıtları, lot izlenebilirliği ve adresli stok.' },
  { icon: FileChartColumn, title: 'Sevkiyat ve raporlama', text: 'Sevkiyat takibi, raporlar, rol bazlı yetki ve işlem kayıtları.' },
]

/** Giriş ve parola sıfırlama ekranlarının sol tanıtım paneli. Metin lisanstan bağımsızdır, proje adı geçmez. */
export function LoginAside() {
  return (
    <aside className="login__aside" aria-hidden="true">
      <div className="login__aside-brand">
        <BrandMark />
        Tekstil ERP
      </div>
      <div>
        <p className="login__headline">Siparişten sevkiyata, tek ekranda.</p>
        <hr className="login__seam" />
        <p className="login__lead">
          Sipariş, üretim, kalite, depo ve sevkiyat; her adım gerçekleştiği anda kayıt altında.
        </p>
      </div>
      <ul className="login__features">
        {features.map(({ icon: Icon, title, text }) => (
          <li key={title}>
            <Icon />
            <div>
              <strong>{title}</strong>
              <span>{text}</span>
            </div>
          </li>
        ))}
      </ul>
    </aside>
  )
}

export function LoginPage() {
  const status = useAppSelector((state) => state.auth.status)
  const notice = useAppSelector((state) => state.auth.notice)
  const location = useLocation()
  const [login, { isLoading, error }] = useLoginMutation()
  // Beni hatırla ile girilmiş son kullanıcı adı önceden yazılır.
  const [userName, setUserName] = useState(() => tokenStorage.lastUserName() ?? '')
  const [rememberMe, setRememberMe] = useState(() => tokenStorage.lastUserName() !== null)
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [capsLock, setCapsLock] = useState(false)
  // Oturumsuz okunur: lisans sahibi ve sunucuya erişim.
  const system = useGetSystemModulesQuery()
  const license = system.data?.license

  if (status === 'authenticated') {
    const from = (location.state as { from?: string } | null)?.from ?? '/'
    return <Navigate to={from} replace />
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    tokenStorage.rememberUserName(rememberMe ? userName.trim() : null)
    void login({ userName, password, rememberMe })
  }

  const requestError = isRequestError(error) ? error : undefined

  return (
    <main className="login">
      <LoginAside />

      <div className="login__panel">
      <form className="login__card" onSubmit={handleSubmit} noValidate>
        <div className="login__brand">
          <BrandMark />
          <div>
            <h1>Hoş geldiniz</h1>
            <p>Devam etmek için oturum açın.</p>
          </div>
        </div>

        {license && !license.valid && <p className="alert alert--error" role="alert">Lisans geçersiz: {license.error} Sistem yöneticinize başvurun.</p>}

        {notice && !requestError && <p className="alert alert--info" role="status">{notice}</p>}

        <label className="field">
          <span>Kullanıcı adı</span>
          <span className="login__input">
            <UserRound aria-hidden="true" />
            <input
              name="userName"
              autoComplete="username"
              autoFocus={!userName}
              value={userName}
              onChange={(event) => setUserName(event.target.value)}
              aria-invalid={Boolean(requestError?.details?.userName)}
            />
          </span>
          {requestError?.details?.userName?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </label>

        <label className="field">
          <span>Parola</span>
          <span className="login__input">
            <LockKeyhole aria-hidden="true" />
            <input
              name="password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              autoFocus={Boolean(tokenStorage.lastUserName())}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              onKeyUp={(event) => setCapsLock(event.getModifierState('CapsLock'))}
              onBlur={() => setCapsLock(false)}
              aria-invalid={Boolean(requestError?.details?.password)}
            />
            <button
              type="button"
              className="login__reveal"
              aria-label={showPassword ? 'Parolayı gizle' : 'Parolayı göster'}
              title={showPassword ? 'Parolayı gizle' : 'Parolayı göster'}
              onClick={() => setShowPassword(!showPassword)}
            >
              {showPassword ? <EyeOff aria-hidden="true" /> : <Eye aria-hidden="true" />}
            </button>
          </span>
          {capsLock && <small className="login__caps" role="status">Büyük harf (Caps Lock) açık.</small>}
          {requestError?.details?.password?.map((message) => (
            <small key={message} className="field__error">{message}</small>
          ))}
        </label>

        <div className="login__options">
          <label className="checkbox">
            <input type="checkbox" checked={rememberMe} onChange={(event) => setRememberMe(event.target.checked)} />
            Beni hatırla
          </label>
          <Link to="/forgot-password">Şifremi unuttum</Link>
        </div>

        {requestError && !requestError.details && (
          <p className="alert alert--error" role="alert">{requestError.message}</p>
        )}

        <button className="button button--primary" type="submit" disabled={isLoading}>
          <LogIn aria-hidden="true" />
          {isLoading ? 'Giriş yapılıyor…' : 'Giriş yap'}
        </button>
        <p className="login__footnote">Hesabınız yoksa sistem yöneticinize başvurun.</p>
      </form>

      <footer className="login__foot">
        <span>{license?.valid && license.customer}</span>
        <span className={system.isError ? 'login__status login__status--down' : 'login__status'} role="status">
          {system.isLoading ? 'Sunucu denetleniyor…' : system.isError ? 'Sunucuya ulaşılamıyor' : 'Sunucu bağlı'}
        </span>
      </footer>
      </div>
    </main>
  )
}
