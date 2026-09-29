import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit'
import * as cartApi from '../api/cart'
import * as checkoutApi from '../api/checkout'
import { getErrorMessage } from '../api/client'
import type { Cart, CheckoutResult } from '../api/types'
import { balanceChanged, logout, sessionExpired } from './authSlice'
import { initialAsyncState, type AsyncState, type AsyncStatus } from './asyncStatus'
import type { RootState } from './index'

interface CartState extends AsyncState {
  // Prices and totals always come from the API (current product price).
  // The frontend never computes money, so there is one source of truth.
  cart: Cart | null
  // Cart item ids with a change in flight, to disable just those rows.
  busyItemIds: string[]
  // Cart item ids ticked for checkout. Only these are sent to POST /api/checkout.
  selectedItemIds: string[]
  checkoutStatus: AsyncStatus
  checkoutError: string | null
}

const initialState: CartState = {
  ...initialAsyncState,
  cart: null,
  busyItemIds: [],
  selectedItemIds: [],
  checkoutStatus: 'idle',
  checkoutError: null,
}

// Every mutation reloads the cart afterwards so totals are recomputed by the backend.
function mutation<Arg>(type: string, run: (arg: Arg) => Promise<unknown>) {
  return createAsyncThunk<Cart, Arg, { rejectValue: string }>(type, async (arg, { rejectWithValue }) => {
    try {
      await run(arg)
      return await cartApi.getCart()
    } catch (err) {
      return rejectWithValue(getErrorMessage(err))
    }
  })
}

export const fetchCart = createAsyncThunk<Cart, void, { rejectValue: string }>(
  'cart/fetch',
  async (_, { rejectWithValue }) => {
    try {
      return await cartApi.getCart()
    } catch (err) {
      return rejectWithValue(getErrorMessage(err))
    }
  },
)

export const addToCart = mutation<{ productId: string; quantity: number }>(
  'cart/add',
  ({ productId, quantity }) => cartApi.addItem(productId, quantity),
)

export const updateCartQuantity = mutation<{ itemId: string; quantity: number }>(
  'cart/updateQuantity',
  ({ itemId, quantity }) => cartApi.updateQuantity(itemId, quantity),
)

export const removeCartItem = mutation<{ itemId: string }>('cart/remove', ({ itemId }) =>
  cartApi.removeItem(itemId),
)

// Replace the cart and keep the selection in sync: items that are new since the
// last load start selected, items that disappeared are dropped.
function applyCart(state: CartState, next: Cart) {
  const previous = new Set(state.cart?.cartItems.map((i) => i.id) ?? [])
  const selected = new Set(state.selectedItemIds)
  state.selectedItemIds = next.cartItems
    .filter((i) => selected.has(i.id) || !previous.has(i.id))
    .map((i) => i.id)
  state.cart = next
}

export const checkout = createAsyncThunk<
  CheckoutResult & { checkedOutItemIds: string[] },
  void,
  { state: RootState; rejectValue: string }
>(
  'cart/checkout',
  async (_, { getState, dispatch, rejectWithValue }) => {
    const itemIds = getState().cart.selectedItemIds
    try {
      const result = await checkoutApi.checkout(itemIds)
      dispatch(balanceChanged(result.remainingBalance))
      return { ...result, checkedOutItemIds: itemIds }
    } catch (err) {
      return rejectWithValue(getErrorMessage(err))
    }
  },
  {
    // Synchronous guard: a second click while one checkout is in flight is
    // dropped before any request goes out, so it can never create two orders.
    condition: (_, { getState }) => {
      const { checkoutStatus, selectedItemIds } = getState().cart
      return checkoutStatus !== 'loading' && selectedItemIds.length > 0
    },
  },
)

const cartSlice = createSlice({
  name: 'cart',
  initialState,
  reducers: {
    toggleItemSelected(state, action: PayloadAction<string>) {
      const id = action.payload
      state.selectedItemIds = state.selectedItemIds.includes(id)
        ? state.selectedItemIds.filter((x) => x !== id)
        : [...state.selectedItemIds, id]
    },
    setAllSelected(state, action: PayloadAction<boolean>) {
      state.selectedItemIds = action.payload ? (state.cart?.cartItems.map((i) => i.id) ?? []) : []
    },
    resetCheckout(state) {
      state.checkoutStatus = 'idle'
      state.checkoutError = null
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchCart.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(fetchCart.fulfilled, (state, action) => {
        state.status = 'success'
        applyCart(state, action.payload)
      })
      .addCase(fetchCart.rejected, (state, action) => {
        state.status = 'error'
        state.error = action.payload ?? 'Could not load cart'
      })

    for (const thunk of [updateCartQuantity, removeCartItem]) {
      builder
        .addCase(thunk.pending, (state, action) => {
          state.busyItemIds.push(action.meta.arg.itemId)
        })
        .addCase(thunk.fulfilled, (state, action) => {
          state.busyItemIds = state.busyItemIds.filter((id) => id !== action.meta.arg.itemId)
          applyCart(state, action.payload)
          state.status = 'success'
        })
        .addCase(thunk.rejected, (state, action) => {
          // Failed change: keep the cart as it was, the page shows the error.
          state.busyItemIds = state.busyItemIds.filter((id) => id !== action.meta.arg.itemId)
        })
    }

    builder
      .addCase(addToCart.fulfilled, (state, action) => {
        applyCart(state, action.payload)
        state.status = 'success'
      })
      .addCase(checkout.pending, (state) => {
        state.checkoutStatus = 'loading'
        state.checkoutError = null
      })
      .addCase(checkout.fulfilled, (state, action) => {
        state.checkoutStatus = 'success'
        // Backend removed exactly the items we sent; drop them only now that it succeeded.
        // (The page refetches afterwards for the authoritative cart.)
        if (state.cart) {
          const sent = new Set(action.payload.checkedOutItemIds)
          state.cart.cartItems = state.cart.cartItems.filter((i) => !sent.has(i.id))
          state.selectedItemIds = state.selectedItemIds.filter((id) => !sent.has(id))
          if (state.cart.cartItems.length === 0) state.cart.totalPrice = 0
        }
      })
      .addCase(checkout.rejected, (state, action) => {
        // Failure: the cart is left exactly as it was.
        state.checkoutStatus = 'error'
        state.checkoutError = action.payload ?? 'Checkout failed'
      })
      // Another user's cart must never leak into the next session.
      .addCase(logout.fulfilled, () => initialState)
      .addCase(sessionExpired, () => initialState)
  },
})

export const { resetCheckout, toggleItemSelected, setAllSelected } = cartSlice.actions
export default cartSlice.reducer

export const selectCartCount = (cart: Cart | null) =>
  cart?.cartItems.reduce((sum, item) => sum + item.quantity, 0) ?? 0
