import { useState, useCallback, useMemo, createContext, useContext } from 'react'
import {
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import type { ColumnDef, Row } from '@tanstack/react-table'

import {
  EditIcon,
  PlusIcon,
  SearchIcon,
  Trash2Icon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  XIcon,
  LayersIcon,
  PackageIcon,
  PaletteIcon,
  BoxIcon,
  GraduationCapIcon,
  Loader2Icon,
  ImageOffIcon,
  DollarSignIcon,
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
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import {
  Dialog,
  DialogContent,
  DialogTitle,
} from '@/components/ui/dialog'
import { VisuallyHidden } from '@radix-ui/react-visually-hidden'
import { DataTableColumnHeader } from '@/components/data-table-column-header'
import type { ColumnFilterType } from '@/components/data-table-column-header'

import type {
  ProductComboResponse,
  ProductComboQueryParams,
  VisualDefinitionResponse,
  ComponentResponse,
} from '@/services/combo-service'
import { getComboImageUrl } from '@/services/combo-service'
import { getVisualDefinitionImageUrl } from '@/services/product-service'
import { getEmbroideryImageUrl } from '@/services/embroidery-service'

import { useCombos, useComboVisualDefinitions } from '@/hooks/use-combos'

import ComboFormDialog from './combo-form-dialog'
import ComboDeleteDialog from './combo-delete-dialog'

// ── Image preview context ────────────────────────────────

const ImagePreviewContext = createContext<(url: string, alt: string) => void>(() => {})

// ── Column sort-key map ──────────────────────────────────

const SORT_MAP: Record<string, string> = {
  name: 'name',
  createdAt: 'createdat',
  isActive: 'isactive',
  visualDefinitionCount: 'visualdefinitioncount',
}

// ── Columns ──────────────────────────────────────────────

interface ColumnContext {
  onEdit: (combo: ProductComboResponse) => void
  onDelete: (combo: ProductComboResponse) => void
  queryParams: ProductComboQueryParams
  onSort: (sortBy: string | undefined, sortDesc: boolean) => void
  onFilter: (key: string, value: string) => void
}

function createColumns(ctx: ColumnContext): ColumnDef<ProductComboResponse>[] {
  const { onEdit, onDelete, queryParams, onSort } = ctx

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
      id: 'expand',
      header: '',
      cell: ({ row }) => (
        <Button
          variant='ghost'
          size='icon'
          className='size-7'
          onClick={() => row.toggleExpanded()}
        >
          <ChevronDownIcon
            className={`size-4 transition-transform ${row.getIsExpanded() ? 'rotate-180' : ''}`}
          />
        </Button>
      ),
      size: 36,
    },
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
      accessorKey: 'visualDefinitionCount',
      header: () => (
        <DataTableColumnHeader
          title='Variantes'
          {...headerProps('visualDefinitionCount')}
        />
      ),
      cell: ({ row }) => (
        <div className='flex items-center gap-1'>
          <BoxIcon className='text-muted-foreground size-3.5' />
          <Badge variant='secondary'>{row.original.visualDefinitionCount}</Badge>
        </div>
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
      accessorKey: 'isActive',
      header: () => (
        <DataTableColumnHeader
          title='Estado'
          filter={
            {
              kind: 'boolean',
              value: queryParams.isActive != null ? String(queryParams.isActive) : 'all',
              onChange: (v: string) => ctx.onFilter('isActive', v),
              trueLabel: 'Activos',
              falseLabel: 'Inactivos',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) => {
        const active = row.original.isActive
        return active ? (
          <Badge variant='default'>Activo</Badge>
        ) : (
          <Badge variant='secondary'>Inactivo</Badge>
        )
      },
    },
    {
      accessorKey: 'createdAt',
      header: () => (
        <DataTableColumnHeader
          title='Fecha Creación'
          {...headerProps('createdAt')}
        />
      ),
      cell: ({ row }) =>
        row.original.createdAt ? (
          <span className='text-muted-foreground text-sm'>
            {new Date(row.original.createdAt).toLocaleDateString('es-MX')}
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

// ── Expanded Row Component ───────────────────────────────

function ExpandedComboRow({ row }: { row: Row<ProductComboResponse> }) {
  const combo = row.original
  const { data: vds, isLoading } = useComboVisualDefinitions(combo.productComboId)

  return (
    <div className='space-y-4 px-6 py-4'>
      {/* Combo description */}
      {combo.description && (
        <p className='text-sm'>
          <span className='text-muted-foreground font-medium'>Descripción: </span>
          {combo.description}
        </p>
      )}

      {/* Visual Definitions */}
      <div className='space-y-2'>
        <h4 className='text-muted-foreground flex items-center gap-1.5 text-xs font-semibold uppercase tracking-wide'>
          <BoxIcon className='size-3.5' />
          Variantes / Versiones ({vds?.length ?? 0})
        </h4>

        {isLoading ? (
          <div className='flex items-center gap-2 py-4'>
            <Loader2Icon className='text-muted-foreground size-4 animate-spin' />
            <span className='text-muted-foreground text-sm'>Cargando variantes…</span>
          </div>
        ) : vds && vds.length > 0 ? (
          <div className='grid gap-3 sm:grid-cols-2 lg:grid-cols-3'>
            {vds.map((vd) => (
              <VisualDefinitionCard key={vd.productComboVisualDefinitionId} vd={vd} />
            ))}
          </div>
        ) : (
          <p className='text-muted-foreground py-2 text-sm'>
            Este combo no tiene variantes definidas.
          </p>
        )}
      </div>
    </div>
  )
}

// ── Visual Definition Card ───────────────────────────────

function ComboImageThumb({
  vd,
}: {
  vd: VisualDefinitionResponse
}) {
  const [error, setError] = useState(false)
  const onPreview = useContext(ImagePreviewContext)

  if (!vd.hasImage || !vd.imageId || error) {
    return (
      <div className='flex size-14 items-center justify-center rounded-md border bg-muted'>
        <ImageOffIcon className='text-muted-foreground size-5' />
      </div>
    )
  }

  const url = getComboImageUrl(vd.imageId)

  return (
    <img
      src={url}
      alt={vd.name}
      onError={() => setError(true)}
      onClick={() => onPreview(url, vd.name)}
      className='size-14 cursor-pointer rounded-md border object-cover transition-opacity hover:opacity-80'
    />
  )
}

// ── Component Row ────────────────────────────────────────

function ComponentImageThumb({
  src,
  alt,
}: {
  src: string
  alt: string
}) {
  const onPreview = useContext(ImagePreviewContext)

  return (
    <div className='shrink-0'>
      <img
        src={src}
        alt={alt}
        className='size-10 cursor-pointer rounded-md border object-cover transition-opacity hover:opacity-80'
        onClick={() => onPreview(src, alt)}
        onError={(e) => {
          e.currentTarget.style.display = 'none'
          const fallback = e.currentTarget.nextElementSibling as HTMLElement | null
          if (fallback) fallback.style.display = 'flex'
        }}
      />
      <div
        className='bg-muted items-center justify-center rounded-md border'
        style={{ display: 'none', width: 40, height: 40 }}
      >
        <BoxIcon className='text-muted-foreground size-4' />
      </div>
    </div>
  )
}

function ComponentRow({ comp }: { comp: ComponentResponse }) {
  const isVD = comp.componentType === 'PRODUCT_VISUAL_DEFINITION'
  const isProduct = comp.componentType === 'PRODUCT'
  const isEmbroidery = comp.componentType === 'EMBROIDERY'

  // Determine display name
  const displayName = isVD
    ? comp.visualDefinitionProductName ?? comp.productName ?? 'Producto'
    : comp.productName ?? comp.embroideryName ?? '—'

  // Determine image URL for types that have one
  const imageUrl = isVD && comp.productVisualDefinitionId
    ? getVisualDefinitionImageUrl(comp.productVisualDefinitionId)
    : isEmbroidery && comp.embroideryId
      ? getEmbroideryImageUrl(comp.embroideryId)
      : null

  return (
    <div className='flex items-center gap-3 px-3 py-2'>
      {/* Thumbnail or icon */}
      {imageUrl ? (
        <ComponentImageThumb src={imageUrl} alt={displayName} />
      ) : (
        <div className='flex size-10 shrink-0 items-center justify-center rounded-md border bg-muted'>
          {isProduct && <PackageIcon className='text-muted-foreground size-4' />}
          {isEmbroidery && <PaletteIcon className='text-muted-foreground size-4' />}
          {isVD && <BoxIcon className='text-muted-foreground size-4' />}
        </div>
      )}

      {/* Info */}
      <div className='min-w-0 flex-1'>
        <div className='flex items-center gap-1.5'>
          {/* Type badge */}
          <Badge variant='outline' className='shrink-0 text-[9px] uppercase tracking-wider'>
            {isVD ? 'Visual Def' : isEmbroidery ? 'Bordado' : 'Producto'}
          </Badge>
          <span className='truncate text-sm font-medium'>{displayName}</span>
        </div>
        {/* Variant badges for PRODUCT_VISUAL_DEFINITION */}
        {isVD && comp.visualDefinitionVariants.length > 0 && (
          <div className='mt-1 flex flex-wrap gap-1'>
            {comp.visualDefinitionVariants.map((v) => (
              <Badge
                key={`${v.systemName}-${v.variantValue}`}
                variant='secondary'
                className='text-[10px]'
              >
                {v.systemName ? `${v.systemName}: ` : ''}{v.variantValue}
              </Badge>
            ))}
          </div>
        )}
        {/* Placement */}
        {comp.placement && (
          <span className='text-muted-foreground mt-0.5 block text-[10px]'>
            Ubicación: {comp.placement}
          </span>
        )}
      </div>

      {/* Right-side badges */}
      <div className='flex shrink-0 items-center gap-1.5'>
        {comp.quantity > 1 && (
          <Badge variant='secondary' className='text-[10px]'>×{comp.quantity}</Badge>
        )}
        {comp.extraPrice != null && comp.extraPrice > 0 && (
          <span className='text-muted-foreground text-xs'>
            +${comp.extraPrice.toFixed(2)}
          </span>
        )}
        {!comp.isRequired && (
          <Badge variant='outline' className='text-[10px]'>Opcional</Badge>
        )}
      </div>
    </div>
  )
}

function VisualDefinitionCard({ vd }: { vd: VisualDefinitionResponse }) {
  const [componentsOpen, setComponentsOpen] = useState(false)

  // Pricing display
  const pricingLabel = vd.fixedPriceAmount != null
    ? `$${vd.fixedPriceAmount.toLocaleString('es-MX', { minimumFractionDigits: 2 })}`
    : vd.discountType !== 'NONE' && vd.discountValue != null
      ? vd.discountType === 'PERCENT'
        ? `${vd.discountValue}% desc.`
        : `-$${vd.discountValue.toLocaleString('es-MX', { minimumFractionDigits: 2 })}`
      : null

  return (
    <div className='bg-background overflow-hidden rounded-lg border'>
      {/* Card header */}
      <div className='flex items-start gap-3 p-3'>
        <div className='shrink-0'>
          <ComboImageThumb vd={vd} />
        </div>

        <div className='min-w-0 flex-1 space-y-1.5'>
          <div className='flex items-center gap-2'>
            <span className='text-sm font-medium leading-tight'>{vd.name}</span>
            {!vd.isActive && (
              <Badge variant='outline' className='text-destructive border-destructive/30 text-[10px]'>
                Inactivo
              </Badge>
            )}
          </div>
          {vd.description && (
            <p className='text-muted-foreground text-xs line-clamp-2'>{vd.description}</p>
          )}
          <div className='flex items-center gap-2'>
            <p className='text-muted-foreground text-xs'>
              {vd.componentCount} {vd.componentCount === 1 ? 'componente' : 'componentes'}
            </p>
            {pricingLabel && (
              <Badge variant='secondary' className='gap-1 text-[10px]'>
                <DollarSignIcon className='size-2.5' />
                {pricingLabel}
              </Badge>
            )}
          </div>
        </div>
      </div>

      {/* Schools */}
      {vd.schools.length > 0 && (
        <div className='flex flex-wrap items-center gap-1.5 border-t px-3 py-2'>
          <GraduationCapIcon className='text-muted-foreground size-3' />
          {vd.schools.map((s) => (
            <Badge key={s.schoolId} variant='secondary' className='text-[10px]'>
              {s.name}
            </Badge>
          ))}
        </div>
      )}

      {/* Collapsible components section */}
      <Collapsible open={componentsOpen} onOpenChange={setComponentsOpen}>
        <CollapsibleTrigger asChild>
          <button
            className='hover:bg-muted/50 flex w-full items-center justify-between border-t px-3 py-2 text-xs font-medium transition-colors'
          >
            <span className='flex items-center gap-1.5'>
              <PackageIcon className='size-3' />
              Componentes
            </span>
            <ChevronDownIcon
              className={`size-3.5 transition-transform duration-200 ${componentsOpen ? 'rotate-180' : ''}`}
            />
          </button>
        </CollapsibleTrigger>
        <CollapsibleContent>
          <div className='divide-y border-t'>
            {vd.components.length > 0 ? (
              vd.components.map((comp) => (
                <ComponentRow key={comp.productComboComponentId} comp={comp} />
              ))
            ) : (
              <p className='text-muted-foreground px-3 py-3 text-xs text-center'>
                Sin componentes
              </p>
            )}
          </div>
        </CollapsibleContent>
      </Collapsible>
    </div>
  )
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
  const [selectedCombo, setSelectedCombo] =
    useState<ProductComboResponse | null>(null)

  // ── Image preview state ────────────────────────────
  const [previewImage, setPreviewImage] = useState<{ url: string; alt: string } | null>(null)
  const openPreview = useCallback((url: string, alt: string) => {
    setPreviewImage({ url, alt })
  }, [])

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
        case 'isActive':
          next.isActive =
            value === 'all' || value === ''
              ? undefined
              : value === 'true'
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
    if (queryParams.isActive != null) count++
    return count
  }, [queryParams])

  // ── Table ──────────────────────────────────────────

  const columns = useMemo(
    () =>
      createColumns({
        onEdit: handleEdit,
        onDelete: handleDelete,
        queryParams,
        onSort: handleSort,
        onFilter: handleColumnFilter,
      }),
    [handleEdit, handleDelete, queryParams, handleSort, handleColumnFilter],
  )

  const table = useReactTable({
    data: combos,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    manualPagination: true,
    pageCount: totalPages,
    getRowCanExpand: () => true,
  })

  // ── Render ─────────────────────────────────────────

  return (
    <ImagePreviewContext.Provider value={openPreview}>
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
                      <>
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
                        {row.getIsExpanded() && (
                          <TableRow key={`${row.id}-expanded`}>
                            <TableCell colSpan={columns.length} className='p-0 bg-muted/30'>
                              <ExpandedComboRow row={row} />
                            </TableCell>
                          </TableRow>
                        )}
                      </>
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

      {/* Image Preview */}
      <Dialog open={previewImage !== null} onOpenChange={(open) => !open && setPreviewImage(null)}>
        <DialogContent className='max-w-lg p-2 sm:max-w-xl'>
          <VisuallyHidden>
            <DialogTitle>{previewImage?.alt ?? 'Vista previa'}</DialogTitle>
          </VisuallyHidden>
          {previewImage && (
            <img
              src={previewImage.url}
              alt={previewImage.alt}
              className='w-full rounded-md object-contain'
            />
          )}
        </DialogContent>
      </Dialog>
    </DashboardLayout>
    </ImagePreviewContext.Provider>
  )
}
