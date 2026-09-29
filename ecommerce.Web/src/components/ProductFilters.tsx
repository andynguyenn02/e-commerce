import { useEffect, useState } from 'react'
import { Search } from 'lucide-react'
import { useDebouncedValue } from '../hooks/useDebouncedValue'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { setCategory, setSearch } from '../store/productsSlice'
import { Input, Select } from './ui/field'

export function ProductFilters() {
  const dispatch = useAppDispatch()
  const { query, categories, categoriesStatus } = useAppSelector((s) => s.products)

  const [searchInput, setSearchInput] = useState(query.search)
  const debouncedSearch = useDebouncedValue(searchInput.trim(), 400)

  useEffect(() => {
    if (debouncedSearch !== query.search) dispatch(setSearch(debouncedSearch))
  }, [debouncedSearch, query.search, dispatch])

  return (
    <div className="mb-6 flex flex-wrap gap-3">
      <div className="relative min-w-52 flex-1">
        <Search size={15} className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-muted" />
        <Input
          type="search"
          placeholder="Search by name or code"
          value={searchInput}
          className="pl-8"
          onChange={(e) => setSearchInput(e.target.value)}
        />
      </div>
      <Select
        value={query.categoryId ?? ''}
        onChange={(e) => dispatch(setCategory(e.target.value || null))}
        disabled={categoriesStatus !== 'success'}
        aria-label="Category"
        className="w-auto min-w-40"
      >
        <option value="">All categories</option>
        {categories.map((c) => (
          <option key={c.id} value={c.id}>
            {c.name}
          </option>
        ))}
      </Select>
    </div>
  )
}
