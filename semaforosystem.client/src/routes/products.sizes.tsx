import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import SizeSystemsPage from '@/pages/sizes/SizeSystemsPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/products/sizes',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: SizeSystemsPage,
})
