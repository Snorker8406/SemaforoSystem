import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import ProvidersPage from '@/pages/providers/ProvidersPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/proveedores',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: ProvidersPage,
})
