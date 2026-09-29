import { api } from './client'
import type { PagedResult, Product, ProductInput, ProductQuery } from './types'

export const getProducts = (query: ProductQuery, signal?: AbortSignal) =>
  api
    .get<PagedResult<Product>>('/api/product', {
      params: {
        search: query.search || undefined,
        categoryId: query.categoryId ?? undefined,
        pageNumber: query.pageNumber,
        pageSize: query.pageSize,
      },
      signal,
    })
    .then((r) => r.data)

export const createProduct = (body: ProductInput) =>
  api.post<string>('/api/product', body).then((r) => r.data)

export const updateProduct = (id: string, body: ProductInput) => api.put(`/api/product/${id}`, body)

// Separate endpoint on purpose: changing a price is its own admin action (§2).
export const updatePrice = (id: string, price: number) =>
  api.patch(`/api/product/${id}/price`, { price })

// Soft delete: hidden from the shop, order history keeps it.
export const deleteProduct = (id: string) => api.delete(`/api/product/${id}`)
