import { createBrowserRouter, Navigate } from 'react-router-dom'
import { Layout } from '../components/Layout'
import LoginPage from '../pages/LoginPage'
import ProductsPage from '../pages/ProductsPage'
import CartPage from '../pages/CartPage'
import OrdersPage from '../pages/OrdersPage'
import OrderDetailPage from '../pages/OrderDetailPage'
import WalletPage from '../pages/WalletPage'
import NotFoundPage from '../pages/NotFoundPage'
import AdminProductsPage from '../pages/admin/AdminProductsPage'
import AdminInventoryPage from '../pages/admin/AdminInventoryPage'
import { RequireRole } from './RequireRole'

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    element: <Layout />,
    children: [
      { index: true, element: <Navigate to="/products" replace /> },
      { path: '/products', element: <ProductsPage /> },
      { path: '/cart', element: <RequireRole roles={['Customer']}><CartPage /></RequireRole> },
      { path: '/orders', element: <RequireRole roles={['Customer']}><OrdersPage /></RequireRole> },
      { path: '/orders/:id', element: <RequireRole roles={['Customer']}><OrderDetailPage /></RequireRole> },
      { path: '/wallet', element: <RequireRole roles={['Customer']}><WalletPage /></RequireRole> },
      { path: '/admin/products', element: <RequireRole roles={['Admin']}><AdminProductsPage /></RequireRole> },
      { path: '/admin/inventory', element: <RequireRole roles={['Admin']}><AdminInventoryPage /></RequireRole> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
])
