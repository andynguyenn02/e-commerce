import { useEffect, useMemo } from 'react'
import { useAppDispatch, useAppSelector } from '../store/hooks'
import { fetchCategories, fetchProducts } from '../store/productsSlice'

// Shared by the shop grid and the admin table: both read the products slice.
export function useProductList() {
  const dispatch = useAppDispatch()
  const state = useAppSelector((s) => s.products)
  const { query, categories, categoriesStatus } = state

  // Refetch whenever the filters change. Aborting the previous request means a
  // slow old response can never overwrite a newer one.
  useEffect(() => {
    const request = dispatch(fetchProducts())
    return () => request.abort()
  }, [dispatch, query])

  useEffect(() => {
    if (categoriesStatus === 'idle') dispatch(fetchCategories())
  }, [categoriesStatus, dispatch])

  const categoryNames = useMemo(() => new Map(categories.map((c) => [c.id, c.name])), [categories])

  return { ...state, categoryNames, reload: () => dispatch(fetchProducts()) }
}
