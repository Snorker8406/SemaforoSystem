import { useState, useCallback, Fragment } from 'react'
import {
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import type { ColumnDef, Row } from '@tanstack/react-table'

import {
  ChevronDownIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  EditIcon,
  LayersIcon,
  PackageIcon,
  PlusIcon,
  SearchIcon,
  SwatchBookIcon,
  Trash2Icon,
  XIcon,
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
  VariantSystemResponse,
  VariantResponse,
  VariantSystemQueryParams,
} from '@/services/variant-service'

import { useVariantSystems } from '@/hooks/use-variants'

import VariantSystemFormDialog from './variant-system-form-dialog'
import VariantSystemDeleteDialog from './variant-system-delete-dialog'
import VariantFormDialog from './variant-form-dialog'
import VariantDeleteDialog from './variant-delete-dialog'

// ── Columns ──────────────────────────────────────────────

interface ColumnContext {
  onEditSystem: (vs: VariantSystemResponse) => void
  onDeleteSystem: (vs: VariantSystemResponse) => void
  onAddVariant: (vs: VariantSystemResponse) => void
}

function createColumns(ctx: ColumnContext): ColumnDef<VariantSystemResponse>[] {
  const { onEditSystem, onDeleteSystem, onAddVariant } = ctx

  return [
    {
      id: 'expander',
      header: '',
      cell: ({ row }) => (
        <Button
          variant='ghost'
          size='icon'
          className='size-8'
          onClick={() => row.toggleExpanded()}
        >
          <ChevronDownIcon
            className={`size-4 transition-transform duration-200 ${
              row.getIsExpanded() ? 'rotate-180' : ''
            }`}
          />
        </Button>
      ),
      size: 40,
    },
    {
      accessorKey: 'name',
      header: 'Nombre',
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <SwatchBookIcon className='text-primary size-4' />
          </div>
          <button
            type='button'
            className='font-medium hover:underline text-left'
            onClick={() => row.toggleExpanded()}
          >
            {row.original.name}
          </button>
        </div>
      ),
    },
    {
      accessorKey: 'description',
      header: 'Descripción',
      cell: ({ row }) => (
        <span className='text-muted-foreground truncate block max-w-75'>
          {row.original.description || '—'}
        </span>
      ),
    },
    {
      accessorKey: 'variantCount',
      header: 'Variantes',
      cell: ({ row }) => (
        <Badge variant='secondary' className='gap-1'>
          <LayersIcon className='size-3' />
          {row.original.variantCount}
        </Badge>
      ),
    },
    {
      accessorKey: 'productCount',
      header: 'Productos',
      cell: ({ row }) => (
        <Badge variant='secondary' className='gap-1'>
          <PackageIcon className='size-3' />
          {row.original.productCount}
        </Badge>
      ),
    },
    {
      id: 'variants-preview',
      header: 'Vista previa',
      cell: ({ row }) => {
        const variants = row.original.variants
        if (variants.length === 0) {
          return <span className='text-muted-foreground text-xs'>Sin variantes</span>
        }
        const shown = variants.slice(0, 8)
        const remaining = variants.length - shown.length
        return (
          <div className='flex flex-wrap gap-1'>
            {shown.map((v) => (
              <Badge key={v.productVariantId} variant='outline' className='text-xs'>
                {v.variantValue}
              </Badge>
            ))}
            {remaining > 0 && (
              <Badge variant='outline' className='text-xs text-muted-foreground'>
                +{remaining}
              </Badge>
            )}
          </div>
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
              <Button
                variant='ghost'
                size='icon'
                className='size-8'
                onClick={() => onAddVariant(row.original)}
              >
                <PlusIcon className='size-4' />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Agregar variante</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant='ghost'
                size='icon'
                className='size-8'
                onClick={() => onEditSystem(row.original)}
              >
                <EditIcon className='size-4' />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Editar sistema</TooltipContent>
          </Tooltip>
          <Tooltip>
            <TooltipTrigger asChild>
              <Button
                variant='ghost'
                size='icon'
                className='text-destructive hover:text-destructive size-8'
                onClick={() => onDeleteSystem(row.original)}
              >
                <Trash2Icon className='size-4' />
              </Button>
            </TooltipTrigger>
            <TooltipContent>Eliminar sistema</TooltipContent>
          </Tooltip>
        </div>
      ),
    },
  ]
}

// ── Expanded Row: Variants sub-table ─────────────────────

interface VariantSubTableProps {
  variantSystem: VariantSystemResponse
  onEditVariant: (variant: VariantResponse) => void
  onDeleteVariant: (variant: VariantResponse) => void
}

function VariantSubTable({ variantSystem, onEditVariant, onDeleteVariant }: VariantSubTableProps) {
  const variants = variantSystem.variants

  if (variants.length === 0) {
    return (
      <div className='flex items-center justify-center py-6 text-muted-foreground text-sm'>
        <SwatchBookIcon className='mr-2 size-4' />
        Este sistema no tiene variantes definidas.
      </div>
    )
  }

  return (
    <div className='rounded-md border'>
      <Table>
        <TableHeader>
          <TableRow className='bg-muted/30 h-8'>
            <TableHead className='w-40 py-1 text-xs'>Valor</TableHead>
            <TableHead className='py-1 text-xs'>Descripción</TableHead>
            <TableHead className='w-25 py-1 text-xs text-right'>Acciones</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {variants.map((variant) => (
            <TableRow key={variant.productVariantId} className='h-8'>
              <TableCell className='py-1'>
                <Badge variant='outline' className='font-mono text-xs py-0'>
                  {variant.variantValue}
                </Badge>
              </TableCell>
              <TableCell className='py-1'>
                <span className='text-muted-foreground text-xs'>
                  {variant.description || '—'}
                </span>
              </TableCell>
              <TableCell className='py-1'>
                <div className='flex items-center justify-end gap-1'>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant='ghost'
                        size='icon'
                        className='size-7'
                        onClick={() => onEditVariant(variant)}
                      >
                        <EditIcon className='size-3.5' />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Editar variante</TooltipContent>
                  </Tooltip>
                  <Tooltip>
                    <TooltipTrigger asChild>
                      <Button
                        variant='ghost'
                        size='icon'
                        className='text-destructive hover:text-destructive size-7'
                        onClick={() => onDeleteVariant(variant)}
                      >
                        <Trash2Icon className='size-3.5' />
                      </Button>
                    </TooltipTrigger>
                    <TooltipContent>Eliminar variante</TooltipContent>
                  </Tooltip>
                </div>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  )
}

// ── Page Component ───────────────────────────────────────

export default function VariantSystemsPage() {
  // ── Query state ────────────────────────────────────
  const [queryParams, setQueryParams] = useState<VariantSystemQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDescending: false,
  })
  const [searchInput, setSearchInput] = useState('')

  // ── Dialog state — Variant Systems ─────────────────
  const [systemFormOpen, setSystemFormOpen] = useState(false)
  const [systemDeleteOpen, setSystemDeleteOpen] = useState(false)
  const [selectedSystem, setSelectedSystem] = useState<VariantSystemResponse | null>(null)

  // ── Dialog state — Variants ────────────────────────
  const [variantFormOpen, setVariantFormOpen] = useState(false)
  const [variantDeleteOpen, setVariantDeleteOpen] = useState(false)
  const [selectedVariant, setSelectedVariant] = useState<VariantResponse | null>(null)
  const [activeVariantSystemId, setActiveVariantSystemId] = useState<number | null>(null)
  const [activeVariantSystemName, setActiveVariantSystemName] = useState<string>('')

  // ── Data fetching ──────────────────────────────────
  const { data, isLoading, isFetching } = useVariantSystems(queryParams)

  const systems = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 0
  const currentPage = data?.page ?? 1

  // ── Handlers — Search ─────────────────────────────

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

  // ── Handlers — Variant System ─────────────────────

  const handleCreateSystem = useCallback(() => {
    setSelectedSystem(null)
    setSystemFormOpen(true)
  }, [])

  const handleEditSystem = useCallback((vs: VariantSystemResponse) => {
    setSelectedSystem(vs)
    setSystemFormOpen(true)
  }, [])

  const handleDeleteSystem = useCallback((vs: VariantSystemResponse) => {
    setSelectedSystem(vs)
    setSystemDeleteOpen(true)
  }, [])

  // ── Handlers — Variants ───────────────────────────

  const handleAddVariant = useCallback((vs: VariantSystemResponse) => {
    setSelectedVariant(null)
    setActiveVariantSystemId(vs.productVariantId)
    setActiveVariantSystemName(vs.name)
    setVariantFormOpen(true)
  }, [])

  const handleEditVariant = useCallback((variant: VariantResponse) => {
    setSelectedVariant(variant)
    setActiveVariantSystemId(variant.productVariantSystemId)
    setActiveVariantSystemName(variant.productVariantSystemName ?? '')
    setVariantFormOpen(true)
  }, [])

  const handleDeleteVariant = useCallback((variant: VariantResponse) => {
    setSelectedVariant(variant)
    setVariantDeleteOpen(true)
  }, [])

  // ── Table ──────────────────────────────────────────

  const columns = createColumns({
    onEditSystem: handleEditSystem,
    onDeleteSystem: handleDeleteSystem,
    onAddVariant: handleAddVariant,
  })

  const table = useReactTable({
    data: systems,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    getRowCanExpand: () => true,
    manualPagination: true,
    pageCount: totalPages,
  })

  // ── Render expanded row ────────────────────────────

  function renderExpandedRow(row: Row<VariantSystemResponse>) {
    return (
      <TableRow key={`${row.id}-expanded`}>
        <TableCell colSpan={columns.length} className='bg-muted/20 p-4'>
          <VariantSubTable
            variantSystem={row.original}
            onEditVariant={handleEditVariant}
            onDeleteVariant={handleDeleteVariant}
          />
        </TableCell>
      </TableRow>
    )
  }

  // ── Render ─────────────────────────────────────────

  return (
    <DashboardLayout>
      <div className='space-y-6'>
        {/* Header */}
        <div className='flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between'>
          <div>
            <h1 className='text-2xl font-bold tracking-tight'>
              Administrar Variantes
            </h1>
            <p className='text-muted-foreground text-sm'>
              Gestiona los sistemas de variantes y sus variantes asociadas.
            </p>
          </div>
          <Button onClick={handleCreateSystem} className='gap-2'>
            <PlusIcon className='size-4' />
            Nuevo Sistema
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
            </div>
          </CardContent>
        </Card>

        {/* Data Table */}
        <Card>
          <CardHeader className='pb-3'>
            <div className='flex items-center justify-between'>
              <CardTitle className='text-base font-medium'>
                {isLoading ? (
                  <Skeleton className='h-5 w-52' />
                ) : (
                  <>
                    {totalCount}{' '}
                    {totalCount === 1
                      ? 'sistema de variantes encontrado'
                      : 'sistemas de variantes encontrados'}
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
                      <Fragment key={row.id}>
                        <TableRow>
                          {row.getVisibleCells().map((cell) => (
                            <TableCell key={cell.id}>
                              {flexRender(
                                cell.column.columnDef.cell,
                                cell.getContext(),
                              )}
                            </TableCell>
                          ))}
                        </TableRow>
                        {row.getIsExpanded() && renderExpandedRow(row)}
                      </Fragment>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell
                        colSpan={columns.length}
                        className='h-32 text-center'
                      >
                        <div className='flex flex-col items-center gap-2'>
                          <SwatchBookIcon className='text-muted-foreground size-8' />
                          <p className='text-muted-foreground'>
                            No se encontraron sistemas de variantes.
                          </p>
                          <Button
                            variant='link'
                            size='sm'
                            onClick={handleCreateSystem}
                          >
                            Crear el primer sistema
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
                    <SelectTrigger className='h-8 w-17.5'>
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

      {/* Variant System Dialogs */}
      <VariantSystemFormDialog
        open={systemFormOpen}
        onOpenChange={setSystemFormOpen}
        variantSystem={selectedSystem}
      />
      <VariantSystemDeleteDialog
        open={systemDeleteOpen}
        onOpenChange={setSystemDeleteOpen}
        variantSystem={selectedSystem}
      />

      {/* Variant Dialogs */}
      <VariantFormDialog
        open={variantFormOpen}
        onOpenChange={setVariantFormOpen}
        variant={selectedVariant}
        variantSystemId={activeVariantSystemId}
        variantSystemName={activeVariantSystemName}
      />
      <VariantDeleteDialog
        open={variantDeleteOpen}
        onOpenChange={setVariantDeleteOpen}
        variant={selectedVariant}
      />
    </DashboardLayout>
  )
}
