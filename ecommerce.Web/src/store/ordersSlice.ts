import { createAsyncThunk, createSlice } from '@reduxjs/toolkit'
import { getErrorMessage } from '../api/client'
import * as ordersApi from '../api/orders'
import type { OrderDetail, OrderSummary } from '../api/types'
import { logout, sessionExpired } from './authSlice'
import { initialAsyncState, type AsyncState } from './asyncStatus'

interface OrdersState {
  list: AsyncState & { items: OrderSummary[] }
  // The order currently being viewed.
  current: AsyncState & { order: OrderDetail | null }
}

const initialState: OrdersState = {
  list: { ...initialAsyncState, items: [] },
  current: { ...initialAsyncState, order: null },
}

export const fetchOrders = createAsyncThunk<OrderSummary[], void, { rejectValue: string }>(
  'orders/fetchList',
  async (_, { rejectWithValue }) => {
    try {
      return await ordersApi.getOrders()
    } catch (err) {
      return rejectWithValue(getErrorMessage(err))
    }
  },
)

export const fetchOrder = createAsyncThunk<OrderDetail, string, { rejectValue: string }>(
  'orders/fetchOne',
  async (id, { signal, rejectWithValue }) => {
    try {
      return await ordersApi.getOrder(id, signal)
    } catch (err) {
      return rejectWithValue(getErrorMessage(err))
    }
  },
)

const ordersSlice = createSlice({
  name: 'orders',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      .addCase(fetchOrders.pending, (state) => {
        state.list.status = 'loading'
        state.list.error = null
      })
      .addCase(fetchOrders.fulfilled, (state, action) => {
        state.list.status = 'success'
        state.list.items = action.payload // already newest first from the API
      })
      .addCase(fetchOrders.rejected, (state, action) => {
        state.list.status = 'error'
        state.list.error = action.payload ?? 'Could not load orders'
      })
      .addCase(fetchOrder.pending, (state) => {
        state.current.status = 'loading'
        state.current.error = null
      })
      .addCase(fetchOrder.fulfilled, (state, action) => {
        state.current.status = 'success'
        state.current.order = action.payload
      })
      .addCase(fetchOrder.rejected, (state, action) => {
        if (action.meta.aborted) return
        state.current.status = 'error'
        state.current.error = action.payload ?? 'Could not load order'
        state.current.order = null
      })
      .addCase(logout.fulfilled, () => initialState)
      .addCase(sessionExpired, () => initialState)
  },
})

export default ordersSlice.reducer
