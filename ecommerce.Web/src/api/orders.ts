import { api } from './client'
import type { OrderDetail, OrderSummary } from './types'

export const getOrders = () => api.get<OrderSummary[]>('/api/order').then((r) => r.data)

export const getOrder = (id: string, signal?: AbortSignal) =>
  api.get<OrderDetail>(`/api/order/${id}`, { signal }).then((r) => r.data)
