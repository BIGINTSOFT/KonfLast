import { baseApi } from '../../api/baseApi'

export type ReportColumnType = 'TEXT' | 'INTEGER' | 'DECIMAL' | 'PERCENT' | 'MINUTES' | 'DATE_TIME' | 'DATE'

export interface ReportDefinition {
  key: string
  title: string
  description: string
  /** false: rapor anlık durumu gösterir, tarih aralığı istemez. */
  requiresDateRange: boolean
  maxRangeDays: number
  /** Rapor metin araması destekliyorsa arama kutusunun açıklaması. */
  searchLabel: string | null
  /** Raporun listelendiği grup (ör. "Depo"); boşsa genel raporlar arasındadır. */
  group: string | null
  /** Rapor bir gün sayısı istiyorsa (ör. hareketsiz stok) kutunun açıklaması. */
  daysLabel: string | null
  defaultDays: number | null
}

export type ReportCell = string | number | null

export interface ReportColumn {
  key: string
  title: string
  type: ReportColumnType
}

/** Raporun tamamını sunucu üretir: sütunlar, sütun sırasıyla hücreler ve varsa toplam satırı. */
export interface ReportTable {
  key: string
  title: string
  generatedAt: string
  from: string | null
  to: string | null
  columns: ReportColumn[]
  rows: ReportCell[][]
  totals: ReportCell[] | null
  /** Satır sınırı aşıldı; liste eksik. */
  truncated: boolean
}

export interface ReportParams {
  key: string
  from?: string
  to?: string
  search?: string
  days?: string
}

export const reportsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getReportCatalog: build.query<ReportDefinition[], void>({
      query: () => '/reports',
    }),
    runReport: build.query<ReportTable, ReportParams>({
      query: ({ key, ...params }) => ({ url: `/reports/${key}`, params }),
      // Rapor her çalıştırmada güncel veriden üretilir; eski sonuç saklanmaz.
      keepUnusedDataFor: 0,
    }),
  }),
})

export const { useGetReportCatalogQuery, useLazyRunReportQuery } = reportsApi

/** Dışa aktarma adresinin API köküne göre yolu ve sorgusu. */
export function reportExportPath({ key, ...params }: ReportParams, format = 'csv'): string {
  const query = new URLSearchParams({ format })
  Object.entries(params).forEach(([name, value]) => {
    if (value) {
      query.set(name, value)
    }
  })
  return `/reports/${encodeURIComponent(key)}/export?${query.toString()}`
}
