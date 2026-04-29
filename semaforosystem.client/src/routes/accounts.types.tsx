import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import AccountTypesPage from '@/pages/accounts/AccountTypesPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/accounts/types',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: AccountTypesPage,
})
