import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { useAppSelector } from '../store/hooks'
import { Permissions } from '../types/permissions'

/** Kayıt sayfalarının görüntüleme yetkileri (adres önekine göre). */
const requiredPermission: [prefix: string, permission: string][] = [
  ['/production-orders', Permissions.ProductionOrderView],
  ['/sales-orders', Permissions.SalesOrderView],
  ['/bundles', Permissions.BundleView],
]

/**
 * Başka bir modülün kaydına bağlantı. Kullanıcının o kaydı görme yetkisi yoksa bağlantı yerine yalnızca metin
 * gösterilir: tıklayıp "Yetkiniz yok" sayfasına düşmez (ör. depo kullanıcısının sevkiyattaki sipariş numarası).
 */
export function RecordLink({ to, children, className }: { to: string; children: ReactNode; className?: string }) {
  const permissions = useAppSelector((state) => state.auth.user?.permissions)
  const required = requiredPermission.find(([prefix]) => to === prefix || to.startsWith(`${prefix}/`))?.[1]
  return required && !permissions?.includes(required)
    ? <span className={className}>{children}</span>
    : <Link to={to} className={className}>{children}</Link>
}
