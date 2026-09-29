import { useState } from 'react'
import { toast } from 'sonner'
import { Minus, Plus, Trash2 } from 'lucide-react'
import type { CartItem } from '../api/types'
import { addToCart, removeCartItem, toggleItemSelected, updateCartQuantity } from '../store/cartSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { formatMoney } from '../utils/format'
import { Button } from './ui/button'
import { Td } from './ui/table'
import { cn } from '../lib/cn'

export function CartRow({ item }: { item: CartItem }) {
  const dispatch = useAppDispatch()
  const busy = useAppSelector(
    (s) => s.cart.busyItemIds.includes(item.id) || s.cart.checkoutStatus === 'loading',
  )
  const selected = useAppSelector((s) => s.cart.selectedItemIds.includes(item.id))
  // What the user is typing; the saved quantity is item.quantity.
  const [draft, setDraft] = useState(String(item.quantity))

  const commit = async (next: number) => {
    const quantity = Math.max(1, Math.floor(next) || 1)
    setDraft(String(quantity))
    if (quantity === item.quantity) return

    const result = await dispatch(updateCartQuantity({ itemId: item.id, quantity }))
    if (updateCartQuantity.rejected.match(result)) {
      toast.error(result.payload ?? 'Could not update quantity')
      setDraft(String(item.quantity)) // back to what the server has
    }
  }

  const remove = async () => {
    const result = await dispatch(removeCartItem({ itemId: item.id }))
    if (removeCartItem.rejected.match(result)) {
      toast.error(result.payload ?? 'Could not remove item')
      return
    }
    toast(`Removed ${item.productName}`, {
      action: {
        label: 'Undo',
        onClick: async () => {
          const undo = await dispatch(addToCart({ productId: item.productId, quantity: item.quantity }))
          if (addToCart.rejected.match(undo)) toast.error(undo.payload ?? 'Could not restore item')
        },
      },
    })
  }

  const overStock = item.quantity > item.availableQuantity

  return (
    <tr className={cn(busy && 'opacity-50')}>
      <Td className="w-10">
        <input
          type="checkbox"
          checked={selected}
          disabled={busy}
          aria-label={`Select ${item.productName} for checkout`}
          onChange={() => dispatch(toggleItemSelected(item.id))}
          className="size-4 accent-ink cursor-pointer"
        />
      </Td>
      <Td>
        <div className="font-medium">{item.productName}</div>
        <div className="font-mono text-[12px] text-muted">{item.productCode}</div>
        {overStock && (
          <div className="text-[12px] text-accent">
            Only {item.availableQuantity} left in stock, please reduce the quantity
          </div>
        )}
      </Td>
      <Td numeric className="tabular-nums">
        {formatMoney(item.unitPrice)}
      </Td>
      <Td>
        <div className="inline-flex items-center rounded-sm border border-rule">
          <Button
            variant="ghost"
            size="sm"
            aria-label="Decrease"
            disabled={busy || item.quantity <= 1}
            onClick={() => commit(item.quantity - 1)}
          >
            <Minus size={14} />
          </Button>
          <input
            type="number"
            min={1}
            value={draft}
            disabled={busy}
            aria-label="Quantity"
            className="h-8 w-12 border-0 bg-transparent text-center text-sm disabled:text-muted"
            onChange={(e) => setDraft(e.target.value)}
            onBlur={() => commit(Number(draft))}
            onKeyDown={(e) => {
              if (e.key === 'Enter') e.currentTarget.blur()
              if (e.key === 'Escape') setDraft(String(item.quantity))
            }}
          />
          <Button
            variant="ghost"
            size="sm"
            aria-label="Increase"
            disabled={busy || item.quantity >= item.availableQuantity}
            onClick={() => commit(item.quantity + 1)}
          >
            <Plus size={14} />
          </Button>
        </div>
      </Td>
      <Td numeric className="font-medium tabular-nums">
        {formatMoney(item.totalPrice)}
      </Td>
      <Td numeric>
        <Button
          variant="quiet"
          size="sm"
          disabled={busy}
          onClick={remove}
          className="hover:text-accent hover:decoration-accent"
        >
          <Trash2 size={15} />
          Remove
        </Button>
      </Td>
    </tr>
  )
}
