// Mirrors the backend DTOs (System.Text.Json camelCase, enums as strings).

export type Role = 'Admin' | 'Customer'

export interface Me {
  id: string
  username: string
  role: Role
}

export interface PagedResult<T> {
  items: T[]
  totalItems: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

export interface Product {
  id: string
  name: string
  price: number
  code: string
  availableQuantity: number
  categoryId: string
  createdAt: string
}

export interface ProductInput {
  name: string
  price: number
  code: string
  availableQuantity: number
  categoryId: string
}

export interface ProductQuery {
  search: string
  categoryId: string | null
  pageNumber: number
  pageSize: number
}

export interface Category {
  id: string
  name: string
}

export interface CartItem {
  id: string
  productId: string
  productName: string
  productCode: string
  unitPrice: number
  quantity: number
  availableQuantity: number
  totalPrice: number
}

export interface Cart {
  userId: string
  cartItems: CartItem[]
  totalPrice: number
}

export interface CheckoutResult {
  orderId: string
  totalAmount: number
  remainingBalance: number
}

export interface OrderSummary {
  orderId: string
  orderDate: string
  quantity: number
  totalAmount: number
}

export interface OrderItem {
  orderItemId: string
  productName: string
  productCode: string
  productPrice: number // price at purchase time, not the current price
  quantity: number
  totalAmount: number
}

export interface OrderDetail {
  orderId: string
  orderDate: string
  items: OrderItem[]
  totalAmount: number
}

export interface Wallet {
  walletId: string
  balance: number
}

export interface WalletTransaction {
  walletTransactionId: string
  orderId: string
  totalAmount: number
  createdAt: string
}

export type InventoryJobStatus = 'Stored' | 'Accepted' | 'Processing' | 'Done' | 'Failed'

export interface InventoryJob {
  jobId: string
  fileName: string
  status: InventoryJobStatus
  createdAt: string
  emailSentAt: string | null
  errorMessage: string | null
}
