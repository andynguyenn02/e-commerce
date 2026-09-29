import { useEffect } from 'react'
import { Link } from 'react-router-dom'
import { EmptyState, ErrorState, Spinner } from '../components/ui/feedback'
import { TableFrame, Td, Th } from '../components/ui/table'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { fetchOrders } from '../store/ordersSlice'
import { formatDate, formatMoney } from '../utils/format'

export default function OrdersPage() {
  const dispatch = useAppDispatch()
  const { items, status, error } = useAppSelector((s) => s.orders.list)

  // Refetch on every visit so an order placed a moment ago shows up.
  useEffect(() => {
    dispatch(fetchOrders())
  }, [dispatch])

  // Keep showing the previous list while refreshing instead of flashing a spinner.
  const hasData = status === 'success' || (status === 'loading' && items.length > 0)

  return (
    <section>
      <h1 className="mb-5">Your orders</h1>

      {!hasData && (status === 'idle' || status === 'loading') && <Spinner label="Loading orders" />}

      {status === 'error' && <ErrorState message={error} onRetry={() => dispatch(fetchOrders())} />}

      {status === 'success' && items.length === 0 && (
        <EmptyState>
          You have no orders yet. <Link to="/products">Start shopping</Link>
        </EmptyState>
      )}

      {hasData && items.length > 0 && (
        <TableFrame>
          <thead>
            <tr>
              <Th>Order</Th>
              <Th>Date</Th>
              <Th numeric>Products</Th>
              <Th numeric>Total</Th>
              <Th />
            </tr>
          </thead>
          <tbody>
            {items.map((o) => (
              <tr key={o.orderId}>
                <Td className="font-mono text-[13px]">#{o.orderId.slice(0, 8)}</Td>
                <Td>{formatDate(o.orderDate)}</Td>
                <Td numeric className="tabular-nums">
                  {o.quantity}
                </Td>
                <Td numeric className="font-medium tabular-nums">
                  {formatMoney(o.totalAmount)}
                </Td>
                <Td numeric>
                  <Link to={`/orders/${o.orderId}`} className="font-medium hover:text-accent">
                    View details
                  </Link>
                </Td>
              </tr>
            ))}
          </tbody>
        </TableFrame>
      )}
    </section>
  )
}
