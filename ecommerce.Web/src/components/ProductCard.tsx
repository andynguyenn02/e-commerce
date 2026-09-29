import { useState } from 'react'
import { toast } from 'sonner'
import { useLocation, useNavigate } from 'react-router-dom'
import type { Product } from '../api/types'
import { addToCart } from '../store/cartSlice'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { formatMoney } from '../utils/format'
import { ProductThumb } from './ProductThumb'
import { Button, ButtonLink } from './ui/button'
import { Input } from './ui/field'
import { cn } from '../lib/cn'

export function ProductCard({ product, categoryName }: { product: Product; categoryName?: string }) {
  const dispatch = useAppDispatch()
  const user = useAppSelector((s) => s.auth.user)
  const navigate = useNavigate()
  const location = useLocation()
  const [quantity, setQuantity] = useState(1)
  const [adding, setAdding] = useState(false)

  const outOfStock = product.availableQuantity <= 0
  const lowStock = !outOfStock && product.availableQuantity <= 5

  const onAdd = async () => {
    if (!user) {
      navigate('/login', { state: { from: location.pathname } })
      return
    }
    setAdding(true)
    const result = await dispatch(addToCart({ productId: product.id, quantity }))
    setAdding(false)
    if (addToCart.fulfilled.match(result)) {
      toast.success(`Added ${quantity} × ${product.name} to cart`)
      setQuantity(1)
    } else {
      // e.g. "USB-C Hub: only 3 left" straight from the backend
      toast.error(result.payload ?? 'Could not add to cart')
    }
  }

  return (
    <article className="group flex flex-col rounded-sm border border-rule transition-colors hover:border-ink">
      <ProductThumb code={product.code} name={product.name} className="rule-b rounded-t-sm" />

      <div className="flex flex-1 flex-col gap-3 p-4">
        <div className="flex items-start justify-between gap-2">
          <span className="font-mono text-[12px] text-muted">{product.code}</span>
          {categoryName && <span className="text-[12px] text-muted">{categoryName}</span>}
        </div>

        <h3 className="m-0 flex-1">{product.name}</h3>

        <div className="flex items-baseline justify-between gap-2 rule-t pt-3">
          <span className="font-display text-xl font-semibold tabular-nums">
            {formatMoney(product.price)}
          </span>
          <span
            className={cn(
              'text-[13px] tabular-nums',
              outOfStock ? 'text-accent' : lowStock ? 'text-caution' : 'text-muted',
            )}
          >
            {outOfStock
              ? 'Out of stock'
              : lowStock
                ? `Only ${product.availableQuantity} left`
                : `${product.availableQuantity} in stock`}
          </span>
        </div>

        {user?.role === 'Admin' ? (
          // Price editing lives on the admin products page (Feature 6).
          <ButtonLink to="/admin/products" variant="outline" size="sm">
            Edit price
          </ButtonLink>
        ) : (
          <div className="flex gap-2">
            <Input
              type="number"
              min={1}
              max={product.availableQuantity}
              value={quantity}
              disabled={outOfStock}
              aria-label="Quantity"
              className="w-16 text-center"
              onChange={(e) => {
                const n = Math.floor(Number(e.target.value))
                setQuantity(Math.min(Math.max(n || 1, 1), Math.max(product.availableQuantity, 1)))
              }}
            />
            <Button className="flex-1" disabled={outOfStock || adding} onClick={onAdd}>
              {adding ? 'Adding' : 'Add to cart'}
            </Button>
          </div>
        )}
      </div>
    </article>
  )
}
