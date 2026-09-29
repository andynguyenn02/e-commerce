import { api } from './client'
import type { Cart } from './types'

export const getCart = () => api.get<Cart>('/api/cart/mycart').then((r) => r.data)

export const addItem = (productId: string, quantity: number) =>
  api.post('/api/cart/add', { productId, quantity })

export const updateQuantity = (cartItemId: string, quantity: number) =>
  api.patch(`/api/cart/update/${cartItemId}`, { quantity })

export const removeItem = (cartItemId: string) => api.delete(`/api/cart/remove/${cartItemId}`)
