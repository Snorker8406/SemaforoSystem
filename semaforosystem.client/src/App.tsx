import { TooltipProvider } from '@/components/ui/tooltip'
import DashboardShell from '@/pages/DashboardShell'

function App() {
  return (
    <TooltipProvider>
      <DashboardShell />
    </TooltipProvider>
  )
}

export default App
