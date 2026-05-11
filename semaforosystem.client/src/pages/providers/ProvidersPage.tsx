import { useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
} from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'

import {
  EditIcon,
  PlusIcon,
  SearchIcon,
  Trash2Icon,
  ChevronLeftIcon,
  ChevronRightIcon,
  XIcon,
  TruckIcon,
  ExternalLinkIcon,
} from 'lucide-react'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'

import type {
  ProviderResponse,
  ProviderQueryParams,
} from '@/services/provider-service'
import { getProviders, getProviderStatuses } from '@/services/provider-service'

import ProviderFormDialog from './provider-form-dialog'
import ProviderDeleteDialog from './provider-delete-dialog'

// ── Status badge helper ──────────────────────────────────

function StatusBadge({ code, name }: { code: string; name: string }) {
  const variant = (() => {
    switch (code?.toUpperCase()) {
      case 'ACTIVE':
        return 'default' as const
      case 'BLOCKED':
        return 'destructive' as const
      default:
        return 'secondary' as const
    }
  })()
  return <Badge variant={variant}>{name}</Badge>
}

// ── Columns ──────────────────────────────────────────────

function createColumns(
  onEdit: (p: ProviderResponse) => void,
  onDelete: (p: ProviderResponse) => void,
): ColumnDef<ProviderResponse>[] {
  return [
    {
      accessorKey: 'legalName',
      header: 'Razón social',
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <TruckIcon className='text-primary size-4' />
          </div>
          <div className='flex flex-col'>
            <span className='font-medium'>{row.original.legalName}</span>
            {row.original.tradeName && (
              <span className='text-muted-foreground text-xs'>
                {row.original.tradeName}
              </span>
            )}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'taxId',
      header: 'RFC / Tax ID',
      cell: ({ row }) => row.original.taxId || '—',
    },
    {
      accessorKey: 'providerStatusName',
      header: 'Estado',
      cell: ({ row }) => (
        <StatusBadge
          code={row.original.providerStatusCode}
          name={row.original.providerStatusName}
        />
      ),
    },
    {
      id: 'contactsCount',
      header: 'Contactos',
      cell: ({ row }) => (
        <span className='text-muted-foreground'>{row.original.contactsCount}</span>
      ),
    },
    {
      id: 'addressesCount',
      header: 'Direcciones',
      cell: ({ row }) => (
        <span className='text-muted-foreground'>{row.original.addressesCount}</span>
      ),
    },
    {
      id: 'bankAccountsCount',
      header: 'Cuentas',
      cell: ({ row }) => (
        <span className='text-muted-foreground'>{row.original.bankAccountsCount}</span>
      ),
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className='flex items-center justify-end gap-1'>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant='ghost'
                size='icon'
                className='size-8'
                asChild
              >
                <Link
                  to='/proveedores/$providerId'
                  params={{ providerId: String(row.original.providerId) }}
                >
                  <ExternalLinkIcon className='size-4' />
                </Link>
              </Button>
            </TooltipTrigger>
            <TooltipContent>Ver detalle</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant='ghost'
                size='icon'
                className='size-8'
                onClick={() => onEdit(row.original)}
              >
                <EditIcon className='size-4' />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Editar</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant='ghost'
                size='icon'
                className='text-destructive hover:text-destructive size-8'
                onClick={() => onDelete(row.original)}
              >
                <Trash2Icon className='size-4' />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Eliminar</TooltipContent>
          </Tooltip>
        </div>
      ),
    },
  ]
}

// ── Page ─────────────────────────────────────────────────

export default function ProvidersPage() {
  const [queryParams, setQueryParams] = useState<ProviderQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'legalName',
    sortDescending: false,
  })
  const [searchInput, setSearchInput] = useState('')

  const [formOpen, setFormOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [selected, setSelected] = useState<ProviderResponse | null>(null)

  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['providers', queryParams],
    queryFn: () => getProviders(queryParams),
    placeholderData: (prev) => prev,
  })

  const { data: statuses = [] } = useQuery({
    queryKey: ['provider-statuses'],
    queryFn: getProviderStatuses,
    staleTime: 10 * 60 * 1000,
  })

  const providers = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 0
  const currentPage = data?.page ?? 1

  // ── Handlers ─────────────────────────────────────────

  const handleSearch = useCallback(() => {
    setQueryParams((prev) => ({
      ...prev,
      search: searchInput.trim() || undefined,
      page: 1,
    }))
  }, [searchInput])

  const handleClearSearch = useCallback(() => {
    setSearchInput('')
    setQueryParams((prev) => ({ ...prev, search: undefined, page: 1 }))
  }, [])

  const handleStatusFilter = useCallback((value: string) => {
    setQueryParams((prev) => ({
      ...prev,
      providerStatusId: value === 'all' ? undefined : Number(value),
      page: 1,
    }))
  }, [])

  const handlePageChange = useCallback((page: number) => {
    setQueryParams((prev) => ({ ...prev, page }))
  }, [])

  const handlePageSizeChange = useCallback((value: string) => {
    setQueryParams((prev) => ({ ...prev, pageSize: Number(value), page: 1 }))
  }, [])

  const handleCreate = useCallback(() => {
    setSelected(null)
    setFormOpen(true)
  }, [])

  const handleEdit = useCallback((p: ProviderResponse) => {
    setSelected(p)
    setFormOpen(true)
  }, [])

  const handleDelete = useCallback((p: ProviderResponse) => {
    setSelected(p)
    setDeleteOpen(true)
  }, [])

  const columns = createColumns(handleEdit, handleDelete)

  const table = useReactTable({
    data: providers,
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    pageCount: totalPages,
  })

  return (
    <DashboardLayout>
      <div className='space-y-6'>
        {/* Header */}
        <div className='flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between'>
          <div>
            <h1 className='text-2xl font-bold tracking-tight'>Proveedores</h1>
            <p className='text-muted-foreground text-sm'>
              Gestiona el directorio de proveedores, contactos, direcciones y cuentas bancarias.
            </p>
          </div>
          <Button onClick={handleCreate} className='gap-2'>
            <PlusIcon className='size-4' />
            Nuevo Proveedor
          </Button>
        </div>

        {/* Filters */}
        <Card>
          <CardContent className='pt-6'>
            <div className='flex flex-col gap-3 sm:flex-row sm:items-center'>
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Buscar por razón social, nombre comercial, RFC o sitio web...'
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  className='pl-9 pr-9'
                />
                {searchInput && (
                  <button
                    type='button'
                    onClick={handleClearSearch}
                    className='text-muted-foreground hover:text-foreground absolute right-3 top-1/2 -translate-y-1/2'
                  >
                    <XIcon className='size-4' />
                  </button>
                )}
              </div>
              <Select
                value={
                  queryParams.providerStatusId
                    ? String(queryParams.providerStatusId)
                    : 'all'
                }
                onValueChange={handleStatusFilter}
              >
                <SelectTrigger className='w-full sm:w-[200px]'>
                  <SelectValue placeholder='Todos los estados' />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='all'>Todos los estados</SelectItem>
                  {statuses.map((s) => (
                    <SelectItem
                      key={s.providerStatusId}
                      value={String(s.providerStatusId)}
                    >
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Button variant='secondary' onClick={handleSearch}>
                Buscar
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Data table */}
        <Card>
          <CardHeader className='pb-3'>
            <div className='flex items-center justify-between'>
              <CardTitle className='text-base font-medium'>
                {isLoading ? (
                  <Skeleton className='h-5 w-40' />
                ) : (
                  <>
                    {totalCount}{' '}
                    {totalCount === 1
                      ? 'proveedor encontrado'
                      : 'proveedores encontrados'}
                  </>
                )}
              </CardTitle>
              {isFetching && !isLoading && (
                <span className='text-muted-foreground text-xs animate-pulse'>
                  Actualizando...
                </span>
              )}
            </div>
          </CardHeader>
          <CardContent>
            <div className='rounded-md border'>
              <Table>
                <TableHeader>
                  {table.getHeaderGroups().map((hg) => (
                    <TableRow key={hg.id}>
                      {hg.headers.map((h) => (
                        <TableHead key={h.id}>
                          {h.isPlaceholder
                            ? null
                            : flexRender(h.column.columnDef.header, h.getContext())}
                        </TableHead>
                      ))}
                    </TableRow>
                  ))}
                </TableHeader>
                <TableBody>
                  {isLoading ? (
                    Array.from({ length: 5 }).map((_, i) => (
                      <TableRow key={i}>
                        {columns.map((_, j) => (
                          <TableCell key={j}>
                            <Skeleton className='h-5 w-full' />
                          </TableCell>
                        ))}
                      </TableRow>
                    ))
                  ) : table.getRowModel().rows.length > 0 ? (
                    table.getRowModel().rows.map((row) => (
                      <TableRow key={row.id}>
                        {row.getVisibleCells().map((cell) => (
                          <TableCell key={cell.id}>
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                          </TableCell>
                        ))}
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell
                        colSpan={columns.length}
                        className='h-32 text-center'
                      >
                        <div className='flex flex-col items-center gap-2'>
                          <TruckIcon className='text-muted-foreground size-8' />
                          <p className='text-muted-foreground'>
                            No se encontraron proveedores.
                          </p>
                          <Button
                            variant='link'
                            size='sm'
                            onClick={handleCreate}
                          >
                            Crear el primer proveedor
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>

            {totalPages > 0 && (
              <div className='flex flex-col items-center justify-between gap-3 pt-4 sm:flex-row'>
                <div className='text-muted-foreground flex items-center gap-2 text-sm'>
                  <span>Filas por página</span>
                  <Select
                    value={String(queryParams.pageSize)}
                    onValueChange={handlePageSizeChange}
                  >
                    <SelectTrigger className='h-8 w-[70px]'>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {[5, 10, 20, 50].map((size) => (
                        <SelectItem key={size} value={String(size)}>
                          {size}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className='flex items-center gap-2'>
                  <span className='text-muted-foreground text-sm'>
                    Página {currentPage} de {totalPages}
                  </span>
                  <div className='flex gap-1'>
                    <Button
                      variant='outline'
                      size='icon'
                      className='size-8'
                      onClick={() => handlePageChange(currentPage - 1)}
                      disabled={currentPage <= 1}
                    >
                      <ChevronLeftIcon className='size-4' />
                    </Button>
                    <Button
                      variant='outline'
                      size='icon'
                      className='size-8'
                      onClick={() => handlePageChange(currentPage + 1)}
                      disabled={currentPage >= totalPages}
                    >
                      <ChevronRightIcon className='size-4' />
                    </Button>
                  </div>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      <ProviderFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        provider={selected}
      />
      <ProviderDeleteDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        provider={selected}
      />
    </DashboardLayout>
  )
}
