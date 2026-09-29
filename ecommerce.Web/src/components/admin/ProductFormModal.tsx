import { useState, type FormEvent } from 'react'
import { getErrorMessage } from '../../api/client'
import { createProduct, updateProduct } from '../../api/products'
import type { Category, Product, ProductInput } from '../../api/types'
import { Dialog, DialogActions } from '../ui/dialog'
import { Button } from '../ui/button'
import { Input, Select, Field } from '../ui/field'

interface Props {
  product: Product | null // null = create
  categories: Category[]
  onClose: () => void
  onSaved: (message: string) => void
}

type Draft = { name: string; code: string; price: string; availableQuantity: string; categoryId: string }
type Errors = Partial<Record<keyof Draft, string>>

// Mirrors the backend FluentValidation rules for fast feedback only.
// The backend still validates everything — try `price: -5` with curl.
function validate(d: Draft): Errors {
  const errors: Errors = {}
  if (!d.name.trim()) errors.name = 'Name is required'
  else if (d.name.trim().length > 200) errors.name = 'Max 200 characters'
  if (!d.code.trim()) errors.code = 'Code is required'
  else if (d.code.trim().length > 50) errors.code = 'Max 50 characters'
  const price = Number(d.price)
  if (d.price.trim() === '' || !Number.isFinite(price) || price <= 0) errors.price = 'Price must be greater than 0'
  const qty = Number(d.availableQuantity)
  if (d.availableQuantity.trim() === '' || !Number.isInteger(qty) || qty < 0)
    errors.availableQuantity = 'Quantity must be a whole number ≥ 0'
  if (!d.categoryId) errors.categoryId = 'Choose a category'
  return errors
}

export function ProductFormModal({ product, categories, onClose, onSaved }: Props) {
  const [draft, setDraft] = useState<Draft>({
    name: product?.name ?? '',
    code: product?.code ?? '',
    price: product ? String(product.price) : '',
    availableQuantity: product ? String(product.availableQuantity) : '0',
    categoryId: product?.categoryId ?? '',
  })
  const [touched, setTouched] = useState(false)
  const [saving, setSaving] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  const errors = validate(draft)
  const shownErrors = touched ? errors : {}

  const set = (field: keyof Draft) => (e: { target: { value: string } }) =>
    setDraft((d) => ({ ...d, [field]: e.target.value }))

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setTouched(true)
    if (Object.keys(errors).length > 0) return

    const body: ProductInput = {
      name: draft.name.trim(),
      code: draft.code.trim(),
      price: Number(draft.price),
      availableQuantity: Number(draft.availableQuantity),
      categoryId: draft.categoryId,
    }
    setSaving(true)
    setServerError(null)
    try {
      if (product) await updateProduct(product.id, body)
      else await createProduct(body)
      onSaved(product ? `Updated ${body.name}` : `Created ${body.name}`)
    } catch (err) {
      setServerError(getErrorMessage(err)) // e.g. "Product code 'X' already exists"
      setSaving(false)
    }
  }

  return (
    <Dialog title={product ? 'Edit product' : 'New product'} onClose={onClose} locked={saving}>
      <form onSubmit={onSubmit} noValidate>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Name" error={shownErrors.name} className="sm:col-span-2">
            <Input
              value={draft.name}
              onChange={set('name')}
              autoFocus
              maxLength={200}
              aria-invalid={!!shownErrors.name}
            />
          </Field>
          <Field label="Code" error={shownErrors.code}>
            <Input value={draft.code} onChange={set('code')} maxLength={50} aria-invalid={!!shownErrors.code} />
          </Field>
          <Field label="Category" error={shownErrors.categoryId}>
            <Select value={draft.categoryId} onChange={set('categoryId')} aria-invalid={!!shownErrors.categoryId}>
              <option value="">Select...</option>
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Price" error={shownErrors.price}>
            <Input
              type="number"
              min={0}
              step="any"
              value={draft.price}
              onChange={set('price')}
              aria-invalid={!!shownErrors.price}
            />
          </Field>
          <Field label="Quantity in stock" error={shownErrors.availableQuantity}>
            <Input
              type="number"
              min={0}
              step={1}
              value={draft.availableQuantity}
              onChange={set('availableQuantity')}
              aria-invalid={!!shownErrors.availableQuantity}
            />
          </Field>

          {serverError && (
            <p className="rounded-sm border border-accent/30 bg-accent-wash px-3 py-2 text-[13px] text-accent-ink sm:col-span-2">
              {serverError}
            </p>
          )}
        </div>

        <DialogActions>
          <Button type="button" variant="outline" disabled={saving} onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={saving}>
            {saving ? 'Saving' : product ? 'Save changes' : 'Create product'}
          </Button>
        </DialogActions>
      </form>
    </Dialog>
  )
}
