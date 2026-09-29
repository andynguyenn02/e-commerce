import type { ReactNode } from 'react'
import { cn } from '../../lib/cn'

type Tone = 'neutral' | 'accent' | 'positive' | 'caution' | 'ink'

const tones: Record<Tone, string> = {
  neutral: 'border-rule bg-paper-sunk text-muted',
  accent: 'border-accent/25 bg-accent-wash text-accent-ink',
  positive: 'border-positive/25 bg-positive-wash text-positive',
  caution: 'border-caution/25 bg-caution-wash text-caution',
  ink: 'border-ink bg-ink text-paper',
}

export function Badge({
  tone = 'neutral',
  className,
  children,
}: {
  tone?: Tone
  className?: string
  children: ReactNode
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center gap-1.5 rounded-xs border px-1.5 py-0.5 text-[12px] font-medium leading-tight',
        tones[tone],
        className,
      )}
    >
      {children}
    </span>
  )
}
