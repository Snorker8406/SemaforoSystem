import { useState, useCallback, useMemo } from 'react'
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
  LayersIcon,
  ListIcon,
  EyeIcon,
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
import { DataTableColumnHeader } from '@/components/data-table-column-header'
import type { ColumnFilterType } from '@/components/data-table-column-header'

import type {
  ProductComboResponse,
  ProductComboQueryParams,
} from '@/services/combo-service'

import { useCombos } from '@/hooks/use-combos'

import ComboFormDialog from './combo-form-dialog'
import ComboDeleteDialog from './combo-delete-dialog'
import ComboDetailDialog from './combo-detail-dialog'

// ── Column sort-key map ──────────────────────────────────

const SORT_MAP: Record<string, string> = {
  name: 'name',
  createDate: 'createdate',
  detailCount: 'detailcount',
  active: 'active',
}

// ── Columns ──────────────────────────────────────────────

interface ColumnContext {
  onEdit: (combo: ProductComboResponse) => void
  onDelete: (combo: ProductComboResponse) => void
  onViewDetails: (combo: ProductComboResponse) => void
  queryParams: ProductComboQueryParams
  onSort: (sortBy: string | undefined, sortDesc: boolean) => void
  onFilter: (key: string, value: string) => void
}

function createColumns(ctx: ColumnContext): ColumnDef<ProductComboResponse>[] {
  const { onEdit, onDelete, onViewDetails, queryParams, onSort } = ctx

  function headerProps(columnKey: string) {
    return {
      sortKey: SORT_MAP[columnKey],
      currentSortBy: queryParams.sortBy,
      currentSortDesc: queryParams.sortDescending,
      onSort,
    }
  }

  return [
    {
      accessorKey: 'name',
      header: () => (
        <DataTableColumnHeader
          title='Nombre'
          {...headerProps('name')}
          filter={
            {
              kind: 'text',
              value: queryParams.search ?? '',
              onChange: (v: string) => ctx.onFilter('search', v),
              placeholder: 'Buscar nombre...',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <LayersIcon className='text-primary size-4' />
          </div>
          <div className='flex flex-col'>
            <span className='font-medium'>{row.original.name}</span>
            {row.original.description && (
              <span className='text-muted-foreground text-xs'>
                {row.original.description}
              </span>
            )}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'detailCount',
      header: () => (
        <DataTableColumnHeader
          title='Productos'
          {...headerProps('detailCount')}
        />
      ),
      cell: ({ row }) => (
        <div className='flex items-center gap-1'>
          <ListIcon className='text-muted-foreground size-3.5' />
          <Badge variant='secondary'>{row.original.detailCount}</Badge>
        </div>
      ),
    },
    {
      accessorKey: 'priceCount',
      header: () => <DataTableColumnHeader title='Precios' />,
      cell: ({ row }) => (
        <Badge variant='outline'>{row.original.priceCount}</Badge>
      ),
    },
    {
      accessorKey: 'schoolCount',
      header: () => <DataTableColumnHeader title='Escuelas' />,
      cell: ({ row }) => (
        <Badge variant='outline'>{row.original.schoolCount}</Badge>
      ),
    },
    {
      accessorKey: 'active',
      header: () => <DataTableColumnHeader title='Estado' />,
      cell: ({ row }) => {
        const active = row.original.active
        return active ? (
          <Badge variant='default'>Activo</Badge>
        ) : (
          <Badge variant='secondary'>Inactivo</Badge>
        )
      },
    },
    {
      accessorKey: 'createDate',
      header: () => (
        <DataTableColumnHeader
          title='Fecha Creación'
          {...headerProps('createDate')}
        />
      ),
      cell: ({ row }) =>
        row.original.createDate ? (
          <span className='text-muted-foreground text-sm'>
            {new Date(row.original.createDate).toLocaleDateString('es-MX')}
          </span>
        ) : (
          <span className='text-muted-foreground'>—</span>
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
                onClick={() => onViewDetails(row.original)}
              >
                <EyeIcon className='size-4' />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Ver detalles</TooltipContent>
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

// ── Page Component ───────────────────────────────────────

export default function CombosPage() {
  // ── Query state ────────────────────────────────────
  const [queryParams, setQueryParams] = useState<ProductComboQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDescending: false,
  })
  const [searchInput, setSearchInput] = useState('')

  // ── Dialog state ───────────────────────────────────
  const [formOpen, setFormOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [detailOpen, setDetailOpen] = useState(false)
  const [selectedCombo, setSelectedCombo] =
    useState<ProductComboResponse | null>(null)

  // ── Data fetching ──────────────────────────────────
  const { data, isLoading, isFetching } = useCombos(queryParams)

  const combos = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 0
  const currentPage = data?.page ?? 1

  // ── Handlers ───────────────────────────────────────

  const handleSort = useCallback(
    (sortBy: string | undefined, sortDesc: boolean) => {
      setQueryParams((prev) => ({
        ...prev,
        sortBy: sortBy ?? 'name',
        sortDescending: sortBy ? sortDesc : false,
        page: 1,
      }))
    },
    [],
  )

  const handleColumnFilter = useCallback((key: string, value: string) => {
    setQueryParams((prev) => {
      const next: ProductComboQueryParams = { ...prev, page: 1 }
      switch (key) {
        case 'search':
          next.search = value.trim() || undefined
          break
      }
      return next
    })
  }, [])

  const handleSearch = useCallback(() => {
    setQueryParams((prev) => ({
      ...prev,
      search: searchInput.trim() || undefined,
      page: 1,
    }))
  }, [searchInput])

  const handleClearSearch = useCallback(() => {
    setSearchInput('')
    setQueryParams((prev) => ({
      ...prev,
      search: undefined,
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
    setSelectedCombo(null)
    setFormOpen(true)
  }, [])

  const handleEdit = useCallback((combo: ProductComboResponse) => {
    setSelectedCombo(combo)
    setFormOpen(true)
  }, [])

  const handleDelete = useCallback((combo: ProductComboResponse) => {
    setSelectedCombo(combo)
    setDeleteOpen(true)
  }, [])

  const handleViewDetails = useCallback((combo: ProductComboResponse) => {
    setSelectedCombo(combo)
    setDetailOpen(true)
  }, [])

  const handleClearAllFilters = useCallback(() => {
    setSearchInput('')
    setQueryParams({
      page: 1,
      pageSize: queryParams.pageSize,
      sortBy: 'name',
      sortDescending: false,
    })
  }, [queryParams.pageSize])

  const activeFilterCount = useMemo(() => {
    let count = 0
    if (queryParams.search) count++
    return count
  }, [queryParams])

  // ── Table ──────────────────────────────────────────

  const columns = useMemo(
    () =>
      createColumns({
        onEdit: handleEdit,
        onDelete: handleDelete,
        onViewDetails: handleViewDetails,
        queryParams,
        onSort: handleSort,
        onFilter: handleColumnFilter,
      }),
    [handleEdit, handleDelete, handleViewDetails, queryParams, handleSort, handleColumnFilter],
  )

  const table = useReactTable({
    data: combos,
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    pageCount: totalPages,
  })

  // ── Render ─────────────────────────────────────────

  return (
    <DashboardLayout>
      <div className='space-y-6'>
        {/* Header */}
        <div className='flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between'>
          <div>
            <h1 className='text-2xl font-bold tracking-tight'>Combos de Producto</h1>
            <p className='text-muted-foreground text-sm'>
              Gestiona los combos (paquetes) de productos y bordados.
            </p>
          </div>
          <Button onClick={handleCreate} className='gap-2'>
            <PlusIcon className='size-4' />
            Nuevo Combo
          </Button>
        </div>

        {/* Search */}
        <Card>
          <CardContent className='pt-6'>
            <div className='flex flex-col gap-3 sm:flex-row sm:items-center'>
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Buscar por nombre o descripción...'
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
              <Button variant='secondary' onClick={handleSearch}>
                Buscar
              </Button>
              {activeFilterCount > 0 && (
                <Button
                  variant='ghost'
                  size='sm'
                  className='gap-1.5'
                  onClick={handleClearAllFilters}
                >
                  <XIcon className='size-3.5' />
                  Limpiar filtros
                  <Badge variant='secondary' className='ml-1 px-1.5 text-xs'>
                    {activeFilterCount}
                  </Badge>
                </Button>
              )}
            </div>
          </CardContent>
        </Card>

        {/* Data Table */}
        <Card>
          <CardHeader className='pb-3'>
            <div className='flex items-center justify-between'>
              <CardTitle className='text-base font-medium'>
                {isLoading ? (
                  <Skeleton className='h-5 w-40' />
                ) : (
                  <>
                    {totalCount}{' '}
                    {totalCount === 1 ? 'combo encontrado' : 'combos encontrados'}
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
                  {table.getHeaderGroups().map((headerGroup) => (
                    <TableRow key={headerGroup.id}>
                      {headerGroup.headers.map((header) => (
                        <TableHead key={header.id}>
                          {header.isPlaceholder
                            ? null
                            : flexRender(
                                header.column.columnDef.header,
                                header.getContext(),
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
                            {flexRender(
                              cell.column.columnDef.cell,
                              cell.getContext(),
                            )}
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
                          <LayersIcon className='text-muted-foreground size-8' />
                          <p className='text-muted-foreground'>
                            No se encontraron combos.
                          </p>
                          <Button
                            variant='link'
                            size='sm'
                            onClick={handleCreate}
                          >
                            Crear el primer combo
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>

            {/* Pagination */}
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

      {/* Dialogs */}
      <ComboFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        combo={selectedCombo}
      />
      <ComboDeleteDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        combo={selectedCombo}
      />
      <ComboDetailDialog
        open={detailOpen}
        onOpenChange={setDetailOpen}
        combo={selectedCombo}
      />
    </DashboardLayout>
  )
}
