import * as RDialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

interface Props {
  title: string
  description?: string
  onClose: () => void
  /** Block closing while a request is in flight. */
  locked?: boolean
  className?: string
  children: ReactNode
}

/** Radix owns focus trap, scroll lock and Escape; we only style the shell. */
export function Dialog({ title, description, onClose, locked, className, children }: Props) {
  return (
    <RDialog.Root open onOpenChange={(open) => !open && !locked && onClose()}>
      <RDialog.Portal>
        <RDialog.Overlay className="fixed inset-0 z-40 bg-ink/40 backdrop-blur-[1px]" />
        <RDialog.Content
          onEscapeKeyDown={(e) => locked && e.preventDefault()}
          onPointerDownOutside={(e) => locked && e.preventDefault()}
          className={cn(
            'fixed left-1/2 top-1/2 z-50 w-[calc(100vw-2rem)] max-w-lg -translate-x-1/2 -translate-y-1/2',
            'max-h-[calc(100vh-2rem)] overflow-y-auto rounded-sm border border-ink bg-paper',
            className,
          )}
        >
          <div className="flex items-start justify-between gap-4 rule-b px-5 py-3.5">
            <div>
              <RDialog.Title className="text-lg">{title}</RDialog.Title>
              {description && (
                <RDialog.Description className="mt-0.5 text-[13px] text-muted">
                  {description}
                </RDialog.Description>
              )}
            </div>
            <RDialog.Close
              aria-label="Close"
              disabled={locked}
              className="-mr-1 rounded-sm p-1 text-muted transition-colors hover:bg-paper-sunk hover:text-ink disabled:opacity-40"
            >
              <X size={16} />
            </RDialog.Close>
          </div>
          <div className="px-5 py-4">{children}</div>
        </RDialog.Content>
      </RDialog.Portal>
    </RDialog.Root>
  )
}

/** Right-aligned action row, divided from the body above it. */
export function DialogActions({ children }: { children: ReactNode }) {
  return <div className="mt-5 flex justify-end gap-2 rule-t pt-4">{children}</div>
}
