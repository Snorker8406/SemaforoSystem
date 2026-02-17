import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import LoginPage from '@/components/blocks/login/login-page'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  beforeLoad: async () => {
    // Si ya está autenticado, redirigir al dashboard
    try {
      await getMe()
      throw redirect({ to: '/' })
    } catch (err) {
      if (err instanceof Response || (err as { to?: string })?.to === '/') {
        throw err
      }
      // No autenticado, permitir acceso al login
    }
  },
  component: LoginPage,
})
