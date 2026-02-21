import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import ProductsPage from '@/pages/products/ProductsPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/products',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: ProductsPage,
})
