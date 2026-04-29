import { useQuery } from '@tanstack/react-query'
import { TagIcon } from 'lucide-react'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'

import { getSaleTypes } from '@/services/sale-service'

const SaleTypesPage = () => {
  const { data: types, isLoading } = useQuery({
    queryKey: ['sale-types'],
    queryFn: getSaleTypes,
  })

  return (
    <DashboardLayout>
      <div className='flex flex-col gap-6 p-6'>
        <div>
          <h1 className='text-2xl font-bold tracking-tight'>Tipos de Venta</h1>
          <p className='text-muted-foreground text-sm'>
            Catálogo de tipos de venta registrados en el sistema
          </p>
        </div>

        {isLoading ? (
          <div className='grid gap-4 sm:grid-cols-2 lg:grid-cols-3'>
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className='h-28 rounded-xl' />
            ))}
          </div>
        ) : (
          <div className='grid gap-4 sm:grid-cols-2 lg:grid-cols-3'>
            {types?.map((type) => (
              <Card key={type.id}>
                <CardHeader className='pb-2 flex flex-row items-center gap-3'>
                  <div className='bg-primary/10 flex size-10 items-center justify-center rounded-lg'>
                    <TagIcon className='text-primary size-5' />
                  </div>
                  <div>
                    <CardTitle className='text-base'>{type.name}</CardTitle>
                    <Badge variant='outline' className='mt-1 text-xs'>
                      {type.code}
                    </Badge>
                  </div>
                </CardHeader>
                <CardContent>
                  <p className='text-muted-foreground text-sm'>
                    {type.description ?? 'Sin descripción.'}
                  </p>
                  <Badge
                    variant={type.active ? 'default' : 'secondary'}
                    className='mt-2 text-xs'
                  >
                    {type.active ? 'Activo' : 'Inactivo'}
                  </Badge>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </DashboardLayout>
  )
}

export default SaleTypesPage
