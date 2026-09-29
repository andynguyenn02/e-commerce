import { cn } from '../lib/cn'

/** Stable small integer from a product code, so a product always looks the same. */
function hash(code: string) {
  let h = 0
  for (let i = 0; i < code.length; i++) h = (h * 31 + code.charCodeAt(i)) | 0
  return Math.abs(h)
}

/** First letters of up to two words: "USB-C Hub" -> "UH". */
function monogram(name: string) {
  const words = name.split(/[\s-]+/).filter(Boolean)
  return (words[0]?.[0] ?? '?').concat(words[1]?.[0] ?? '').toUpperCase()
}

const ANGLES = [0, 45, 90, 135]

/**
 * Products have no images in the API, so each card gets an engraved plate
 * instead of a grey box: hatching angled and spaced by the product code, with
 * the name's monogram struck over it. Monochrome, to stay inside the palette.
 */
export function ProductThumb({
  code,
  name,
  className,
}: {
  code: string
  name: string
  className?: string
}) {
  const h = hash(code)
  const angle = ANGLES[h % ANGLES.length]
  const gap = 6 + (h % 4) * 3

  return (
    <div
      aria-hidden="true"
      className={cn('relative grid aspect-4/3 place-items-center overflow-hidden bg-paper-sunk', className)}
      style={{
        backgroundImage: `repeating-linear-gradient(${angle}deg, var(--color-rule) 0 1px, transparent 1px ${gap}px)`,
      }}
    >
      <span className="font-display text-4xl font-semibold tracking-tight text-ink/70 select-none">
        {monogram(name)}
      </span>
    </div>
  )
}
