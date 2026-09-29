import { Loader2 } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'
import { Button } from './button'

export function Spinner({ label, className }: { label?: string; className?: string }) {
  return (
    <div className="flex items-center justify-center gap-2.5 py-12" role="status">
      <Loader2 className={cn('size-5 animate-spin text-muted', className)} />
      {label && <span className="text-sm text-muted">{label}</span>}
    </div>
  )
}

export function Skeleton({ className }: { className?: string }) {
  return <div className={cn('animate-pulse rounded-sm bg-paper-sunk', className)} />
}

/** Empty screens are an invitation to act, so they take an optional action. */
export function EmptyState({ children, action }: { children: ReactNode; action?: ReactNode }) {
  return (
    <div className="rounded-sm border border-dashed border-rule px-6 py-16 text-center">
      <p className="m-0 text-sm text-muted">{children}</p>
      {action && <div className="mt-4 flex justify-center">{action}</div>}
    </div>
  )
}

export function ErrorState({ message, onRetry }: { message: string | null; onRetry: () => void }) {
  return (
    <div className="rounded-sm border border-accent/30 bg-accent-wash px-6 py-12 text-center" role="alert">
      <p className="m-0 text-sm text-accent-ink">{message ?? 'Something went wrong'}</p>
      <div className="mt-4 flex justify-center">
        <Button variant="outline" size="sm" onClick={onRetry}>
          Try again
        </Button>
      </div>
    </div>
  )
}
