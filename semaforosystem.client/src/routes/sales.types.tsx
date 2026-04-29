import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import SaleTypesPage from '@/pages/sales/SaleTypesPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/sales/types',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: SaleTypesPage,
})
