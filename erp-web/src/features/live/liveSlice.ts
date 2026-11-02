import { createSlice } from '@reduxjs/toolkit'
import type { PayloadAction } from '@reduxjs/toolkit'
import type { LiveStatus } from './liveUpdates'

interface LiveState {
  status: LiveStatus
}

const initialState: LiveState = { status: 'disconnected' }

const liveSlice = createSlice({
  name: 'live',
  initialState,
  reducers: {
    liveStatusChanged(state, action: PayloadAction<LiveStatus>) {
      state.status = action.payload
    },
  },
})

export const { liveStatusChanged } = liveSlice.actions
export const liveReducer = liveSlice.reducer
