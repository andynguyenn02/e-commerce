import axios, { AxiosError } from 'axios'

// Auth lives in an httpOnly cookie set by the backend, so no component ever
// touches a token: the browser attaches it because of withCredentials.
export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
})

// The auth feature registers what to do on 401 (clear session, go to /login).
// Kept as a hook so this file doesn't import the store (avoids a circular import).
let onUnauthorized: (() => void) | null = null
export function setUnauthorizedHandler(handler: () => void) {
  onUnauthorized = handler
}

api.interceptors.response.use(
  (res) => res,
  (err: AxiosError) => {
    if (err.response?.status === 401) onUnauthorized?.()
    return Promise.reject(err)
  },
)

// Backend errors come back as { error, code } from GlobalExceptionHandler.
export function getErrorMessage(err: unknown): string {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { error?: string } | undefined
    if (data?.error) return data.error
    if (!err.response) return 'Cannot reach the server. Please try again.'
    // Kestrel rejects oversized bodies before our handler runs, so there is no { error }.
    if (err.response.status === 413) return 'File is too large for the server to accept.'
    return err.message
  }
  return err instanceof Error ? err.message : 'Something went wrong'
}
