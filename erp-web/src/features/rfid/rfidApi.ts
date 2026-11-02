import { baseApi } from '../../api/baseApi'

export type RfidCardType = 'BUNDLE' | 'OPERATOR'

export const rfidCardTypeLabels: Record<RfidCardType, string> = {
  BUNDLE: 'Demet kartı',
  OPERATOR: 'Personel kartı',
}

/** Kartın atanabileceği kayıt türleri; API yolunun ilk parçasıdır. */
export type CardOwner = 'bundles' | 'operators'

/** Okutulan demet kartı; kart boşsa demet alanları boştur. */
export interface BundleCard {
  uid: string
  label: string | null
  isActive: boolean
  bundleId: number | null
  bundleNo: string | null
}

export interface BundleCardRelease {
  uid: string
  bundleId: number
  bundleNo: string
}

// Atama hem demeti / operatörü hem de kart listesindeki "atandığı yer" bilgisini değiştirir.
export const rfidApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    assignCard: build.mutation<null, { owner: CardOwner; id: number; uid: string }>({
      query: ({ owner, id, uid }) => ({ url: `/${owner}/${id}/rfid-card`, method: 'POST', body: { uid } }),
      invalidatesTags: ['Bundle', 'MasterData'],
    }),
    releaseCard: build.mutation<null, { owner: CardOwner; id: number }>({
      query: ({ owner, id }) => ({ url: `/${owner}/${id}/rfid-card`, method: 'DELETE' }),
      invalidatesTags: ['Bundle', 'MasterData'],
    }),
    // Kart boşaltma ekranı: her okutma güncel durumu sorar, önbellek tutulmaz.
    getBundleCard: build.query<BundleCard, string>({
      query: (uid) => `/bundle-cards/${encodeURIComponent(uid)}`,
      keepUnusedDataFor: 0,
    }),
    releaseBundleCard: build.mutation<BundleCardRelease, string>({
      query: (uid) => ({ url: `/bundle-cards/${encodeURIComponent(uid)}/release`, method: 'POST' }),
      invalidatesTags: ['Bundle', 'MasterData'],
    }),
  }),
})

export const { useAssignCardMutation, useReleaseCardMutation, useLazyGetBundleCardQuery, useReleaseBundleCardMutation } = rfidApi
