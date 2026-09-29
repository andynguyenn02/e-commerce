import type { ReactNode } from 'react'

export function EmptyState({ children }: { children: ReactNode }) {
  return <div className="state-box muted">{children}</div>
}

export function ErrorState({ message, onRetry }: { message: string | null; onRetry: () => void }) {
  return (
    <div className="state-box" role="alert">
      <p className="state-error">{message ?? 'Something went wrong'}</p>
      <button onClick={onRetry}>Try again</button>
    </div>
  )
}
