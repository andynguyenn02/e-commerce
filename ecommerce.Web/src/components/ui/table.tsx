import type { ReactNode, ThHTMLAttributes, TdHTMLAttributes } from 'react'
import { cn } from '../../lib/cn'

/** Horizontally scrollable frame so wide admin tables never break the page. */
export function TableFrame({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div className={cn('overflow-x-auto rounded-sm border border-rule', className)}>
      <table className="w-full border-collapse text-sm">{children}</table>
    </div>
  )
}

export function Th({ className, numeric, ...props }: ThHTMLAttributes<HTMLTableCellElement> & { numeric?: boolean }) {
  return (
    <th
      className={cn(
        'rule-b bg-paper-sunk px-4 py-2.5 text-left align-middle text-[13px] font-medium text-muted',
        numeric && 'text-right',
        className,
      )}
      {...props}
    />
  )
}

export function Td({ className, numeric, ...props }: TdHTMLAttributes<HTMLTableCellElement> & { numeric?: boolean }) {
  return (
    <td
      className={cn(
        'px-4 py-3 align-middle [tr:not(:last-child)>&]:rule-b',
        numeric && 'text-right whitespace-nowrap',
        className,
      )}
      {...props}
    />
  )
}
