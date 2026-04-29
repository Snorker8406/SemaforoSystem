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
  ChevronLeftIcon,
  ChevronRightIcon,
  EyeIcon,
  ReceiptIcon,
  SearchIcon,
  XIcon,
} from 'lucide-react'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'

import type {
  SaleListItem,
  SaleQueryParams,
} from '@/services/sale-service'
import {
  getSales,
  getSaleStatuses,
  getSaleTypes,
} from '@/services/sale-service'

// ── Status color ──────────────────────────────────────────

function statusVariant(
  code: string | null,
): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (code?.toUpperCase()) {
    case 'COMPLETED':
      return 'default'
    case 'DRAFT':
      return 'secondary'
    case 'CANCELED':
    case 'VOID':
      return 'destructive'
    default:
      return 'outline'
  }
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('es-MX', {
    style: 'currency',
    currency: 'MXN',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('es-MX', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

// ── Columns ───────────────────────────────────────────────

function createColumns(): ColumnDef<SaleListItem>[] {
  return [
    {
      accessorKey: 'folio',
      header: 'Folio',
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <ReceiptIcon className='text-primary size-4' />
          </div>
          <div className='flex flex-col'>
            <span className='font-medium'>
              {row.original.folio ?? `#${row.original.saleId}`}
            </span>
            <span className='text-muted-foreground text-xs'>
              #{row.original.saleId}
            </span>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'saleDate',
      header: 'Fecha',
      cell: ({ row }) => formatDate(row.original.saleDate),
    },
    {
      accessorKey: 'clientName',
      header: 'Cliente',
      cell: ({ row }) => (
        <span className='font-medium'>{row.original.clientName ?? '—'}</span>
      ),
    },
    {
      accessorKey: 'employeeName',
      header: 'Vendedor',
      cell: ({ row }) => (
        <span className='text-muted-foreground'>
          {row.original.employeeName ?? '—'}
        </span>
      ),
    },
    {
      accessorKey: 'saleTypeName',
      header: 'Tipo',
      cell: ({ row }) => (
        <Badge variant='outline'>{row.original.saleTypeName ?? '—'}</Badge>
      ),
    },
    {
      accessorKey: 'saleStatusName',
      header: 'Estatus',
      cell: ({ row }) => (
        <Badge variant={statusVariant(row.original.saleStatusCode)}>
          {row.original.saleStatusName ?? '—'}
        </Badge>
      ),
    },
    {
      accessorKey: 'total',
      header: 'Total',
      cell: ({ row }) => (
        <span className='font-medium'>{formatCurrency(row.original.total)}</span>
      ),
    },
    {
      accessorKey: 'paidTotal',
      header: 'Pagado',
      cell: ({ row }) => formatCurrency(row.original.paidTotal),
    },
    {
      accessorKey: 'balance',
      header: 'Saldo',
      cell: ({ row }) => {
        const b = row.original.balance
        return (
          <span
            className={
              b > 0
                ? 'text-destructive font-medium'
                : 'font-medium text-green-600'
            }
          >
            {formatCurrency(b)}
          </span>
        )
      },
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className='flex items-center justify-end gap-1'>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button variant='ghost' size='icon' asChild>
                <Link to={`/sales/${row.original.saleId}`}>
                  <EyeIcon className='size-4' />
                </Link>
              </Button>
            </TooltipTrigger>
            <TooltipContent>Ver detalle</TooltipContent>
          </Tooltip>
        </div>
      ),
    },
  ]
}

// ── Page ──────────────────────────────────────────────────

const PAGE_SIZES = [10, 20, 50]

const SalesPage = () => {
  const [params, setParams] = useState<SaleQueryParams>({
    page: 1,
    pageSize: 20,
    sortBy: 'saleId',
    sortDescending: true,
  })
  const [search, setSearch] = useState('')

  const { data, isLoading } = useQuery({
    queryKey: ['sales', params],
    queryFn: () => getSales(params),
  })

  const { data: types } = useQuery({
    queryKey: ['sale-types'],
    queryFn: getSaleTypes,
  })

  const { data: statuses } = useQuery({
    queryKey: ['sale-statuses'],
    queryFn: getSaleStatuses,
  })

  const columns = createColumns()

  const table = useReactTable({
    data: data?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    pageCount: data?.totalPages ?? 0,
  })

  const applySearch = useCallback(() => {
    setParams((p) => ({ ...p, page: 1, search: search.trim() || undefined }))
  }, [search])

  const clearFilters = useCallback(() => {
    setSearch('')
    setParams({
      page: 1,
      pageSize: 20,
      sortBy: 'saleId',
      sortDescending: true,
    })
  }, [])

  const hasFilters =
    !!params.search ||
    !!params.saleTypeId ||
    !!params.statusCode ||
    !!params.from ||
    !!params.to

  return (
    <DashboardLayout>
      <div className='flex flex-col gap-6 p-6'>
        {/* Header */}
        <div className='flex items-center justify-between'>
          <div>
            <h1 className='text-2xl font-bold tracking-tight'>Ventas</h1>
            <p className='text-muted-foreground text-sm'>
              Consulta el histórico de ventas y sus detalles
            </p>
          </div>
        </div>

        {/* Filters */}
        <Card>
          <CardContent className='pt-4'>
            <div className='flex flex-wrap gap-3'>
              <div className='flex min-w-50 flex-1 gap-2'>
                <Input
                  placeholder='Buscar folio, cliente o referencia...'
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && applySearch()}
                />
                <Button variant='secondary' size='icon' onClick={applySearch}>
                  <SearchIcon className='size-4' />
                </Button>
              </div>

              <Select
                value={params.saleTypeId ? String(params.saleTypeId) : 'all'}
                onValueChange={(v) =>
                  setParams((p) => ({
                    ...p,
                    page: 1,
                    saleTypeId: v === 'all' ? undefined : Number(v),
                  }))
                }
              >
                <SelectTrigger className='w-40'>
                  <SelectValue placeholder='Tipo' />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='all'>Todos los tipos</SelectItem>
                  {types?.map((t) => (
                    <SelectItem key={t.id} value={String(t.id)}>
                      {t.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Select
                value={params.statusCode ?? 'all'}
                onValueChange={(v) =>
                  setParams((p) => ({
                    ...p,
                    page: 1,
                    statusCode: v === 'all' ? undefined : v,
                  }))
                }
              >
                <SelectTrigger className='w-40'>
                  <SelectValue placeholder='Estatus' />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='all'>Todos los estatus</SelectItem>
                  {statuses?.map((s) => (
                    <SelectItem key={s.id} value={s.code}>
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              <Input
                type='date'
                value={params.from ?? ''}
                onChange={(e) =>
                  setParams((p) => ({
                    ...p,
                    page: 1,
                    from: e.target.value || undefined,
                  }))
                }
                className='w-40'
              />
              <Input
                type='date'
                value={params.to ?? ''}
                onChange={(e) =>
                  setParams((p) => ({
                    ...p,
                    page: 1,
                    to: e.target.value || undefined,
                  }))
                }
                className='w-40'
              />

              {hasFilters && (
                <Button variant='ghost' size='icon' onClick={clearFilters}>
                  <XIcon className='size-4' />
                </Button>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Table */}
        <Card>
          <CardHeader>
            <CardTitle className='text-base'>
              {isLoading
                ? 'Cargando...'
                : `${data?.totalCount ?? 0} venta(s)`}
            </CardTitle>
          </CardHeader>
          <CardContent className='p-0'>
            <Table>
              <TableHeader>
                {table.getHeaderGroups().map((hg) => (
                  <TableRow key={hg.id}>
                    {hg.headers.map((h) => (
                      <TableHead key={h.id}>
                        {flexRender(
                          h.column.columnDef.header,
                          h.getContext(),
                        )}
                      </TableHead>
                    ))}
                  </TableRow>
                ))}
              </TableHeader>
              <TableBody>
                {isLoading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <TableRow key={i}>
                      {columns.map((_, ci) => (
                        <TableCell key={ci}>
                          <Skeleton className='h-5 w-full' />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                ) : table.getRowModel().rows.length === 0 ? (
                  <TableRow>
                    <TableCell
                      colSpan={columns.length}
                      className='text-muted-foreground py-10 text-center'
                    >
                      No se encontraron ventas.
                    </TableCell>
                  </TableRow>
                ) : (
                  table.getRowModel().rows.map((row) => (
                    <TableRow key={row.id} className='hover:bg-muted/50'>
                      {row.getVisibleCells().map((cell) => (
                        <TableCell key={cell.id}>
                          {flexRender(
                            cell.column.columnDef.cell,
                            cell.getContext(),
                          )}
                        </TableCell>
                      ))}
                    </TableRow>
                  ))
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className='flex items-center justify-between'>
            <div className='flex items-center gap-2'>
              <span className='text-muted-foreground text-sm'>Filas:</span>
              <Select
                value={String(params.pageSize)}
                onValueChange={(v) =>
                  setParams((p) => ({ ...p, page: 1, pageSize: Number(v) }))
                }
              >
                <SelectTrigger className='w-18 h-8'>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {PAGE_SIZES.map((s) => (
                    <SelectItem key={s} value={String(s)}>
                      {s}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className='flex items-center gap-2'>
              <span className='text-muted-foreground text-sm'>
                Pág. {data.page} de {data.totalPages}
              </span>
              <Button
                variant='outline'
                size='icon'
                disabled={!data.hasPreviousPage}
                onClick={() =>
                  setParams((p) => ({ ...p, page: (p.page ?? 1) - 1 }))
                }
              >
                <ChevronLeftIcon className='size-4' />
              </Button>
              <Button
                variant='outline'
                size='icon'
                disabled={!data.hasNextPage}
                onClick={() =>
                  setParams((p) => ({ ...p, page: (p.page ?? 1) + 1 }))
                }
              >
                <ChevronRightIcon className='size-4' />
              </Button>
            </div>
          </div>
        )}
      </div>
    </DashboardLayout>
  )
}

export default SalesPage
