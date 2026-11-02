import { baseApi } from '../../api/baseApi'

export interface FormDraft {
  formKey: string
  payload: string
  title: string | null
  updatedAt: string
}

// Taslaklar önbelleğe alınmaz: form her açılışta sunucudaki son hâli sorar.
export const draftsApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    getFormDraft: build.query<FormDraft | null, string>({
      query: (formKey) => `/workspace/drafts/${encodeURIComponent(formKey)}`,
      keepUnusedDataFor: 0,
    }),
    saveFormDraft: build.mutation<FormDraft, { formKey: string; payload: string; title: string | null }>({
      query: ({ formKey, payload, title }) => ({
        url: `/workspace/drafts/${encodeURIComponent(formKey)}`,
        method: 'PUT',
        body: { payload, title },
      }),
    }),
    deleteFormDraft: build.mutation<null, string>({
      query: (formKey) => ({ url: `/workspace/drafts/${encodeURIComponent(formKey)}`, method: 'DELETE' }),
    }),
  }),
})

export const { useGetFormDraftQuery, useSaveFormDraftMutation, useDeleteFormDraftMutation } = draftsApi
