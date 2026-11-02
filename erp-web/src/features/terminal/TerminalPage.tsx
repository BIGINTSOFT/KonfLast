import { ArrowLeft, ArrowLeftRight, ClipboardCheck, ClipboardList, LogOut, Monitor, PackageCheck, Search, Smartphone, Truck } from 'lucide-react'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { LucideIcon } from 'lucide-react'
import { BrandMark } from '../../components/BrandMark'
import { ErrorAlert } from '../../components/FormControls'
import { usePermission } from '../../hooks/usePermission'
import { useAppSelector } from '../../store/hooks'
import { Permissions } from '../../types/permissions'
import { useLogoutMutation } from '../auth/authApi'
import { RequirePermission } from '../auth/RequireAuth'
import { formatStockQuantity } from '../stock/stockPresentation'
import { BalanceList, ScanInput } from './ScanInput'
import { TerminalCount } from './TerminalCount'
import { TerminalLoading } from './TerminalLoading'
import { TerminalMove } from './TerminalMove'
import { TerminalPick } from './TerminalPick'
import { TerminalReceive } from './TerminalReceive'
import { useScanner } from './terminalApi'
import type { ScanResult } from './terminalApi'

type Mode = 'lookup' | 'move' | 'count' | 'pick' | 'receive' | 'loading'

/**
 * manage: stok yönetme yetkisi ister; purchase: satın alma siparişlerini görme yetkisi de ister; shipment: sevkiyat
 * yönetme ya da sevk etme yetkisi ister (stok yönetme yetkisi gerekmez).
 */
const modes: Record<Mode, { title: string; description: string; icon: LucideIcon; manage: boolean; purchase?: boolean; shipment?: boolean }> = {
  lookup: { title: 'Barkod sorgu', description: 'Top, adres ya da stok kartı okutun; nerede, ne kadar var görün.', icon: Search, manage: false },
  move: { title: 'Adres değiştir', description: 'Topu ya da kartı okutun, sonra yerleştirdiğiniz rafın adresini okutun.', icon: ArrowLeftRight, manage: true },
  count: { title: 'Sayım', description: 'Açık sayım fişine raf raf miktar girin.', icon: ClipboardCheck, manage: true },
  pick: { title: 'Toplama', description: 'Sipariş numarasını okutun; hangi raftan hangi topu alacağınızı görün, aldıkça okutun.', icon: ClipboardList, manage: false },
  receive: { title: 'Mal kabul', description: 'Satın alma siparişinden gelen malzemeyi teslim alın.', icon: PackageCheck, manage: true, purchase: true },
  loading: { title: 'Sevkiyat yükleme', description: 'Sevkiyatı seçin, araca konan her kolinin etiketini okutun; eksik koliler listelenir.', icon: Truck, manage: false, shipment: true },
}

function ScanSummary({ result }: { result: ScanResult }) {
  const { location, item, lot } = result
  return (
    <section className="terminal-card">
      {location && (
        <>
          <h2>Adres <code>{location.code}</code></h2>
          <p className="muted">
            {location.warehouseCode} — {location.warehouseName}
            {location.description && ` · ${location.description}`}
            {!location.isActive && ' · pasif'}
          </p>
        </>
      )}
      {lot && item && (
        <>
          <h2>Top <code>{lot.lotNo}</code></h2>
          <p>
            <code>{item.code}</code> {item.name}
            <br />
            <span className="muted">
              Giriş {formatStockQuantity(lot.initialQuantity, item.unit)}
              {lot.batchNo && ` · parti ${lot.batchNo}`}
              {lot.shade && ` · ton ${lot.shade}`}
              {lot.width !== null && ` · en ${lot.width} cm`}
            </span>
          </p>
        </>
      )}
      {!lot && item && (
        <>
          <h2>Stok kartı <code>{item.code}</code></h2>
          <p>{item.name}{item.isLotTracked && <span className="muted"> · top / parti takipli</span>}</p>
        </>
      )}
      <BalanceList rows={result.balances} />
      {result.balances.length > 1 && item && (
        <p className="muted">
          Toplam {formatStockQuantity(result.balances.reduce((sum, row) => sum + row.quantity, 0), item.unit)}
        </p>
      )}
    </section>
  )
}

function TerminalLookup() {
  const scanner = useScanner()
  const [result, setResult] = useState<ScanResult | null>(null)

  const handleScan = async (code: string) => {
    setResult(await scanner.scan(code))
  }

  return (
    <>
      <ScanInput label="Top, adres ya da stok kartı okutun" onScan={(code) => void handleScan(code)} busy={scanner.busy} focusToken={result} />
      <ErrorAlert error={scanner.error} />
      {result && <ScanSummary result={result} />}
    </>
  )
}

/**
 * El terminali: barkod okuyucuyla yapılan depo işleri. Okuyucu klavye gibi çalışır (kodu yazar, Enter gönderir);
 * ekran dar ve tek sütundur. Masaüstünde sekme olarak, el cihazında menüsüz (/handheld) açılır.
 */
export function TerminalPage({ standalone = false }: { standalone?: boolean }) {
  const canManage = usePermission(Permissions.StockManage)
  const [mode, setMode] = useState<Mode | null>(null)
  const canSeePurchases = usePermission(Permissions.PurchaseView)
  const canManageShipments = usePermission(Permissions.ShipmentManage)
  const canShip = usePermission(Permissions.ShipmentShip)
  const canLoad = canManageShipments || canShip
  const available = (Object.keys(modes) as Mode[]).filter(
    (key) => (canManage || !modes[key].manage) && (canSeePurchases || !modes[key].purchase) && (canLoad || !modes[key].shipment),
  )

  return (
    <section className="page terminal">
      {mode === null ? (
        <>
          <h1>El terminali</h1>
          <ul className="terminal-menu">
            {available.map((key) => {
              const { title, description, icon: Icon } = modes[key]
              return (
                <li key={key}>
                  <button type="button" className="terminal-menu__item" onClick={() => setMode(key)}>
                    <Icon aria-hidden="true" />
                    <span>
                      <strong>{title}</strong>
                      <span className="muted">{description}</span>
                    </span>
                  </button>
                </li>
              )
            })}
          </ul>
          {!standalone && (
            <p className="muted">
              Barkod okuyucu klavye gibi çalışır: imleç barkod kutusundayken okutmanız yeterlidir. El cihazında menüsüz
              görünüm için{' '}
              <a href="/handheld" target="_blank" rel="noreferrer"><Smartphone aria-hidden="true" /> el cihazı görünümünü</a> açın.
              Raflar için <Link to="/location-labels">adres etiketi</Link> basabilirsiniz.
            </p>
          )}
        </>
      ) : (
        <>
          <header className="terminal__header">
            <button type="button" className="button button--ghost" onClick={() => setMode(null)}>
              <ArrowLeft aria-hidden="true" />
              Menü
            </button>
            <h1>{modes[mode].title}</h1>
          </header>
          {mode === 'lookup' && <TerminalLookup />}
          {mode === 'move' && <TerminalMove />}
          {mode === 'count' && <TerminalCount />}
          {mode === 'pick' && <TerminalPick />}
          {mode === 'receive' && <TerminalReceive />}
          {mode === 'loading' && <TerminalLoading />}
        </>
      )}
    </section>
  )
}

/** El cihazı görünümü: menü ve sekmeler olmadan yalnızca terminal ekranı. */
export function HandheldPage() {
  const user = useAppSelector((state) => state.auth.user)
  const [logout, { isLoading }] = useLogoutMutation()

  const guard = (page: ReactNode) => <RequirePermission permission={Permissions.StockView}>{page}</RequirePermission>

  return (
    <div className="handheld">
      <header className="handheld__bar">
        <BrandMark />
        <strong>Depo terminali</strong>
        <span className="handheld__user">{user?.userName}</span>
        <Link to="/" className="appbar__icon" aria-label="Masaüstü görünümü" title="Masaüstü görünümü">
          <Monitor aria-hidden="true" />
        </Link>
        <button type="button" className="appbar__icon" aria-label="Çıkış yap" title="Çıkış yap" disabled={isLoading} onClick={() => void logout()}>
          <LogOut aria-hidden="true" />
        </button>
      </header>
      <main>{guard(<TerminalPage standalone />)}</main>
    </div>
  )
}
