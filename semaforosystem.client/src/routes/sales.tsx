import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import SalesPage from '@/pages/sales/SalesPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/sales',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: SalesPage,
})
