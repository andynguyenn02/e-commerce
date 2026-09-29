import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from './ui/button'

interface Props {
  page: number
  totalPages: number
  disabled?: boolean
  onChange: (page: number) => void
}

// Shows first, last and a window around the current page: 1 … 4 5 [6] 7 8 … 20
function pageNumbers(page: number, total: number): (number | '…')[] {
  const pages = new Set([1, total, page - 2, page - 1, page, page + 1, page + 2])
  const sorted = [...pages].filter((p) => p >= 1 && p <= total).sort((a, b) => a - b)
  const result: (number | '…')[] = []
  sorted.forEach((p, i) => {
    if (i > 0 && p - sorted[i - 1] > 1) result.push('…')
    result.push(p)
  })
  return result
}

export function Pagination({ page, totalPages, disabled, onChange }: Props) {
  if (totalPages <= 1) return null

  return (
    <nav className="mt-8 flex flex-wrap items-center justify-center gap-1 rule-t pt-6" aria-label="Pagination">
      <Button
        variant="ghost"
        size="sm"
        disabled={disabled || page <= 1}
        onClick={() => onChange(page - 1)}
        aria-label="Previous page"
      >
        <ChevronLeft size={15} />
        Prev
      </Button>
      {pageNumbers(page, totalPages).map((p, i) =>
        p === '…' ? (
          <span key={`gap-${i}`} className="px-1 text-muted">
            …
          </span>
        ) : (
          <Button
            key={p}
            variant={p === page ? 'solid' : 'ghost'}
            size="sm"
            className="min-w-8 tabular-nums"
            aria-current={p === page ? 'page' : undefined}
            disabled={disabled}
            onClick={() => onChange(p)}
          >
            {p}
          </Button>
        ),
      )}
      <Button
        variant="ghost"
        size="sm"
        disabled={disabled || page >= totalPages}
        onClick={() => onChange(page + 1)}
        aria-label="Next page"
      >
        Next
        <ChevronRight size={15} />
      </Button>
    </nav>
  )
}
