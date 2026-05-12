import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import ProviderPayablesPage from '@/pages/providers/ProviderPayablesPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/proveedores/cuentas-por-pagar',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: ProviderPayablesPage,
})
