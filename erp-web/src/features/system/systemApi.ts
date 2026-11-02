import { baseApi } from '../../api/baseApi'

/** Tekstil ERP'nin sektör modülleri (projeler). Çekirdek ekranlar her projede görünür. */
export type ProjectKey = 'Konfeksiyon' | 'Boyahane' | 'Dokuma' | 'Orme'

export const projectOrder: ProjectKey[] = ['Konfeksiyon', 'Boyahane', 'Dokuma', 'Orme']

/** Modülün ekranda görünen adı; anahtar kodda ASCII kalır. */
export const projectLabels: Record<ProjectKey, string> = { Konfeksiyon: 'Konfeksiyon', Boyahane: 'Boyahane', Dokuma: 'Dokuma', Orme: 'Örme' }

export interface SystemModule {
  key: string
  displayName: string
}

export interface LicenseStatus {
  valid: boolean
  customer: string | null
  expiresAt: string | null
  error: string | null
}

export interface SystemModules {
  /** Bu kurulumda devrede olan modüller (kurulu, lisanslı ve açık). */
  modules: SystemModule[]
  license: LicenseStatus
}

// Kurulum boyunca değişmez: uygulama açıkken bir kez okunur.
export const systemApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getSystemModules: build.query<SystemModules, void>({
      query: () => '/system/modules',
      keepUnusedDataFor: 24 * 60 * 60,
    }),
  }),
})

export const { useGetSystemModulesQuery } = systemApi

/** Devredeki projeler, sabit sırayla. Bilinmeyen anahtarlar (ileride eklenecek modüller) atlanır. */
export function activeProjects(data: SystemModules | undefined): ProjectKey[] {
  const keys = new Set(data?.modules.map((m) => m.key) ?? [])
  return projectOrder.filter((key) => keys.has(key))
}

