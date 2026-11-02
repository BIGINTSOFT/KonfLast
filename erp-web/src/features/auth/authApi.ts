import { baseApi } from '../../api/baseApi'
import { tokenStorage } from '../../services/tokenStorage'
import type { AuthResponse } from '../../types/api'
import { credentialsReceived, sessionCleared } from './authSlice'

export interface LoginRequest {
  userName: string
  password: string
  rememberMe: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export const authApi = baseApi.injectEndpoints({
  endpoints: (build) => ({
    login: build.mutation<AuthResponse, LoginRequest>({
      query: (body) => ({ url: '/auth/login', method: 'POST', body }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled
          tokenStorage.set(data.refreshToken, data.rememberMe)
          dispatch(credentialsReceived(data))
        } catch {
          // Hata, mutation sonucu üzerinden ekranda gösterilir.
        }
      },
    }),
    logout: build.mutation<null, void>({
      query: () => ({ url: '/auth/logout', method: 'POST', body: { refreshToken: tokenStorage.get() ?? '' } }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          await queryFulfilled
        } catch {
          // Sunucuya ulaşılamasa da yerel oturum kapatılır.
        } finally {
          tokenStorage.clear()
          dispatch(sessionCleared())
          dispatch(baseApi.util.resetApiState())
        }
      },
    }),
    forgotPassword: build.mutation<null, { userNameOrEmail: string }>({
      query: (body) => ({ url: '/auth/forgot-password', method: 'POST', body }),
    }),
    resetPassword: build.mutation<null, { token: string; newPassword: string }>({
      query: (body) => ({ url: '/auth/reset-password', method: 'POST', body }),
    }),
    changePassword: build.mutation<null, ChangePasswordRequest>({
      query: (body) => ({ url: '/auth/change-password', method: 'POST', body }),
      async onQueryStarted(_, { dispatch, queryFulfilled }) {
        try {
          await queryFulfilled
          // Sunucu tüm oturumları kapattı; yeni parolayla tekrar giriş gerekir.
          tokenStorage.clear()
          dispatch(sessionCleared('Parolanız değiştirildi. Lütfen yeni parolanızla giriş yapın.'))
          dispatch(baseApi.util.resetApiState())
        } catch {
          // Hata formda gösterilir.
        }
      },
    }),
  }),
})

export const {
  useLoginMutation,
  useLogoutMutation,
  useChangePasswordMutation,
  useForgotPasswordMutation,
  useResetPasswordMutation,
} = authApi
