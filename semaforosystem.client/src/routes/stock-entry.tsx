import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import StockEntryPage from '@/pages/stock-entry/StockEntryPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/stock-entry',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: StockEntryPage,
})
