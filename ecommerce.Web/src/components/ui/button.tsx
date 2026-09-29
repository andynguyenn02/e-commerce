import type { ButtonHTMLAttributes } from 'react'
import { Link, type LinkProps } from 'react-router-dom'
import { cn } from '../../lib/cn'

type Variant = 'solid' | 'outline' | 'danger' | 'ghost' | 'quiet'
type Size = 'sm' | 'md' | 'lg'

const base =
  'inline-flex items-center justify-center gap-2 rounded-sm font-medium whitespace-nowrap ' +
  'transition-colors disabled:pointer-events-none disabled:opacity-40'

const variants: Record<Variant, string> = {
  // Solid ink, not the accent: red is saved for destructive and for live state.
  solid: 'bg-ink text-paper hover:bg-ink-soft',
  outline: 'border border-rule bg-paper text-ink hover:border-ink hover:bg-paper-sunk',
  danger: 'bg-accent text-paper hover:bg-accent-ink',
  ghost: 'text-ink hover:bg-paper-sunk',
  quiet: 'text-muted underline decoration-rule underline-offset-4 hover:text-ink hover:decoration-ink',
}

const sizes: Record<Size, string> = {
  sm: 'h-8 px-2.5 text-[13px]',
  md: 'h-9 px-3.5 text-sm',
  lg: 'h-11 px-6 text-base',
}

export const buttonClass = (variant: Variant = 'solid', size: Size = 'md', extra?: string) =>
  cn(base, variants[variant], sizes[size], extra)

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant
  size?: Size
}

export function Button({ variant, size, className, ...props }: ButtonProps) {
  return <button className={buttonClass(variant, size, className)} {...props} />
}

/** Same shape as Button, but renders a router link. */
export function ButtonLink({
  variant,
  size,
  className,
  ...props
}: LinkProps & { variant?: Variant; size?: Size }) {
  return <Link className={buttonClass(variant, size, className)} {...props} />
}
