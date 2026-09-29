import { useEffect, useRef, useState } from 'react'
import { toast } from 'sonner'
import { Plus } from 'lucide-react'
import { getErrorMessage } from '../../api/client'
import { deleteProduct } from '../../api/products'
import type { Product } from '../../api/types'
import { PriceCell } from '../../components/admin/PriceCell'
import { ProductFormModal } from '../../components/admin/ProductFormModal'
import { ConfirmDialog } from '../../components/ConfirmDialog'
import { Pagination } from '../../components/Pagination'
import { ProductFilters } from '../../components/ProductFilters'
import { Button } from '../../components/ui/button'
import { EmptyState, ErrorState, Spinner } from '../../components/ui/feedback'
import { TableFrame, Th, Td } from '../../components/ui/table'
import { cn } from '../../lib/cn'
import { useProductList } from '../../hooks/useProductList'
import { useAppDispatch } from '../../store/hooks'
import { setPage } from '../../store/productsSlice'

type FormState = { mode: 'closed' } | { mode: 'create' } | { mode: 'edit'; product: Product }

export default function AdminProductsPage() {
  const dispatch = useAppDispatch()
  const { items, status, error, query, totalPages, totalItems, categories, categoryNames, reload } =
    useProductList()

  const [form, setForm] = useState<FormState>({ mode: 'closed' })
  const [toDelete, setToDelete] = useState<Product | null>(null)
  const [deleting, setDeleting] = useState(false)
  const [flashId, setFlashId] = useState<string | null>(null)
  const flashTimer = useRef<number | undefined>(undefined)

  useEffect(() => () => window.clearTimeout(flashTimer.current), [])

  // Deleting the last row of the last page would leave an empty page: step back.
  useEffect(() => {
    if (status === 'success' && items.length === 0 && query.pageNumber > 1) {
      dispatch(setPage(query.pageNumber - 1))
    }
  }, [status, items.length, query.pageNumber, dispatch])

  const flash = (id: string) => {
    setFlashId(id)
    window.clearTimeout(flashTimer.current)
    flashTimer.current = window.setTimeout(() => setFlashId(null), 1500)
  }

  const onConfirmDelete = async () => {
    if (!toDelete) return
    setDeleting(true)
    try {
      await deleteProduct(toDelete.id)
      toast.success(`Deleted ${toDelete.name}`)
      setToDelete(null)
      reload()
    } catch (err) {
      toast.error(getErrorMessage(err))
    } finally {
      setDeleting(false)
    }
  }

  return (
    <section>
      <div className="mb-5 flex items-baseline justify-between gap-4 rule-b pb-4">
        <div className="flex items-baseline gap-3">
          <h1 className="m-0">Manage products</h1>
          {status === 'success' && <span className="text-sm tabular-nums text-muted">{totalItems} products</span>}
        </div>
        <Button size="sm" onClick={() => setForm({ mode: 'create' })}>
          <Plus size={15} />
          New product
        </Button>
      </div>

      <ProductFilters />

      {(status === 'idle' || status === 'loading') && <Spinner label="Loading products" />}

      {status === 'error' && <ErrorState message={error} onRetry={reload} />}

      {status === 'success' && items.length === 0 && query.pageNumber === 1 && (
        <EmptyState>No products found.</EmptyState>
      )}

      {status === 'success' && items.length > 0 && (
        <>
          <TableFrame>
            <thead>
              <tr>
                <Th>Code</Th>
                <Th>Name</Th>
                <Th>Category</Th>
                <Th numeric>Price</Th>
                <Th numeric>Stock</Th>
                <Th numeric>Actions</Th>
              </tr>
            </thead>
            <tbody>
              {items.map((p) => (
                <tr key={p.id} className={cn(flashId === p.id && 'bg-positive-wash transition-colors duration-1000')}>
                  <Td className="font-mono text-[13px]">{p.code}</Td>
                  <Td className="font-medium">{p.name}</Td>
                  <Td>{categoryNames.get(p.categoryId) ?? '—'}</Td>
                  <Td numeric>
                    <PriceCell
                      product={p}
                      onSaved={() => {
                        flash(p.id)
                        toast.success(`Price updated for ${p.name}`)
                      }}
                    />
                  </Td>
                  <Td numeric className={p.availableQuantity === 0 ? 'text-accent' : undefined}>
                    {p.availableQuantity}
                  </Td>
                  <Td numeric>
                    <div className="flex justify-end gap-1">
                      <Button variant="quiet" size="sm" onClick={() => setForm({ mode: 'edit', product: p })}>
                        Edit
                      </Button>
                      <Button variant="quiet" size="sm" className="hover:text-accent" onClick={() => setToDelete(p)}>
                        Delete
                      </Button>
                    </div>
                  </Td>
                </tr>
              ))}
            </tbody>
          </TableFrame>
          <Pagination
            page={query.pageNumber}
            totalPages={totalPages}
            onChange={(page) => dispatch(setPage(page))}
          />
        </>
      )}

      {form.mode !== 'closed' && (
        <ProductFormModal
          product={form.mode === 'edit' ? form.product : null}
          categories={categories}
          onClose={() => setForm({ mode: 'closed' })}
          onSaved={(message) => {
            toast.success(message)
            if (form.mode === 'edit') flash(form.product.id)
            setForm({ mode: 'closed' })
            reload()
          }}
        />
      )}

      {toDelete && (
        <ConfirmDialog
          title="Delete product?"
          confirmLabel="Delete"
          busy={deleting}
          onConfirm={onConfirmDelete}
          onCancel={() => setToDelete(null)}
        >
          <p>
            <strong>{toDelete.name}</strong> ({toDelete.code}) will be hidden from the shop.
          </p>
          <p className="mt-1 text-[13px] text-muted">
            This is a soft delete: past orders keep showing this product and the price paid.
          </p>
        </ConfirmDialog>
      )}
    </section>
  )
}
