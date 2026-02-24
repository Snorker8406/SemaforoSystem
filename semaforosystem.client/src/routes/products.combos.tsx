import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import CombosPage from '@/pages/combos/CombosPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/products/combos',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: CombosPage,
})
