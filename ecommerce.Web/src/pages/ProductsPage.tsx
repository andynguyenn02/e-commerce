import { EmptyState, ErrorState, Skeleton } from '../components/ui/feedback'
import { Pagination } from '../components/Pagination'
import { ProductCard } from '../components/ProductCard'
import { ProductFilters } from '../components/ProductFilters'
import { useProductList } from '../hooks/useProductList'
import { useAppDispatch } from '../store/hooks'
import { setPage } from '../store/productsSlice'

const GRID = 'grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4'

export default function ProductsPage() {
  const dispatch = useAppDispatch()
  const { items, status, error, query, totalPages, totalItems, categoryNames, reload } = useProductList()

  return (
    <section>
      <div className="mb-5 flex items-baseline justify-between gap-4 rule-b pb-4">
        <h1 className="m-0">Products</h1>
        {status === 'success' && (
          <span className="text-sm tabular-nums text-muted">
            {totalItems} {totalItems === 1 ? 'item' : 'items'}
          </span>
        )}
      </div>

      <ProductFilters />

      {(status === 'idle' || status === 'loading') && (
        <div className={GRID} aria-busy="true">
          {Array.from({ length: query.pageSize }, (_, i) => (
            <Skeleton key={i} className="h-[340px] border border-rule bg-paper-sunk" />
          ))}
        </div>
      )}

      {status === 'error' && <ErrorState message={error} onRetry={reload} />}

      {status === 'success' && items.length === 0 && (
        <EmptyState>
          {query.search ? <>Nothing matches “{query.search}”. Try a different term or clear the filters.</> : 'No products yet.'}
        </EmptyState>
      )}

      {status === 'success' && items.length > 0 && (
        <>
          <div className={GRID}>
            {items.map((p) => (
              <ProductCard key={p.id} product={p} categoryName={categoryNames.get(p.categoryId)} />
            ))}
          </div>
          <Pagination
            page={query.pageNumber}
            totalPages={totalPages}
            onChange={(page) => dispatch(setPage(page))}
          />
        </>
      )}
    </section>
  )
}
