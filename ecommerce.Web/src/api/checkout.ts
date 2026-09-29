import { api } from './client'
import type { CheckoutResult } from './types'

export const checkout = (cartItemIds: string[]) =>
  api.post<CheckoutResult>('/api/checkout', { cartItemId: cartItemIds }).then((r) => r.data)
