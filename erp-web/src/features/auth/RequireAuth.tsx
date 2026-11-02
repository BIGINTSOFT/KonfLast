import type { ReactNode } from 'react'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { usePermission } from '../../hooks/usePermission'
import { useAppSelector } from '../../store/hooks'

/** Oturum yoksa giriş ekranına yönlendirir; saklı oturum denenirken bekleme gösterir. */
export function RequireAuth() {
  const status = useAppSelector((state) => state.auth.status)
  const location = useLocation()

  if (status === 'unknown') {
    return <div className="splash" role="status">Oturum kontrol ediliyor…</div>
  }

  if (status === 'anonymous') {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />
  }

  return <Outlet />
}

/** Yetkisi olmayan kullanıcıya ekranı göstermez. Asıl yetki kontrolü API'dedir. */
export function RequirePermission({ permission, children }: { permission: string; children: ReactNode }) {
  if (!usePermission(permission)) {
    return (
      <section className="page">
        <h1>Yetkiniz yok</h1>
        <p className="muted">Bu ekranı görüntülemek için gerekli yetkiye sahip değilsiniz.</p>
      </section>
    )
  }

  return children
}
