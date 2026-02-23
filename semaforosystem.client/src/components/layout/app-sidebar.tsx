import type { ComponentType, SVGAttributes } from 'react'
import { Link } from '@tanstack/react-router'
import { ChevronRight } from 'lucide-react'

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
  PackageIcon,
  PackageSearchIcon,
  SchoolIcon,
  SettingsIcon,
  SquareActivityIcon,
  TruckIcon,
  Undo2Icon,
  UsersIcon,
} from 'lucide-react'

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
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
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
} from '@/components/ui/sidebar'

// ── Tipos ────────────────────────────────────────────────

interface NavSubItem {
  label: string
  href: string
}

interface NavItem {
  label: string
  href: string
  icon: ComponentType<SVGAttributes<SVGElement>>
  badge?: string | number
  isActive?: boolean
  children?: NavSubItem[]
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
      {
        label: 'Escuelas',
        href: '#',
        icon: SchoolIcon,
        isActive: true,
        children: [
          { label: 'Administrar Escuelas', href: '/schools' },
          { label: 'Niveles', href: '/schools/levels' },
        ],
      },
      {
        label: 'Productos',
        href: '#',
        icon: PackageIcon,
        children: [
          { label: 'Administrar Productos', href: '/products' },
          { label: 'Administrar Tallas', href: '/products/sizes' },
          { label: 'Administrar Variantes', href: '/products/variants' },
        ],
      },
      {
        label: 'Proceso Surtido',
        href: '#',
        icon: PackageSearchIcon,
        children: [
          { label: 'Productos Comunes', href: '/supply-process/common-products' },
        ],
      },
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
    <Sidebar collapsible='icon'>
      <SidebarContent>
        {navigation.map((group, groupIndex) => (
          <SidebarGroup key={groupIndex}>
            {group.label && <SidebarGroupLabel>{group.label}</SidebarGroupLabel>}
            <SidebarGroupContent>
              <SidebarMenu>
                {group.items.map((item) =>
                  item.children ? (
                    <Collapsible
                      key={item.label}
                      asChild
                      defaultOpen={item.isActive}
                      className='group/collapsible'
                    >
                      <SidebarMenuItem>
                        <CollapsibleTrigger asChild>
                          <SidebarMenuButton tooltip={item.label}>
                            {<item.icon />}
                            <span>{item.label}</span>
                            <ChevronRight className='ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90' />
                          </SidebarMenuButton>
                        </CollapsibleTrigger>
                        <CollapsibleContent>
                          <SidebarMenuSub>
                            {item.children.map((sub) => (
                              <SidebarMenuSubItem key={sub.label}>
                                <SidebarMenuSubButton asChild>
                                  <Link to={sub.href}>
                                    <span>{sub.label}</span>
                                  </Link>
                                </SidebarMenuSubButton>
                              </SidebarMenuSubItem>
                            ))}
                          </SidebarMenuSub>
                        </CollapsibleContent>
                      </SidebarMenuItem>
                    </Collapsible>
                  ) : (
                    <SidebarMenuItem key={item.label}>
                      <SidebarMenuButton asChild tooltip={item.label}>
                        <Link to={item.href === '#' ? '/' : item.href}>
                          {<item.icon />}
                          <span>{item.label}</span>
                        </Link>
                      </SidebarMenuButton>
                      {item.badge !== undefined && (
                        <SidebarMenuBadge className='bg-primary/10 rounded-full'>
                          {item.badge}
                        </SidebarMenuBadge>
                      )}
                    </SidebarMenuItem>
                  ),
                )}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        ))}
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  )
}

export default AppSidebar
