import type { InputHTMLAttributes, SelectHTMLAttributes, ReactNode } from 'react'
import { cn } from '../../lib/cn'

const control =
  'h-9 w-full rounded-sm border border-rule bg-paper px-2.5 text-sm text-ink ' +
  'placeholder:text-muted transition-colors hover:border-muted focus:border-ink ' +
  'disabled:bg-paper-sunk disabled:text-muted aria-invalid:border-accent'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(control, className)} {...props} />
}

/** Native select, styled — a custom listbox would buy nothing here. */
export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={cn(control, 'cursor-pointer pr-8', className)} {...props} />
}

interface FieldProps {
  label: string
  error?: string
  hint?: ReactNode
  className?: string
  children: ReactNode
}

export function Field({ label, error, hint, className, children }: FieldProps) {
  return (
    <label className={cn('flex flex-col gap-1.5', className)}>
      <span className="text-[13px] font-medium text-ink-soft">{label}</span>
      {children}
      {error ? (
        <span className="text-[12px] text-accent">{error}</span>
      ) : (
        hint && <span className="text-[12px] text-muted">{hint}</span>
      )}
    </label>
  )
}
