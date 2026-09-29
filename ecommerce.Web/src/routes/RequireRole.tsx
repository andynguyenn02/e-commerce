import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import type { Role } from '../api/types'
import { Spinner } from '../components/ui/feedback'
import { useAppSelector } from '../store/hooks'

// UX only: hides pages that make no sense for this user. Real protection is
// [Authorize] on the backend — edit the role in Redux DevTools and the page
// shows, but every API call still returns 403.
export function RequireRole({ roles, children }: { roles: Role[]; children: ReactNode }) {
  const { user, initialized } = useAppSelector((s) => s.auth)
  const location = useLocation()

  if (!initialized) return <Spinner label="Checking session..." />
  if (!user) return <Navigate to="/login" replace state={{ from: location.pathname }} />
  if (!roles.includes(user.role)) return <Navigate to="/products" replace />

  return children
}
