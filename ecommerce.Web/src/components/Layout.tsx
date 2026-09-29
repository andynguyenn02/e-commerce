import { useEffect } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { fetchBalance, logout } from '../store/authSlice'
import { fetchCart, selectCartCount } from '../store/cartSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { formatMoney } from '../utils/format'
import { Button } from './ui/button'
import { cn } from '../lib/cn'

/** Nav items sit on the baseline rule; the active one thickens it to ink. */
const navLink = ({ isActive }: { isActive: boolean }) =>
  cn(
    '-mb-px inline-flex items-center gap-1.5 border-b-2 px-0.5 pb-2.5 pt-2.5 text-sm transition-colors',
    isActive ? 'border-ink font-medium text-ink' : 'border-transparent text-muted hover:text-ink',
  )

export function Layout() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const user = useAppSelector((s) => s.auth.user)
  const balance = useAppSelector((s) => s.auth.balance)
  const cartStatus = useAppSelector((s) => s.cart.status)
  const cartCount = useAppSelector((s) => selectCartCount(s.cart.cart))

  // Load the cart once per session so the badge is right on every page.
  useEffect(() => {
    if (user?.role === 'Customer' && cartStatus === 'idle') dispatch(fetchCart())
  }, [user, cartStatus, dispatch])

  useEffect(() => {
    if (user?.role === 'Customer') dispatch(fetchBalance())
  }, [user, dispatch])

  const onLogout = async () => {
    await dispatch(logout())
    navigate('/login', { replace: true })
  }

  return (
    <>
      <header className="rule-b">
        {/* Masthead: the name sits alone above the nav, catalogue-style. */}
        <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 pt-4">
          <NavLink to="/products" className="font-display text-2xl font-semibold tracking-tight">
            E-Shop
          </NavLink>

          <div className="flex items-center gap-4 text-sm">
            {user?.role === 'Customer' && balance !== null && (
              <NavLink
                to="/wallet"
                title="Wallet balance"
                className="tabular-nums font-medium hover:text-accent"
              >
                {formatMoney(balance)}
              </NavLink>
            )}
            {user ? (
              <div className="flex items-center gap-3">
                <span className="hidden text-muted sm:inline">{user.username}</span>
                <span className="hidden text-[12px] text-muted sm:inline">({user.role})</span>
                <Button variant="ghost" size="sm" onClick={onLogout} aria-label="Log out">
                  <LogOut size={15} />
                  <span className="hidden sm:inline">Log out</span>
                </Button>
              </div>
            ) : (
              <NavLink to="/login" className="font-medium hover:text-accent">
                Sign in
              </NavLink>
            )}
          </div>
        </div>

        <nav className="mx-auto flex max-w-6xl gap-6 overflow-x-auto px-4" aria-label="Main">
          <NavLink to="/products" end className={navLink}>
            Products
          </NavLink>
          {/* Admin has no cart/orders/wallet: those APIs are Customer-only */}
          {user?.role === 'Customer' && (
            <>
              <NavLink to="/cart" className={navLink}>
                Cart
                {cartCount > 0 && (
                  <span className="rounded-xs bg-ink px-1.5 text-[11px] font-medium tabular-nums text-paper">
                    {cartCount}
                  </span>
                )}
              </NavLink>
              <NavLink to="/orders" className={navLink}>
                Orders
              </NavLink>
              <NavLink to="/wallet" className={navLink}>
                Wallet
              </NavLink>
            </>
          )}
          {user?.role === 'Admin' && (
            <>
              <NavLink to="/admin/products" className={navLink}>
                Manage products
              </NavLink>
              <NavLink to="/admin/inventory" className={navLink}>
                Inventory
              </NavLink>
            </>
          )}
        </nav>
      </header>

      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </>
  )
}
