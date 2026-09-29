import { useEffect } from 'react'
import { Link, useLocation, useParams } from 'react-router-dom'
import { ErrorState, Spinner } from '../components/ui/feedback'
import { TableFrame, Td, Th } from '../components/ui/table'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { fetchOrder } from '../store/ordersSlice'
import { formatDate, formatMoney } from '../utils/format'

export default function OrderDetailPage() {
  const { id = '' } = useParams()
  const location = useLocation()
  const dispatch = useAppDispatch()
  const { order, status, error } = useAppSelector((s) => s.orders.current)
  const justPlaced = (location.state as { justPlaced?: boolean } | null)?.justPlaced

  useEffect(() => {
    const request = dispatch(fetchOrder(id))
    return () => request.abort()
  }, [dispatch, id])

  // Don't show the previously viewed order while the new one loads.
  const showing = status === 'success' && order?.orderId === id ? order : null

  return (
    <section>
      <Link to="/orders" className="mb-4 inline-block text-sm text-muted hover:text-ink">
        ← All orders
      </Link>

      {justPlaced && showing && (
        <div className="mb-4 rounded-sm border border-positive/30 bg-positive-wash px-4 py-3 text-sm text-positive">
          Thank you! Your order has been placed.
        </div>
      )}

      {!showing && status !== 'error' && <Spinner label="Loading order" />}

      {status === 'error' && <ErrorState message={error} onRetry={() => dispatch(fetchOrder(id))} />}

      {showing && (
        <>
          <div className="mb-5 flex items-baseline justify-between gap-4 rule-b pb-4">
            <h1 className="m-0">
              Order <span className="font-mono text-[13px]">#{showing.orderId.slice(0, 8)}</span>
            </h1>
            <span className="text-sm text-muted">{formatDate(showing.orderDate)}</span>
          </div>

          <TableFrame>
            <thead>
              <tr>
                <Th>Product</Th>
                {/* "Price paid", not "Price": it is the price at purchase time
                    and may differ from the product's current price. */}
                <Th numeric>Price paid</Th>
                <Th numeric>Quantity</Th>
                <Th numeric>Line total</Th>
              </tr>
            </thead>
            <tbody>
              {showing.items.map((item) => (
                <tr key={item.orderItemId}>
                  <Td>
                    <div className="font-medium">{item.productName}</div>
                    <div className="font-mono text-[12px] text-muted">{item.productCode}</div>
                  </Td>
                  <Td numeric className="tabular-nums">
                    {formatMoney(item.productPrice)}
                  </Td>
                  <Td numeric className="tabular-nums">
                    {item.quantity}
                  </Td>
                  <Td numeric className="font-medium tabular-nums">
                    {formatMoney(item.totalAmount)}
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableFrame>

          <div className="mt-4 flex items-center justify-between gap-4 rounded-sm border border-ink p-4">
            <div className="text-[13px] text-muted">
              Prices shown are what you paid at checkout. Current product prices may differ.
            </div>
            <div className="text-right">
              <div className="text-sm text-muted">Total paid</div>
              <div className="font-display text-2xl tabular-nums">{formatMoney(showing.totalAmount)}</div>
            </div>
          </div>
        </>
      )}
    </section>
  )
}
