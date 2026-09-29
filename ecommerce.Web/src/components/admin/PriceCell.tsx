import { useState } from 'react'
import { toast } from 'sonner'
import { Pencil } from 'lucide-react'
import { getErrorMessage } from '../../api/client'
import { updatePrice } from '../../api/products'
import type { Product } from '../../api/types'
import { useAppDispatch } from '../../store/hooks'
import { productPriceUpdated } from '../../store/productsSlice'
import { formatMoney } from '../../utils/format'
import { Input } from '../ui/field'

// Click the price to edit in place: Enter saves, Escape or leaving the box cancels.
export function PriceCell({ product, onSaved }: { product: Product; onSaved: () => void }) {
  const dispatch = useAppDispatch()
  const [editing, setEditing] = useState(false)
  const [value, setValue] = useState('')
  const [saving, setSaving] = useState(false)

  const start = () => {
    setValue(String(product.price))
    setEditing(true)
  }

  const save = async () => {
    const price = Number(value)
    if (value.trim() === '' || !Number.isFinite(price) || price <= 0) {
      toast.error('Price must be greater than 0')
      return
    }
    if (price === product.price) {
      setEditing(false)
      return
    }
    setSaving(true)
    try {
      await updatePrice(product.id, price)
      dispatch(productPriceUpdated({ id: product.id, price }))
      setEditing(false)
      onSaved()
    } catch (err) {
      toast.error(getErrorMessage(err))
    } finally {
      setSaving(false)
    }
  }

  if (!editing) {
    return (
      <button
        className="group inline-flex items-center gap-1 tabular-nums underline decoration-dotted decoration-muted underline-offset-4 transition-colors hover:decoration-ink"
        onClick={start}
        title="Click to edit price"
      >
        {formatMoney(product.price)}
        <Pencil size={12} className="text-muted opacity-0 group-hover:opacity-100 group-focus-visible:opacity-100" aria-hidden />
      </button>
    )
  }

  return (
    <Input
      className="w-28 text-right"
      type="number"
      min={0}
      step="any"
      value={value}
      autoFocus
      disabled={saving}
      aria-label={`New price for ${product.name}`}
      onChange={(e) => setValue(e.target.value)}
      onKeyDown={(e) => {
        if (e.key === 'Enter') save()
        if (e.key === 'Escape') setEditing(false)
      }}
      onBlur={() => !saving && setEditing(false)}
    />
  )
}
