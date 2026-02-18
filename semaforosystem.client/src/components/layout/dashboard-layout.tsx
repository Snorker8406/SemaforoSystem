import type { ReactNode } from 'react'

import { SidebarProvider } from '@/components/ui/sidebar'
import AppSidebar from '@/components/layout/app-sidebar'
import AppHeader from '@/components/layout/app-header'
import AppFooter from '@/components/layout/app-footer'

interface DashboardLayoutProps {
  children: ReactNode
}

const DashboardLayout = ({ children }: DashboardLayoutProps) => {
  return (
    <div className='flex min-h-dvh w-full'>
      <SidebarProvider>
        <AppSidebar />
        <div className='flex flex-1 flex-col'>
          <AppHeader />
          <main className='mx-auto size-full max-w-7xl flex-1 px-4 py-6 sm:px-6'>
            {children}
          </main>
          <AppFooter />
        </div>
      </SidebarProvider>
    </div>
  )
}

export default DashboardLayout
