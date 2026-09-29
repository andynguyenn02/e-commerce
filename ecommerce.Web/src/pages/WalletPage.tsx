import { useEffect, useMemo } from 'react'
import { Link } from 'react-router-dom'
import { getTransactions } from '../api/wallet'
import { EmptyState, ErrorState, Spinner } from '../components/ui/feedback'
import { TableFrame, Td, Th } from '../components/ui/table'
import { useAsync } from '../hooks/useAsync'
import { fetchBalance } from '../store/authSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { formatDate, formatMoney } from '../utils/format'
import { cn } from '../lib/cn'

export default function WalletPage() {
  const dispatch = useAppDispatch()
  const balance = useAppSelector((s) => s.auth.balance)
  const { status, data, error, reload } = useAsync(getTransactions)

  useEffect(() => {
    dispatch(fetchBalance())
  }, [dispatch])

  // Backend returns them unordered; newest first reads like a statement.
  const transactions = useMemo(
    () => [...(data ?? [])].sort((a, b) => b.createdAt.localeCompare(a.createdAt)),
    [data],
  )

  return (
    <section>
      <h1 className="mb-5">Wallet</h1>

      <div className="mb-6 rounded-sm border border-ink p-4">
        <div className="text-sm text-muted">Current balance</div>
        <div className="font-display text-3xl tabular-nums">
          {balance === null ? '—' : formatMoney(balance)}
        </div>
      </div>

      <h2 className="mb-4">Transactions</h2>

      {(status === 'idle' || status === 'loading') && <Spinner label="Loading transactions" />}

      {status === 'error' && <ErrorState message={error} onRetry={reload} />}

      {status === 'success' && transactions.length === 0 && <EmptyState>No transactions yet.</EmptyState>}

      {status === 'success' && transactions.length > 0 && (
        <TableFrame>
          <thead>
            <tr>
              <Th>Date</Th>
              <Th>Order</Th>
              <Th numeric>Amount</Th>
            </tr>
          </thead>
          <tbody>
            {transactions.map((t) => (
              <tr key={t.walletTransactionId}>
                <Td>{formatDate(t.createdAt)}</Td>
                <Td>
                  <Link to={`/orders/${t.orderId}`} className="font-mono text-[13px] hover:text-accent">
                    #{t.orderId.slice(0, 8)}
                  </Link>
                </Td>
                <Td
                  numeric
                  className={cn(
                    'font-medium tabular-nums',
                    t.totalAmount < 0 ? 'text-accent' : 'text-positive',
                  )}
                >
                  {formatMoney(t.totalAmount)}
                </Td>
              </tr>
            ))}
          </tbody>
        </TableFrame>
      )}
    </section>
  )
}
