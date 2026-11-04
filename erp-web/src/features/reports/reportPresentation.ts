import { formatDate, formatDateTime, formatQuantity } from '../../utils/format'
import { formatPercent } from '../productionTracking/trackingPresentation'
import { formatDuration } from '../workSessions/workSessionPresentation'
import type { ReportCell, ReportColumnType } from './reportsApi'

const decimal = new Intl.NumberFormat('tr-TR', { maximumFractionDigits: 3 })

const numericTypes: ReportColumnType[] = ['INTEGER', 'DECIMAL', 'PERCENT', 'MINUTES']

/** Sayısal sütunlar sağa hizalanır. */
export function isNumericColumn(type: ReportColumnType): boolean {
  return numericTypes.includes(type)
}

/** Hücreyi sütun türüne göre biçimlendirir; değer yoksa tire. Değerlerin kendisini sunucu hesaplar. */
export function formatReportCell(value: ReportCell, type: ReportColumnType): string {
  if (value === null) {
    return '—'
  }

  if (typeof value === 'string') {
    // Toplam satırının etiketi ("Toplam") tarih sütununa düşebilir; tarih olmayan metin olduğu gibi gösterilir.
    if (type === 'DATE_TIME' && !Number.isNaN(Date.parse(value))) {
      return formatDateTime(value)
    }
    return type === 'DATE' && /^\d{4}-\d{2}-\d{2}$/.test(value) ? formatDate(value) : value
  }

  switch (type) {
    case 'INTEGER':
      return formatQuantity(value)
    case 'PERCENT':
      return formatPercent(value)
    case 'MINUTES':
      return formatDuration(value)
    default:
      return decimal.format(value)
  }
}

/** İndirilen dosyanın adı: rapor anahtarı ve (varsa) gün aralığı. */
export function reportFileName(key: string, fromDate: string | null, toDate: string | null, extension = 'csv'): string {
  const range = fromDate && toDate ? `_${fromDate}_${toDate}` : ''
  return `${key}${range}.${extension}`
}
