import { createRouter } from '@tanstack/react-router'
import { Route as rootRoute } from './routes/__root'
import { Route as indexRoute } from './routes/index'
import { Route as loginRoute } from './routes/login'
import { Route as schoolsRoute } from './routes/schools'
import { Route as schoolsLevelsRoute } from './routes/schools.levels'
import { Route as productsRoute } from './routes/products'
import { Route as productsSizesRoute } from './routes/products.sizes'
import { Route as productsVariantsRoute } from './routes/products.variants'
import { Route as productsCombosRoute } from './routes/products.combos'
import { Route as commonProductsRoute } from './routes/supply-process.common-products'
import { Route as stockEntryRoute } from './routes/stock-entry'

const routeTree = rootRoute.addChildren([
  indexRoute,
  loginRoute,
  schoolsRoute,
  schoolsLevelsRoute,
  productsRoute,
  productsSizesRoute,
  productsVariantsRoute,
  productsCombosRoute,
  commonProductsRoute,
  stockEntryRoute,
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
