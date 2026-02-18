import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import SchoolsPage from '@/pages/schools/SchoolsPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/schools',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: SchoolsPage,
})
