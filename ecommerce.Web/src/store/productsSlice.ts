import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { getCategories } from '../api/categories'
import { getErrorMessage } from '../api/client'
import { getProducts } from '../api/products'
import type { Category, PagedResult, Product, ProductQuery } from '../api/types'
import { initialAsyncState, type AsyncState } from './asyncStatus'
import type { RootState } from './index'

interface ProductsState extends AsyncState {
  items: Product[]
  totalItems: number
  totalPages: number
  // Filters live in the store (not the page) so leaving and coming back keeps them.
  query: ProductQuery
  categories: Category[]
  categoriesStatus: AsyncState['status']
}

const initialState: ProductsState = {
  ...initialAsyncState,
  items: [],
  totalItems: 0,
  totalPages: 0,
  query: { search: '', categoryId: null, pageNumber: 1, pageSize: 12 },
  categories: [],
  categoriesStatus: 'idle',
}

export const fetchProducts = createAsyncThunk<
  PagedResult<Product>,
  void,
  { state: RootState; rejectValue: string }
>('products/fetch', async (_, { getState, signal, rejectWithValue }) => {
  try {
    return await getProducts(getState().products.query, signal)
  } catch (err) {
    return rejectWithValue(getErrorMessage(err))
  }
})

export const fetchCategories = createAsyncThunk('products/fetchCategories', getCategories)

const productsSlice = createSlice({
  name: 'products',
  initialState,
  reducers: {
    // Changing a filter jumps back to page 1: page 5 of the old results may not exist.
    setSearch(state, action: PayloadAction<string>) {
      state.query.search = action.payload
      state.query.pageNumber = 1
    },
    setCategory(state, action: PayloadAction<string | null>) {
      state.query.categoryId = action.payload
      state.query.pageNumber = 1
    },
    setPage(state, action: PayloadAction<number>) {
      state.query.pageNumber = action.payload
    },
    // After an inline price edit succeeds, patch the row instead of refetching the page.
    productPriceUpdated(state, action: PayloadAction<{ id: string; price: number }>) {
      const product = state.items.find((p) => p.id === action.payload.id)
      if (product) product.price = action.payload.price
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchProducts.pending, (state) => {
        state.status = 'loading'
        state.error = null
      })
      .addCase(fetchProducts.fulfilled, (state, action) => {
        state.status = 'success'
        state.items = action.payload.items
        state.totalItems = action.payload.totalItems
        state.totalPages = action.payload.totalPages
      })
      .addCase(fetchProducts.rejected, (state, action) => {
        // Superseded requests are aborted by the page; their result is irrelevant.
        if (action.meta.aborted) return
        state.status = 'error'
        state.error = action.payload ?? 'Could not load products'
      })
      .addCase(fetchCategories.pending, (state) => {
        state.categoriesStatus = 'loading'
      })
      .addCase(fetchCategories.fulfilled, (state, action) => {
        state.categoriesStatus = 'success'
        state.categories = action.payload
      })
      .addCase(fetchCategories.rejected, (state) => {
        state.categoriesStatus = 'error'
      })
  },
})

export const { setSearch, setCategory, setPage, productPriceUpdated } = productsSlice.actions
export default productsSlice.reducer
