import { createRouter } from '@tanstack/react-router'
import { Route as rootRoute } from './routes/__root'
import { Route as indexRoute } from './routes/index'
import { Route as loginRoute } from './routes/login'
import { Route as schoolsRoute } from './routes/schools'
import { Route as schoolsLevelsRoute } from './routes/schools.levels'

const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  schoolsRoute,
  schoolsLevelsRoute,
])

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
