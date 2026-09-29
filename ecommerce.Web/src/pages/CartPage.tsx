import { useEffect, useMemo } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { CartRow } from '../components/CartRow'
import { EmptyState, ErrorState, Spinner } from '../components/ui/feedback'
import { TableFrame, Th } from '../components/ui/table'
import { Button } from '../components/ui/button'
import { checkout, fetchCart, resetCheckout, setAllSelected } from '../store/cartSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { formatMoney } from '../utils/format'

export default function CartPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { cart, status, error, checkoutStatus, checkoutError, selectedItemIds } = useAppSelector(
    (s) => s.cart,
  )
  const submitting = checkoutStatus === 'loading'

  // Always refetch on open: an admin may have changed prices since the badge loaded.
  useEffect(() => {
    dispatch(resetCheckout())
    dispatch(fetchCart())
  }, [dispatch])

  const onCheckout = async () => {
    const result = await dispatch(checkout())
    if (checkout.fulfilled.match(result)) {
      toast.success(`Order placed: ${formatMoney(result.payload.totalAmount)} paid`)
      dispatch(fetchCart())
      navigate(`/orders/${result.payload.orderId}`, { state: { justPlaced: true } })
    } else if (checkout.rejected.match(result) && result.payload) {
      // Cart is untouched. Reload it so new stock/price warnings show up.
      toast.error(result.payload)
      dispatch(fetchCart())
    }
  }

  const items = useMemo(() => cart?.cartItems ?? [], [cart])
  const selectedItems = useMemo(
    () => items.filter((i) => selectedItemIds.includes(i.id)),
    [items, selectedItemIds],
  )
  const allSelected = items.length > 0 && selectedItems.length === items.length
  const someSelected = selectedItems.length > 0 && !allSelected

  // Every figure here is an API value: the cart total when everything is ticked,
  // otherwise the sum of the API's line totals. The charged amount is always
  // recomputed by the backend at checkout.
  const selectedTotal = allSelected
    ? (cart?.totalPrice ?? 0)
    : selectedItems.reduce((sum, i) => sum + i.totalPrice, 0)

  return (
    <section>
      <h1 className="mb-5">Your cart</h1>

      {/* Only block the page on the first load; refreshes keep showing the cart. */}
      {!cart && (status === 'idle' || status === 'loading') && <Spinner label="Loading cart" />}

      {!cart && status === 'error' && <ErrorState message={error} onRetry={() => dispatch(fetchCart())} />}

      {cart && items.length === 0 && (
        <EmptyState>
          Your cart is empty. <Link to="/products">Browse products</Link>
        </EmptyState>
      )}

      {cart && items.length > 0 && (
        <>
          <TableFrame className="mb-6">
            <thead>
              <tr>
                <Th className="w-10">
                  <input
                    type="checkbox"
                    aria-label="Select all items"
                    checked={allSelected}
                    ref={(el) => {
                      if (el) el.indeterminate = someSelected
                    }}
                    disabled={submitting}
                    onChange={(e) => dispatch(setAllSelected(e.target.checked))}
                    className="size-4 accent-ink cursor-pointer"
                  />
                </Th>
                <Th>Product</Th>
                <Th numeric>Unit price</Th>
                <Th>Quantity</Th>
                <Th numeric>Line total</Th>
                <Th />
              </tr>
            </thead>
            <tbody>
              {items.map((item) => (
                // Keyed by quantity too, so the input resets whenever the server value changes.
                <CartRow key={`${item.id}:${item.quantity}`} item={item} />
              ))}
            </tbody>
          </TableFrame>

          <div className="flex items-end justify-between gap-6 rounded-sm border border-ink p-5">
            <div>
              <div className="text-sm text-muted">
                Selected total ({selectedItems.length} of {items.length} items)
              </div>
              <div className="font-display text-4xl tabular-nums">{formatMoney(selectedTotal)}</div>
              <div className="mt-1 text-[13px] text-muted">Prices are the current product prices.</div>
            </div>
            <div className="flex flex-col items-end gap-2">
              <Button
                size="lg"
                disabled={submitting || selectedItems.length === 0}
                onClick={onCheckout}
              >
                {submitting
                  ? 'Processing'
                  : `Checkout ${selectedItems.length} ${selectedItems.length === 1 ? 'item' : 'items'}`}
              </Button>
              {selectedItems.length === 0 && !submitting && (
                <p className="m-0 text-[13px] text-muted">Select at least one item to check out.</p>
              )}
              {checkoutStatus === 'error' && checkoutError && (
                <p
                  className="m-0 rounded-sm border border-accent/30 bg-accent-wash px-3 py-2 text-[13px] text-accent-ink"
                  role="alert"
                >
                  {checkoutError}
                </p>
              )}
            </div>
          </div>
        </>
      )}
    </section>
  )
}
