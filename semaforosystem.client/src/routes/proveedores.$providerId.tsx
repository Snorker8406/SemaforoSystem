import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import ProviderDetailPage from '@/pages/providers/ProviderDetailPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/proveedores/$providerId',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: ProviderDetailPage,
})
