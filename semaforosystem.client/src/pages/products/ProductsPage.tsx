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
  PackageIcon,
  TagIcon,
  DollarSignIcon,
  WarehouseIcon,
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
  ProductResponse,
  ProductQueryParams,
  BrandLookup,
  CategoryInfo,
} from '@/services/product-service'
import { getProductPictureUrl } from '@/services/product-service'

import { useProducts, useBrands, useCategories } from '@/hooks/use-products'

import ProductFormDialog from './product-form-dialog'
import ProductDeleteDialog from './product-delete-dialog'

// ── Column sort-key map (accessorKey → backend sortBy value) ─────────

const SORT_MAP: Record<string, string> = {
  name: 'name',
  brandName: 'brand',
  model: 'model',
  serialCount: 'serialcount',
  latestPrice: 'latestprice',
  latestCost: 'latestcost',
  stockTotal: 'stocktotal',
  schoolCount: 'schoolcount',
}

// ── Columns ──────────────────────────────────────────────

interface ColumnContext {
  onEdit: (product: ProductResponse) => void
  onDelete: (product: ProductResponse) => void
  queryParams: ProductQueryParams
  onSort: (sortBy: string | undefined, sortDesc: boolean) => void
  onFilter: (key: string, value: string) => void
  brands: BrandLookup[]
  categories: CategoryInfo[]
}

function createColumns(ctx: ColumnContext): ColumnDef<ProductResponse>[] {
  const {
    onEdit,
    onDelete,
    queryParams,
    onSort,
    onFilter,
    brands,
    categories,
  } = ctx

  const brandFilterOptions = brands.map((b) => ({
    label: b.name ?? String(b.brandId),
    value: String(b.brandId),
  }))

  const categoryFilterOptions = categories.map((c) => ({
    label: c.name,
    value: String(c.categoryId),
  }))

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
              onChange: (v: string) => onFilter('search', v),
              placeholder: 'Buscar nombre...',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          {row.original.hasPicture ? (
            <img
              src={getProductPictureUrl(row.original.productId)}
              alt={row.original.name ?? 'Producto'}
              className='size-9 rounded-lg object-cover'
              onError={(e) => {
                const target = e.currentTarget
                target.style.display = 'none'
                target.nextElementSibling?.classList.remove('hidden')
              }}
            />
          ) : null}
          <div className={`bg-primary/10 flex size-9 items-center justify-center rounded-lg ${row.original.hasPicture ? 'hidden' : ''}`}>
            <PackageIcon className='text-primary size-4' />
          </div>
          <div className='flex flex-col'>
            <span className='font-medium'>
              {row.original.name ?? 'Sin nombre'}
            </span>
            {row.original.barcode && (
              <span className='text-muted-foreground text-xs'>
                {row.original.barcode}
              </span>
            )}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'brandName',
      header: () => (
        <DataTableColumnHeader
          title='Marca'
          {...headerProps('brandName')}
          filter={
            {
              kind: 'select',
              value: queryParams.brandId ? String(queryParams.brandId) : 'all',
              onChange: (v: string) => onFilter('brandId', v),
              options: brandFilterOptions,
              placeholder: 'Todas las marcas',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) =>
        row.original.brandName ? (
          <Badge variant='secondary'>{row.original.brandName}</Badge>
        ) : (
          <span className='text-muted-foreground'>—</span>
        ),
    },
    {
      accessorKey: 'model',
      header: () => (
        <DataTableColumnHeader
          title='Modelo'
          {...headerProps('model')}
          filter={
            {
              kind: 'text',
              value: queryParams.model ?? '',
              onChange: (v: string) => onFilter('model', v),
              placeholder: 'Filtrar modelo...',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) => row.original.model || '—',
    },
    {
      accessorKey: 'categories',
      header: () => (
        <DataTableColumnHeader
          title='Categorías'
          filter={
            {
              kind: 'select',
              value: queryParams.categoryId ? String(queryParams.categoryId) : 'all',
              onChange: (v: string) => onFilter('categoryId', v),
              options: categoryFilterOptions,
              placeholder: 'Todas las categorías',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) =>
        row.original.categories.length > 0 ? (
          <div className='flex flex-wrap gap-1'>
            {row.original.categories.slice(0, 2).map((c) => (
              <Badge key={c.categoryId} variant='outline' className='gap-1 text-xs'>
                <TagIcon className='size-2.5' />
                {c.name}
              </Badge>
            ))}
            {row.original.categories.length > 2 && (
              <Badge variant='outline' className='text-xs'>
                +{row.original.categories.length - 2}
              </Badge>
            )}
          </div>
        ) : (
          <span className='text-muted-foreground'>—</span>
        ),
    },
    {
      accessorKey: 'sizeSystemName',
      header: () => <DataTableColumnHeader title='Sist. Tallas' />,
      cell: ({ row }) =>
        row.original.sizeSystemName ? (
          <Badge variant='outline'>{row.original.sizeSystemName}</Badge>
        ) : (
          <span className='text-muted-foreground'>—</span>
        ),
    },
    {
      accessorKey: 'serialCount',
      header: () => (
        <DataTableColumnHeader title='Series' {...headerProps('serialCount')} />
      ),
      cell: ({ row }) => row.original.serialCount ?? '—',
    },
    {
      accessorKey: 'latestPrice',
      header: () => (
        <DataTableColumnHeader title='Precio' {...headerProps('latestPrice')} />
      ),
      cell: ({ row }) =>
        row.original.latestPrice != null ? (
          <div className='flex items-center gap-1'>
            <DollarSignIcon className='text-emerald-500 size-3.5' />
            <span className='font-medium'>
              {row.original.latestPrice.toLocaleString('es-MX', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2,
              })}
            </span>
          </div>
        ) : (
          <span className='text-muted-foreground'>—</span>
        ),
    },
    {
      accessorKey: 'latestCost',
      header: () => (
        <DataTableColumnHeader title='Costo' {...headerProps('latestCost')} />
      ),
      cell: ({ row }) =>
        row.original.latestCost != null ? (
          <span className='text-muted-foreground text-sm'>
            ${row.original.latestCost.toLocaleString('es-MX', {
              minimumFractionDigits: 2,
              maximumFractionDigits: 2,
            })}
          </span>
        ) : (
          <span className='text-muted-foreground'>—</span>
        ),
    },
    {
      accessorKey: 'stockTotal',
      header: () => (
        <DataTableColumnHeader
          title='Stock'
          {...headerProps('stockTotal')}
          filter={
            {
              kind: 'boolean',
              value: queryParams.hasStock != null ? String(queryParams.hasStock) : 'all',
              onChange: (v: string) => onFilter('hasStock', v),
              trueLabel: 'Con stock',
              falseLabel: 'Sin stock',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) => (
        <div className='flex items-center gap-1'>
          <WarehouseIcon className='text-muted-foreground size-3.5' />
          <Badge
            variant={row.original.stockTotal > 0 ? 'secondary' : 'outline'}
          >
            {row.original.stockTotal}
          </Badge>
        </div>
      ),
    },
    {
      accessorKey: 'schoolCount',
      header: () => (
        <DataTableColumnHeader
          title='Escuelas'
          {...headerProps('schoolCount')}
          filter={
            {
              kind: 'boolean',
              value: queryParams.hasSchools != null ? String(queryParams.hasSchools) : 'all',
              onChange: (v: string) => onFilter('hasSchools', v),
              trueLabel: 'Con escuelas',
              falseLabel: 'Sin escuelas',
            } satisfies ColumnFilterType
          }
        />
      ),
      cell: ({ row }) => (
        <Badge variant='secondary'>{row.original.schoolCount}</Badge>
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

// ── Page Component ───────────────────────────────────────

export default function ProductsPage() {
  // ── Query state ────────────────────────────────────
  const [queryParams, setQueryParams] = useState<ProductQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDescending: false,
  })
  const [searchInput, setSearchInput] = useState('')

  // ── Dialog state ───────────────────────────────────
  const [formOpen, setFormOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [selectedProduct, setSelectedProduct] =
    useState<ProductResponse | null>(null)

  // ── Data fetching ──────────────────────────────────
  const { data, isLoading, isFetching } = useProducts(queryParams)
  const { data: brands = [] } = useBrands()
  const { data: categories = [] } = useCategories()

  const products = data?.items ?? []
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
      const next: ProductQueryParams = { ...prev, page: 1 }

      switch (key) {
        case 'search':
          next.search = value.trim() || undefined
          break
        case 'brandId':
          next.brandId = value && value !== 'all' ? Number(value) : undefined
          break
        case 'categoryId':
          next.categoryId =
            value && value !== 'all' ? Number(value) : undefined
          break
        case 'model':
          next.model = value.trim() || undefined
          break
        case 'hasSchools':
          next.hasSchools =
            value === 'all' || value === ''
              ? undefined
              : value === 'true'
          break
        case 'hasStock':
          next.hasStock =
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
    setSelectedProduct(null)
    setFormOpen(true)
  }, [])

  const handleEdit = useCallback((product: ProductResponse) => {
    setSelectedProduct(product)
    setFormOpen(true)
  }, [])

  const handleDelete = useCallback((product: ProductResponse) => {
    setSelectedProduct(product)
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

  // ── Active filter count (for UI feedback) ──────────
  const activeFilterCount = useMemo(() => {
    let count = 0
    if (queryParams.search) count++
    if (queryParams.brandId) count++
    if (queryParams.categoryId) count++
    if (queryParams.model) count++
    if (queryParams.hasSchools != null) count++
    if (queryParams.hasStock != null) count++
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
        brands,
        categories,
      }),
    [handleEdit, handleDelete, queryParams, handleSort, handleColumnFilter, brands, categories],
  )

  const table = useReactTable({
    data: products,
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
            <h1 className='text-2xl font-bold tracking-tight'>Productos</h1>
            <p className='text-muted-foreground text-sm'>
              Gestiona el catálogo de productos del sistema.
            </p>
          </div>
          <Button onClick={handleCreate} className='gap-2'>
            <PlusIcon className='size-4' />
            Nuevo Producto
          </Button>
        </div>

        {/* Search + Filters bar */}
        <Card>
          <CardContent className='pt-6'>
            <div className='flex flex-col gap-3 sm:flex-row sm:items-center'>
              {/* Search */}
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Buscar por nombre, código o descripción...'
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
                    {totalCount === 1
                      ? 'producto encontrado'
                      : 'productos encontrados'}
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
                          <PackageIcon className='text-muted-foreground size-8' />
                          <p className='text-muted-foreground'>
                            No se encontraron productos.
                          </p>
                          <Button
                            variant='link'
                            size='sm'
                            onClick={handleCreate}
                          >
                            Crear el primer producto
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
      <ProductFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        product={selectedProduct}
      />
      <ProductDeleteDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        product={selectedProduct}
      />
    </DashboardLayout>
  )
}
