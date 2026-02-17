import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import DashboardShell from '@/pages/DashboardShell'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  beforeLoad: async () => {
    // Verificar autenticación via cookie
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: DashboardShell,
})
