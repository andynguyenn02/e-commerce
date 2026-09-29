import { api } from './client'
import type { Me } from './types'

export interface Credentials {
  username: string
  password: string
}

// Login only sets the httpOnly cookie; the user info comes from /me.
export const login = (body: Credentials) => api.post('/api/auth/login', body)

export const logout = () => api.post('/api/auth/logout')

export const getMe = () => api.get<Me>('/api/auth/me').then((r) => r.data)
