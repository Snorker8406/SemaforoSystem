import { TooltipProvider } from '@/components/ui/tooltip'
import Login from '@/pages/LoginPage'
// import DashboardShell from '@/pages/DashboardShell'

function App() {
  return (
    <TooltipProvider>
      {/* TODO: Add routing - for now showing Login page */}
      <Login />
      {/* <DashboardShell /> */}
    </TooltipProvider>
  )
}

export default App
