import { baseApi } from '../../api/baseApi'

/** Ana sayfadaki bir "yapılacak iş": bekleyen kayıt sayısı ve işin yapılacağı ekran. */
/** Bekleyen tek bir kayıt (ör. onay bekleyen sipariş); bildirimde kendi sayfasına götürür. */
export interface WorkTaskItem {
  title: string
  subtitle: string | null
  path: string
  /** Kaydın bu duruma geldiği an (onaya gönderilme / oluşturulma). */
  at: string | null
  by: string | null
}

export interface WorkTask {
  key: string
  title: string
  description: string
  count: number
  path: string
  /** Onay bekleyen kayıtlar tek tek (en yeniden eskiye, sınırlı sayıda); kayıt bazında gösterilmeyen işte yok. */
  items?: WorkTaskItem[] | null
}

export interface SearchHit {
  /** Kaydın türü, örn. "Üretim siparişi". */
  type: string
  title: string
  subtitle: string | null
  path: string
}

export interface SetupStep {
  key: string
  group: string
  title: string
  description: string
  path: string
  count: number
  done: boolean
  optional: boolean
}

export interface SetupStatus {
  requiredSteps: number
  completedRequiredSteps: number
  steps: SetupStep[]
}

// Bekleyen işler hemen her kayıttan etkilenir: ilgili etiketlerin hepsiyle tazelenir.
export const workspaceApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getWorkTasks: build.query<WorkTask[], void>({
      query: () => '/workspace/tasks',
      providesTags: ['Workspace', 'ProductionOrder', 'Bundle', 'PurchaseOrder', 'Shipment', 'MachineStatus', 'Stock', 'Quality'],
    }),
    searchRecords: build.query<SearchHit[], string>({
      query: (q) => ({ url: '/workspace/search', params: { q } }),
      providesTags: ['Workspace'],
    }),
    getSetupStatus: build.query<SetupStatus, void>({
      query: () => '/workspace/setup',
      providesTags: ['Workspace', 'MasterData', 'ProductionOrder', 'ProductionRoute', 'Payroll', 'Material'],
    }),
    /** Oturumdaki kullanıcının ana sayfa kısayolları: ekran adresleri, sırasıyla. */
    getShortcuts: build.query<string[], void>({
      query: () => '/workspace/shortcuts',
      providesTags: ['Shortcut'],
    }),
    saveShortcuts: build.mutation<string[], string[]>({
      query: (paths) => ({ url: '/workspace/shortcuts', method: 'PUT', body: { paths } }),
      invalidatesTags: ['Shortcut'],
    }),
  }),
})

export const {
  useGetWorkTasksQuery,
  useSearchRecordsQuery,
  useGetSetupStatusQuery,
  useGetShortcutsQuery,
  useSaveShortcutsMutation,
} = workspaceApi
