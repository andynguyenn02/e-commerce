import { configureStore } from '@reduxjs/toolkit'
import auth from './authSlice'
import cart from './cartSlice'
import orders from './ordersSlice'
import products from './productsSlice'

// Slices are split by data domain, not by screen: auth, products, cart, orders.
export const store = configureStore({
  reducer: { auth, products, cart, orders },
})

export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
