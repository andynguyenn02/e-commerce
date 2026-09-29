import type { ReactNode } from 'react'
import { Dialog, DialogActions } from './ui/dialog'
import { Button } from './ui/button'

interface Props {
  title: string
  children: ReactNode
  confirmLabel: string
  busy?: boolean
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmDialog({ title, children, confirmLabel, busy, onConfirm, onCancel }: Props) {
  return (
    <Dialog title={title} onClose={onCancel} locked={busy} className="max-w-md">
      <div className="text-sm text-ink-soft">{children}</div>
      <DialogActions>
        <Button variant="outline" disabled={busy} onClick={onCancel}>
          Cancel
        </Button>
        <Button variant="danger" disabled={busy} onClick={onConfirm} autoFocus>
          {busy ? 'Working' : confirmLabel}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
