import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { login } from '../store/authSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { Button } from '../components/ui/button'
import { Field, Input } from '../components/ui/field'

export default function LoginPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const { user, status, error } = useAppSelector((s) => s.auth)
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')

  const from = (location.state as { from?: string } | null)?.from ?? '/products'
  const loading = status === 'loading'

  if (user) return <Navigate to={from} replace />

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const result = await dispatch(login({ username: username.trim(), password }))
    if (login.fulfilled.match(result)) navigate(from, { replace: true })
  }

  return (
    <div className="min-h-screen grid place-items-center px-4">
      <div className="w-full max-w-sm">
        <div className="mb-6 text-center font-display text-3xl">E-Shop</div>

        <form className="flex flex-col gap-4 rounded-sm border border-rule p-6" onSubmit={onSubmit}>
          <h1 className="m-0">Sign in</h1>

          <Field label="Username">
            <Input
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              autoComplete="username"
              autoFocus
              required
            />
          </Field>

          <Field label="Password">
            <Input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </Field>

          {status === 'error' && error && (
            <p className="rounded-sm border border-accent/30 bg-accent-wash px-3 py-2 text-[13px] text-accent-ink">
              {error}
            </p>
          )}

          <Button type="submit" disabled={loading}>
            {loading ? 'Signing in' : 'Sign in'}
          </Button>
        </form>
      </div>
    </div>
  )
}
