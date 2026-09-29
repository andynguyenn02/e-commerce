import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit'
import * as authApi from '../api/auth'
import { getErrorMessage } from '../api/client'
import { getBalance } from '../api/wallet'
import type { Me } from '../api/types'
import { initialAsyncState, type AsyncState } from './asyncStatus'

interface AuthState extends AsyncState {
  user: Me | null
  // false until the startup /me check finishes; guards wait on it so a
  // page reload doesn't bounce a logged-in user to /login
  initialized: boolean
  // Wallet balance: shown in a few places only, so no separate slice.
  balance: number | null
}

const initialState: AuthState = {
  ...initialAsyncState,
  user: null,
  initialized: false,
  balance: null,
}

// The token is an httpOnly cookie, so JS can't read it (safer against XSS than
// localStorage). The session is restored on reload by asking the backend who we are.
export const restoreSession = createAsyncThunk('auth/restoreSession', authApi.getMe)

export const login = createAsyncThunk<Me, authApi.Credentials, { rejectValue: string }>(
  'auth/login',
  async (credentials, { rejectWithValue }) => {
    try {
      await authApi.login(credentials)
      return await authApi.getMe()
    } catch (err) {
      return rejectWithValue(getErrorMessage(err))
    }
  },
)

export const fetchBalance = createAsyncThunk('auth/fetchBalance', async () => (await getBalance()).balance)

export const logout = createAsyncThunk('auth/logout', async () => {
  // Clear the local session even if the call fails (e.g. cookie already expired).
  await authApi.logout().catch(() => undefined)
})

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    // Dispatched by the 401 interceptor when the cookie expires mid-session.
    sessionExpired(state) {
      state.user = null
      state.balance = null
    },
    // Checkout returns the new balance, so no extra request is needed.
    balanceChanged(state, action: PayloadAction<number>) {
      state.balance = action.payload
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(restoreSession.fulfilled, (state, action) => {
        state.user = action.payload
        state.initialized = true
      })
      .addCase(restoreSession.rejected, (state) => {
        state.user = null
        state.initialized = true
      })
      .addCase(login.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(login.fulfilled, (state, action) => {
        state.status = 'success'
        state.user = action.payload
        state.initialized = true
      })
      .addCase(login.rejected, (state, action) => {
        state.status = 'error'
        state.error = action.payload ?? 'Login failed'
      })
      .addCase(fetchBalance.fulfilled, (state, action) => {
        state.balance = action.payload
      })
      .addCase(logout.fulfilled, () => ({ ...initialState, initialized: true }))
  },
})

export const { sessionExpired, balanceChanged } = authSlice.actions
export default authSlice.reducer
