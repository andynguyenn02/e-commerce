import { api } from './client'
import type { Category } from './types'

export const getCategories = () => api.get<Category[]>('/api/category').then((r) => r.data)
