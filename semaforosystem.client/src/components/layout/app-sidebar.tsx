import type { ComponentType, SVGAttributes } from 'react'

import {
  ArrowRightLeftIcon,
  CalendarClockIcon,
  ChartNoAxesCombinedIcon,
  ChartPieIcon,
  ChartSplineIcon,
  ClipboardListIcon,
  Clock9Icon,
  CrownIcon,
  HashIcon,
  SchoolIcon,
  SettingsIcon,
  SquareActivityIcon,
  TruckIcon,
  Undo2Icon,
  UsersIcon,
} from 'lucide-react'

import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
} from '@/components/ui/sidebar'

// ── Tipos ────────────────────────────────────────────────

interface NavItem {
  label: string
  href: string
  icon: ComponentType<SVGAttributes<SVGElement>>
  badge?: string | number
}

interface NavGroup {
  label?: string
  items: NavItem[]
}

// ── Configuración del menú ───────────────────────────────

const navigation: NavGroup[] = [
  {
    items: [
      { label: 'Dashboard', href: '/', icon: ChartNoAxesCombinedIcon, badge: 5 },
    ],
  },
  {
    label: 'Administracion',
    items: [
      { label: 'Proveedores', href: '/proveedores', icon: TruckIcon },
      { label: 'Escuelas', href: '/escuelas', icon: SchoolIcon },
      { label: 'Content Performance', href: '#', icon: ChartSplineIcon },
      { label: 'Audience Insight', href: '#', icon: UsersIcon },
      { label: 'Engagement Metrics', href: '#', icon: ChartPieIcon },
      { label: 'Hashtag Performance', href: '#', icon: HashIcon, badge: 3 },
      { label: 'Competitor Analysis', href: '#', icon: ArrowRightLeftIcon },
      { label: 'Campaign Tracking', href: '#', icon: Clock9Icon },
      { label: 'Sentiment Tracking', href: '#', icon: ClipboardListIcon },
      { label: 'Influencer', href: '#', icon: CrownIcon },
    ],
  },
  {
    label: 'Supporting Features',
    items: [
      { label: 'Real Time Monitoring', href: '#', icon: SquareActivityIcon },
      { label: 'Schedule Post & Calendar', href: '#', icon: CalendarClockIcon },
      { label: 'Report & Export', href: '#', icon: Undo2Icon },
      { label: 'Settings & Integrations', href: '#', icon: SettingsIcon },
      { label: 'User Management', href: '#', icon: UsersIcon },
    ],
  },
]

// ── Componente ───────────────────────────────────────────

const AppSidebar = () => {
  return (
    <Sidebar>
      <SidebarContent>
        {navigation.map((group, groupIndex) => (
          <SidebarGroup key={groupIndex}>
            {group.label && <SidebarGroupLabel>{group.label}</SidebarGroupLabel>}
            <SidebarGroupContent>
              <SidebarMenu>
                {group.items.map((item) => (
                  <SidebarMenuItem key={item.label}>
                    <SidebarMenuButton asChild>
                      <a href={item.href}>
                        <item.icon />
                        <span>{item.label}</span>
                      </a>
                    </SidebarMenuButton>
                    {item.badge !== undefined && (
                      <SidebarMenuBadge className='bg-primary/10 rounded-full'>
                        {item.badge}
                      </SidebarMenuBadge>
                    )}
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        ))}
      </SidebarContent>
    </Sidebar>
  )
}

export default AppSidebar
