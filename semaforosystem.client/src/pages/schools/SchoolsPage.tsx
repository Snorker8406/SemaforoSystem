import { useState, useCallback } from 'react'
import { useQuery } from '@tanstack/react-query'
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
  SchoolIcon,
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
  SchoolResponse,
  SchoolQueryParams,
} from '@/services/school-service'
import { getSchools, getSchoolLevels } from '@/services/school-service'

import SchoolFormDialog from './school-form-dialog'
import SchoolDeleteDialog from './school-delete-dialog'

// ── Columns ──────────────────────────────────────────────

function createColumns(
  onEdit: (school: SchoolResponse) => void,
  onDelete: (school: SchoolResponse) => void,
): ColumnDef<SchoolResponse>[] {
  return [
    {
      accessorKey: 'name',
      header: 'Nombre',
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <SchoolIcon className='text-primary size-4' />
          </div>
          <div className='flex flex-col'>
            <span className='font-medium'>{row.original.name}</span>
            {row.original.email && (
              <span className='text-muted-foreground text-xs'>
                {row.original.email}
              </span>
            )}
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'schoolLevelName',
      header: 'Nivel',
      cell: ({ row }) => (
        <Badge variant='secondary'>{row.original.schoolLevelName}</Badge>
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
      accessorKey: 'phoneNumber',
      header: 'Teléfono',
      cell: ({ row }) => row.original.phoneNumber || '—',
    },
    {
      accessorKey: 'principalInfo',
      header: 'Director',
      cell: ({ row }) => (
        <span className='max-w-[150px] truncate block'>
          {row.original.principalInfo || '—'}
        </span>
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

export default function SchoolsPage() {
  // ── Query state ────────────────────────────────────
  const [queryParams, setQueryParams] = useState<SchoolQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDescending: false,
  })
  const [searchInput, setSearchInput] = useState('')

  // ── Dialog state ───────────────────────────────────
  const [formOpen, setFormOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [selectedSchool, setSelectedSchool] = useState<SchoolResponse | null>(
    null,
  )

  // ── Data fetching ──────────────────────────────────
  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['schools', queryParams],
    queryFn: () => getSchools(queryParams),
    placeholderData: (previousData) => previousData,
  })

  const { data: levels = [] } = useQuery({
    queryKey: ['school-levels'],
    queryFn: getSchoolLevels,
    staleTime: 10 * 60 * 1000,
  })

  const schools = data?.items ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = data?.totalPages ?? 0
  const currentPage = data?.page ?? 1

  // ── Handlers ───────────────────────────────────────

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

  const handleLevelFilter = useCallback((value: string) => {
    setQueryParams((prev) => ({
      ...prev,
      schoolLevelId: value === 'all' ? undefined : Number(value),
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
    setSelectedSchool(null)
    setFormOpen(true)
  }, [])

  const handleEdit = useCallback((school: SchoolResponse) => {
    setSelectedSchool(school)
    setFormOpen(true)
  }, [])

  const handleDelete = useCallback((school: SchoolResponse) => {
    setSelectedSchool(school)
    setDeleteOpen(true)
  }, [])

  // ── Table ──────────────────────────────────────────

  const columns = createColumns(handleEdit, handleDelete)

  const table = useReactTable({
    data: schools,
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
            <h1 className='text-2xl font-bold tracking-tight'>Escuelas</h1>
            <p className='text-muted-foreground text-sm'>
              Gestiona el catálogo de escuelas del sistema.
            </p>
          </div>
          <Button onClick={handleCreate} className='gap-2'>
            <PlusIcon className='size-4' />
            Nueva Escuela
          </Button>
        </div>

        {/* Filters */}
        <Card>
          <CardContent className='pt-6'>
            <div className='flex flex-col gap-3 sm:flex-row sm:items-center'>
              {/* Search */}
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Buscar por nombre, email o dirección...'
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
              {/* Level filter */}
              <Select
                value={
                  queryParams.schoolLevelId
                    ? String(queryParams.schoolLevelId)
                    : 'all'
                }
                onValueChange={handleLevelFilter}
              >
                <SelectTrigger className='w-full sm:w-[200px]'>
                  <SelectValue placeholder='Todos los niveles' />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='all'>Todos los niveles</SelectItem>
                  {levels.map((level) => (
                    <SelectItem
                      key={level.schoolLevelId}
                      value={String(level.schoolLevelId)}
                    >
                      {level.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
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
            <div className='flex items-center justify-between'>
              <CardTitle className='text-base font-medium'>
                {isLoading ? (
                  <Skeleton className='h-5 w-40' />
                ) : (
                  <>
                    {totalCount}{' '}
                    {totalCount === 1 ? 'escuela encontrada' : 'escuelas encontradas'}
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
                          <SchoolIcon className='text-muted-foreground size-8' />
                          <p className='text-muted-foreground'>
                            No se encontraron escuelas.
                          </p>
                          <Button
                            variant='link'
                            size='sm'
                            onClick={handleCreate}
                          >
                            Crear la primera escuela
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
      <SchoolFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        school={selectedSchool}
      />
      <SchoolDeleteDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        school={selectedSchool}
      />
    </DashboardLayout>
  )
}
