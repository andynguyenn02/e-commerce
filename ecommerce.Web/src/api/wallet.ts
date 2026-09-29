import { api } from './client'
import type { Wallet, WalletTransaction } from './types'

export const getBalance = () => api.get<Wallet>('/api/wallet/balance').then((r) => r.data)

export const getTransactions = () =>
  api.get<WalletTransaction[]>('/api/wallet/transactions').then((r) => r.data)
