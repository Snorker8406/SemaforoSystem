import { useState, useCallback, useMemo, memo, Fragment } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import type { ColumnDef } from '@tanstack/react-table'

import {
  ChevronDownIcon,
  ChevronRightIcon,
  EyeIcon,
  EyeOffIcon,
  PackageSearchIcon,
  SchoolIcon,
  PackageIcon,
  SearchIcon,
  XIcon,
} from 'lucide-react'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
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

import type { SchoolWithProducts, SchoolProductItem } from '@/services/supply-process-service'
import { getSchoolsWithProducts } from '@/services/supply-process-service'

// ── Columns (stable reference) ───────────────────────────

const columns: ColumnDef<SchoolWithProducts>[] = [
    {
      id: 'expander',
      header: '',
      size: 40,
      cell: ({ row }) => (
        <Button
          variant='ghost'
          size='icon'
          className='size-7'
          onClick={(e) => {
            e.stopPropagation()
            row.toggleExpanded()
          }}
        >
          {row.getIsExpanded() ? (
            <ChevronDownIcon className='size-4' />
          ) : (
            <ChevronRightIcon className='size-4' />
          )}
        </Button>
      ),
    },
    {
      accessorKey: 'name',
      header: 'Escuela',
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <SchoolIcon className='text-primary size-4' />
          </div>
          <div className='flex flex-col'>
            <span className='font-medium'>{row.original.name}</span>
            <span className='text-muted-foreground text-xs'>
              {row.original.schoolLevelName}
            </span>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'ciudad',
      header: 'Ciudad',
      cell: ({ row }) => row.original.ciudad || '—',
    },
    {
      accessorKey: 'state',
      header: 'Estado',
      cell: ({ row }) => row.original.state || '—',
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
  ]

// ── Stable fn ref for getRowCanExpand ─────────────────────
const rowCanExpand = () => true

// ── Product sub-row ──────────────────────────────────────

const ProductSubRow = memo(function ProductSubRow({ product }: { product: SchoolProductItem }) {
  const [schoolsOpen, setSchoolsOpen] = useState(false)

  return (
    <div
      className='rounded-md border px-3 py-2'
      style={{
        backgroundColor: product.schoolCommonProduct
          ? 'rgba(34, 197, 94, 0.08)'
          : 'rgba(239, 68, 68, 0.08)',
        borderColor: product.schoolCommonProduct
          ? 'rgba(34, 197, 94, 0.25)'
          : 'rgba(239, 68, 68, 0.25)',
      }}
    >
      <div className='flex items-center justify-between gap-4'>
        <div className='flex items-center gap-3 min-w-0'>
          <div className='bg-muted flex size-8 items-center justify-center rounded'>
            <PackageIcon className='text-muted-foreground size-3.5' />
          </div>
          <div className='flex flex-col min-w-0'>
            <span className='text-sm font-medium truncate'>
              {product.name ?? 'Sin nombre'}
            </span>
            <span className='text-muted-foreground text-xs'>
              {product.categoryName} · Series: {product.serialCount ?? 0}
            </span>
          </div>
        </div>
        <div className='flex items-center gap-2 shrink-0'>
          <Tooltip>
            <TooltipTrigger asChild>
              <Badge
                variant={product.schoolCommonProduct ? 'default' : 'outline'}
                className='gap-1'
              >
                <SchoolIcon className='size-3' />
                {product.schoolCount}
              </Badge>
            </TooltipTrigger>
            <TooltipContent>
              {product.schoolCommonProduct
                ? 'Producto común (alcanza el umbral)'
                : 'No alcanza el umbral de escuelas'}
            </TooltipContent>
          </Tooltip>
        </div>
      </div>

      {product.schoolNames.length > 0 && (
        <Collapsible open={schoolsOpen} onOpenChange={setSchoolsOpen}>
          <CollapsibleTrigger asChild>
            <Button variant='ghost' size='sm' className='mt-1 ml-11 h-7 gap-1 text-xs'>
              {schoolsOpen ? (
                <ChevronDownIcon className='size-3' />
              ) : (
                <ChevronRightIcon className='size-3' />
              )}
              Escuelas ({product.schoolNames.length})
            </Button>
          </CollapsibleTrigger>
          <CollapsibleContent>
            <div className='mt-1 ml-11 space-y-1'>
              {product.schoolNames.map((schoolName, idx) => (
                <div
                  key={idx}
                  className='text-muted-foreground flex items-center gap-2 text-xs'
                >
                  <SchoolIcon className='size-3 shrink-0' />
                  <span>{schoolName}</span>
                </div>
              ))}
            </div>
          </CollapsibleContent>
        </Collapsible>
      )}
    </div>
  )
})

// ── Expanded row ─────────────────────────────────────────

type ProductFilter = 'all' | 'common' | 'not-common'

const ExpandedRow = memo(function ExpandedRow({
  products,
  schoolName,
  colCount,
  productFilter,
}: {
  products: SchoolProductItem[]
  schoolName: string
  colCount: number
  productFilter: ProductFilter
}) {
  const filtered = useMemo(() => {
    if (productFilter === 'all') return products
    return products.filter((p) =>
      productFilter === 'common' ? p.schoolCommonProduct : !p.schoolCommonProduct,
    )
  }, [products, productFilter])

  return (
    <TableRow className='bg-muted/30 hover:bg-muted/30'>
      <TableCell colSpan={colCount} className='p-4'>
        <div className='space-y-2'>
          <p className='text-sm font-medium mb-2'>
            Productos de{' '}
            <span className='text-primary'>{schoolName}</span>
            {productFilter !== 'all' && (
              <span className='text-muted-foreground text-xs font-normal ml-2'>
                ({filtered.length} de {products.length})
              </span>
            )}
          </p>
          {filtered.length > 0 ? (
            filtered.map((product) => (
              <ProductSubRow key={product.productId} product={product} />
            ))
          ) : (
            <p className='text-muted-foreground text-sm'>
              {productFilter === 'common'
                ? 'No hay productos comunes en esta escuela.'
                : productFilter === 'not-common'
                  ? 'No hay productos no comunes en esta escuela.'
                  : 'No hay productos escolares.'}
            </p>
          )}
        </div>
      </TableCell>
    </TableRow>
  )
})

// ── Page Component ───────────────────────────────────────

export default function CommonProductsPage() {
  const [minSchoolCount, setMinSchoolCount] = useState(3)
  const [thresholdInput, setThresholdInput] = useState('3')
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [productFilter, setProductFilter] = useState<ProductFilter>('all')

  // ── Data fetching ──────────────────────────────────
  const { data: schools = [], isLoading, isFetching } = useQuery({
    queryKey: ['schools-with-products', minSchoolCount],
    queryFn: () => getSchoolsWithProducts(minSchoolCount),
    placeholderData: (previousData) => previousData,
  })

  // ── Client-side search filter ──────────────────────
  const filteredSchools = useMemo(() => {
    if (!search) return schools
    const q = search.toLowerCase()
    return schools.filter(
      (s) =>
        s.name.toLowerCase().includes(q) ||
        s.schoolLevelName.toLowerCase().includes(q) ||
        (s.ciudad && s.ciudad.toLowerCase().includes(q)) ||
        (s.state && s.state.toLowerCase().includes(q)),
    )
  }, [schools, search])

  // ── Table ──────────────────────────────────────────
  const table = useReactTable({
    data: filteredSchools,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getExpandedRowModel: getExpandedRowModel(),
    getRowCanExpand: rowCanExpand,
  })

  // ── Handlers ───────────────────────────────────────
  const handleApplyThreshold = useCallback(() => {
    const val = parseInt(thresholdInput, 10)
    if (!isNaN(val) && val >= 0) {
      setMinSchoolCount(val)
    }
  }, [thresholdInput])

  const handleSearch = useCallback(() => {
    setSearch(searchInput.trim())
  }, [searchInput])

  const handleClearSearch = useCallback(() => {
    setSearchInput('')
    setSearch('')
  }, [])

  // ── Render ─────────────────────────────────────────

  return (
    <DashboardLayout>
      <div className='space-y-6'>
        {/* Header */}
        <div>
          <h1 className='text-2xl font-bold tracking-tight'>
            Productos Comunes por Escuela
          </h1>
          <p className='text-muted-foreground text-sm'>
            Visualiza los productos escolares de cada escuela y determina cuáles
            son comunes según un umbral de escuelas.
          </p>
        </div>

        {/* Filters */}
        <Card>
          <CardContent className='pt-6'>
            <div className='flex flex-col gap-3 sm:flex-row sm:items-end'>
              {/* Search */}
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Buscar escuela por nombre, nivel, ciudad...'
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
              {/* Threshold */}
              <div className='flex items-end gap-2'>
                <div className='space-y-1'>
                  <label className='text-sm font-medium'>Umbral mín. escuelas</label>
                  <Input
                    type='number'
                    min={0}
                    value={thresholdInput}
                    onChange={(e) => setThresholdInput(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && handleApplyThreshold()}
                    className='w-24'
                  />
                </div>
                <Button variant='secondary' onClick={handleApplyThreshold}>
                  Aplicar
                </Button>
              </div>
              {/* Search button */}
              <Button variant='secondary' onClick={handleSearch}>
                Buscar
              </Button>
            </div>
          </CardContent>
        </Card>

        {/* Data Table */}
        <Card>
          <CardHeader className='pb-3'>
            <div className='flex items-center justify-between flex-wrap gap-2'>
              <CardTitle className='text-base font-medium'>
                {isLoading ? (
                  <Skeleton className='h-5 w-48' />
                ) : (
                  <>
                    {filteredSchools.length}{' '}
                    {filteredSchools.length === 1
                      ? 'escuela encontrada'
                      : 'escuelas encontradas'}
                    {' · Umbral: '}
                    <Badge variant='outline' className='ml-1'>
                      ≥ {minSchoolCount}
                    </Badge>
                  </>
                )}
              </CardTitle>
              <div className='flex items-center gap-1'>
                {isFetching && !isLoading && (
                  <span className='text-muted-foreground text-xs animate-pulse mr-2'>
                    Actualizando...
                  </span>
                )}
                <Button
                  variant={productFilter === 'all' ? 'default' : 'outline'}
                  size='sm'
                  className='h-7 gap-1 text-xs'
                  onClick={() => setProductFilter('all')}
                >
                  <EyeIcon className='size-3' />
                  Todos
                </Button>
                <Button
                  variant={productFilter === 'common' ? 'default' : 'outline'}
                  size='sm'
                  className='h-7 gap-1 text-xs'
                  style={productFilter === 'common' ? { backgroundColor: 'rgba(34, 197, 94, 0.8)' } : {}}
                  onClick={() => setProductFilter('common')}
                >
                  <EyeIcon className='size-3' />
                  Comunes
                </Button>
                <Button
                  variant={productFilter === 'not-common' ? 'default' : 'outline'}
                  size='sm'
                  className='h-7 gap-1 text-xs'
                  style={productFilter === 'not-common' ? { backgroundColor: 'rgba(239, 68, 68, 0.8)' } : {}}
                  onClick={() => setProductFilter('not-common')}
                >
                  <EyeOffIcon className='size-3' />
                  No comunes
                </Button>
              </div>
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
                        <TableRow
                          className='cursor-pointer'
                          onClick={() => row.toggleExpanded()}
                        >
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
                          <ExpandedRow
                            products={row.original.products}
                            schoolName={row.original.name}
                            colCount={columns.length}
                            productFilter={productFilter}
                          />
                        )}
                      </Fragment>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell
                        colSpan={columns.length}
                        className='h-32 text-center'
                      >
                        <div className='flex flex-col items-center gap-2'>
                          <PackageSearchIcon className='text-muted-foreground size-8' />
                          <p className='text-muted-foreground'>
                            No se encontraron escuelas con productos escolares.
                          </p>
                        </div>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </CardContent>
        </Card>
      </div>
    </DashboardLayout>
  )
}
