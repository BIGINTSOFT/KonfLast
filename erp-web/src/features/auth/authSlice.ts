import { createSlice } from '@reduxjs/toolkit'
import type { PayloadAction } from '@reduxjs/toolkit'
import type { AuthResponse, CurrentUser } from '../../types/api'

interface AuthState {
  /** 'unknown': sayfa açılışında saklı oturum henüz denenmedi. */
  status: 'unknown' | 'authenticated' | 'anonymous'
  accessToken: string | null
  user: CurrentUser | null
  /** Oturum kapanınca giriş ekranında gösterilecek bilgi (ör. parola değişti). */
  notice: string | null
}

const initialState: AuthState = { status: 'unknown', accessToken: null, user: null, notice: null }

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    credentialsReceived(state, action: PayloadAction<AuthResponse>) {
      state.status = 'authenticated'
      state.accessToken = action.payload.accessToken
      state.user = action.payload.user
      state.notice = null
    },
    sessionCleared(state, action: PayloadAction<string | undefined>) {
      state.status = 'anonymous'
      state.accessToken = null
      state.user = null
      state.notice = action.payload ?? null
    },
  },
})

export const { credentialsReceived, sessionCleared } = authSlice.actions
export const authReducer = authSlice.reducer
