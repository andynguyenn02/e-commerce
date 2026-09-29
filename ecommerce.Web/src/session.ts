import { toast } from 'sonner'
import { setUnauthorizedHandler } from './api/client'
import { router } from './routes/router'
import { store } from './store'
import { restoreSession, sessionExpired } from './store/authSlice'

export function initSession() {
  // A 401 only means "session expired" if we thought we were logged in.
  // Guests calling /me on startup and failed logins also get 401 — ignore those.
  setUnauthorizedHandler(() => {
    if (!store.getState().auth.user) return
    store.dispatch(sessionExpired())
    toast.error('Your session has expired. Please sign in again.')
    router.navigate('/login', { state: { from: window.location.pathname } })
  })

  store.dispatch(restoreSession())
}
