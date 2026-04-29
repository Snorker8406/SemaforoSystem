import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import AccountsPage from '@/pages/accounts/AccountsPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/accounts',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: AccountsPage,
})
