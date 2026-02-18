import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import { getMe } from '@/services/auth-service'
import SchoolLevelsPage from '@/pages/schools/SchoolLevelsPage'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/schools/levels',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: SchoolLevelsPage,
})
