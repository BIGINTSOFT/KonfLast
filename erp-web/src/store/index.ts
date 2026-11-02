import { configureStore } from '@reduxjs/toolkit'
import { baseApi } from '../api/baseApi'
import { authReducer } from '../features/auth/authSlice'
import { liveReducer } from '../features/live/liveSlice'

export const store = configureStore({
  reducer: {
    auth: authReducer,
    live: liveReducer,
    [baseApi.reducerPath]: baseApi.reducer,
  },
  middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(baseApi.middleware),
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
