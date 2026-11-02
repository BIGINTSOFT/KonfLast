import { Link } from 'react-router-dom'
import { ErrorAlert } from '../../components/FormControls'
import { useGetBoyahaneSetupQuery } from './boyahaneInsightsApi'
import type { BoyahaneSetup } from './boyahaneInsightsApi'

const steps: { key: keyof BoyahaneSetup; label: string; path: string; hint: string; optional?: boolean }[] = [
  { key: 'customers', label: 'Müşteriler', path: '/boyahane-customers', hint: 'Fason çalışılan müşteriler.', optional: true },
  { key: 'fabrics', label: 'Kumaş kartları', path: '/boyahane-fabrics', hint: 'Ham en ve gramajla birlikte.' },
  { key: 'yarns', label: 'İplik kartları', path: '/boyahane-yarns', hint: 'İplik boyama yapılıyorsa.', optional: true },
  { key: 'dyeChemicals', label: 'Boya ve kimyasallar', path: '/boyahane-dye-chemicals', hint: 'Lab denemeleri bunlardan kurulur.' },
  { key: 'defectTypes', label: 'Hata türleri', path: '/boyahane-defect-types', hint: 'Ham kontrolde kullanılır.' },
  { key: 'operations', label: 'Operasyonlar', path: '/boyahane-operations', hint: 'Ön terbiye, boyama, yıkama gibi adımlar.' },
  { key: 'routes', label: 'Operasyon rotaları', path: '/boyahane/routes', hint: 'Her işlem türünün adım sırası.' },
  { key: 'machines', label: 'Makineler', path: '/boyahane-machines', hint: 'Kapasite ve yapılan operasyonlar.' },
  { key: 'rfidCards', label: 'RFID kartları', path: '/boyahane-rfid-cards', hint: 'Personel ve parti kartları.' },
  { key: 'operators', label: 'Operatörler', path: '/boyahane-operators', hint: 'Tablette kartla oturum açar.' },
  { key: 'tablets', label: 'Tabletler', path: '/boyahane-tablets', hint: 'Makineye ya da kontrol noktasına bağlanır.' },
  { key: 'stopReasons', label: 'Duruş nedenleri', path: '/boyahane-stop-reasons', hint: 'Tabletteki ara ve arıza seçenekleri.' },
]

/** Kurulum rehberi: üretime başlamadan önce tanımlanması gerekenler ve durumu. */
export function BoyahaneSetupPage() {
  const { data, error, isLoading } = useGetBoyahaneSetupQuery()

  return (
    <section className="page">
      <header className="page__header">
        <div>
          <h1>Kurulum Rehberi</h1>
          <p className="muted">Üretime başlamadan önce tanımlanması gerekenler, önerilen sırayla.</p>
        </div>
      </header>
      {isLoading && <p role="status">Yükleniyor…</p>}
      <ErrorAlert error={error} />
      {data && data.machinesWithoutOperation > 0 && (
        <div className="alert alert--info">{data.machinesWithoutOperation} makinede yapılan operasyon seçilmemiş; bu makinelerde adım başlatılamaz.</div>
      )}
      {data && (
        <div className="table-wrap">
          <table className="table">
            <thead><tr><th>Sıra</th><th>Tanım</th><th>Açıklama</th><th className="table__number">Kayıt</th><th>Durum</th></tr></thead>
            <tbody>
              {steps.map((step, index) => {
                const count = data[step.key]
                return (
                  <tr key={step.key}>
                    <td>{index + 1}</td>
                    <td><Link to={step.path}>{step.label}</Link></td>
                    <td className="muted">{step.hint}</td>
                    <td className="table__number">{count}</td>
                    <td>
                      {count > 0
                        ? <span className="badge badge--ok">Tamam</span>
                        : <span className={`badge badge--${step.optional ? 'neutral' : 'warning'}`}>{step.optional ? 'İsteğe bağlı' : 'Eksik'}</span>}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
