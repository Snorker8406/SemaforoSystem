import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import SaleDetailPage from '@/pages/sales/SaleDetailPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/sales/$saleId',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: SaleDetailPage,
})
