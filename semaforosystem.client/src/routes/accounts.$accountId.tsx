import { createRoute, redirect } from '@tanstack/react-router'
import { Route as rootRoute } from './__root'
import AccountDetailPage from '@/pages/accounts/AccountDetailPage'
import { getMe } from '@/services/auth-service'

export const Route = createRoute({
  getParentRoute: () => rootRoute,
  path: '/accounts/$accountId',
  beforeLoad: async () => {
    try {
      await getMe()
    } catch {
      throw redirect({ to: '/login' })
    }
  },
  component: AccountDetailPage,
})
