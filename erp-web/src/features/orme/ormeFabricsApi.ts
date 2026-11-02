import { baseApi } from '../../api/baseApi'

export interface OrmeFabricYarn {
  id: number
  sequence: number
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  /** Kumaş ağırlığındaki oran (%). */
  percent: number
  stitchLengthMm: number | null
}

/** Satırlar gönderilen sırayla kaydedilir; gönderilmeyenler silinir. */
export interface OrmeFabricYarnRequest {
  yarnId: number
  percent: number
  stitchLengthMm: number | null
}

export const ormeFabricsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getOrmeFabricYarns: build.query<{ yarns: OrmeFabricYarn[] }, number>({
      query: (fabricId) => `/orme-fabrics/${fabricId}/yarns`,
      providesTags: ['MasterData'],
    }),
    saveOrmeFabricYarns: build.mutation<unknown, { fabricId: number; yarns: OrmeFabricYarnRequest[] }>({
      query: ({ fabricId, yarns }) => ({ url: `/orme-fabrics/${fabricId}/yarns`, method: 'PUT', body: { yarns } }),
      // Listede iplik sayısı ve toplam oran görünür.
      invalidatesTags: ['MasterData'],
    }),
  }),
})

export const { useGetOrmeFabricYarnsQuery, useSaveOrmeFabricYarnsMutation } = ormeFabricsApi
