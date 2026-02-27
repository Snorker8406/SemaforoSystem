import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import EmbroideriesPage from '@/pages/embroideries/EmbroideriesPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/embroideries',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: EmbroideriesPage,
})
