import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import CommonProductsPage from '@/pages/supply-process/CommonProductsPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/supply-process/common-products',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: CommonProductsPage,
})
