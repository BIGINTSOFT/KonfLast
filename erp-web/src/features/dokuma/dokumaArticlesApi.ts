import { baseApi } from '../../api/baseApi'

export type ArticleYarnRole = 'WARP' | 'WEFT'

export interface DokumaArticleYarn {
  id: number
  role: ArticleYarnRole
  sequence: number
  yarnId: number
  yarnCode: string
  yarnName: string
  yarnColor: string | null
  /** Çözgüde tel sayısı, atkıda desen raporundaki atkı sayısı. */
  ends: number
}

/** Satırlar gönderilen sırayla kaydedilir; gönderilmeyenler silinir. */
export interface DokumaArticleYarnRequest {
  role: ArticleYarnRole
  yarnId: number
  ends: number
}

export const dokumaArticlesApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getDokumaArticleYarns: build.query<{ yarns: DokumaArticleYarn[] }, number>({
      query: (articleId) => `/dokuma-articles/${articleId}/yarns`,
      providesTags: ['MasterData'],
    }),
    saveDokumaArticleYarns: build.mutation<unknown, { articleId: number; yarns: DokumaArticleYarnRequest[] }>({
      query: ({ articleId, yarns }) => ({ url: `/dokuma-articles/${articleId}/yarns`, method: 'PUT', body: { yarns } }),
      // Listede toplam tel ve iplik sayıları görünür.
      invalidatesTags: ['MasterData'],
    }),
  }),
})

export const { useGetDokumaArticleYarnsQuery, useSaveDokumaArticleYarnsMutation } = dokumaArticlesApi
