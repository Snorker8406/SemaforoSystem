import { createRouter } from '@tanstack/react-router'
import { Route as rootRoute } from './routes/__root'
import { Route as indexRoute } from './routes/index'
import { Route as loginRoute } from './routes/login'

const routeTree = rootRoute.addChildren([indexRoute, loginRoute])

export const router = createRouter({
  routeTree,
  context: {
    queryClient: undefined!,  // Se inyecta en el provider
  },
  defaultPreload: 'intent',
})

// Tipado para TanStack Router
declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}
