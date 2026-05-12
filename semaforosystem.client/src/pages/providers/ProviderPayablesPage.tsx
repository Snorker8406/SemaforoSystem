import DashboardLayout from '@/components/layout/dashboard-layout'
import ProviderPayablesTab from './provider-payables-tab'

export default function ProviderPayablesPage() {
  return (
    <DashboardLayout>
      <div className='space-y-6'>
        <div>
          <h1 className='text-2xl font-bold tracking-tight'>Cuentas por pagar</h1>
          <p className='text-muted-foreground text-sm'>
            Administración global de obligaciones con proveedores. Para crear cuentas
            entra al detalle del proveedor correspondiente.
          </p>
        </div>
        <ProviderPayablesTab title='Todas las cuentas por pagar' />
      </div>
    </DashboardLayout>
  )
}
