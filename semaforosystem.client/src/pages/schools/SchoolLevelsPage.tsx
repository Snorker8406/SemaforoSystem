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
  LayersIcon,
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
  SchoolLevelResponse,
  SchoolLevelQueryParams,
} from '@/services/school-level-service'
import { getSchoolLevels } from '@/services/school-level-service'

import SchoolLevelFormDialog from './school-level-form-dialog'
import SchoolLevelDeleteDialog from './school-level-delete-dialog'

// ── Columns ──────────────────────────────────────────────

function createColumns(
  onEdit: (level: SchoolLevelResponse) => void,
  onDelete: (level: SchoolLevelResponse) => void,
): ColumnDef<SchoolLevelResponse>[] {
  return [
    {
      accessorKey: 'name',
      header: 'Nombre',
      cell: ({ row }) => (
        <div className='flex items-center gap-2'>
          <div className='bg-primary/10 flex size-9 items-center justify-center rounded-lg'>
            <LayersIcon className='text-primary size-4' />
          </div>
          <span className='font-medium'>{row.original.name}</span>
        </div>
      ),
    },
    {
      accessorKey: 'description',
      header: 'Descripción',
      cell: ({ row }) => (
        <span className='text-muted-foreground max-w-[300px] truncate block'>
          {row.original.description || '—'}
        </span>
      ),
    },
    {
      accessorKey: 'schoolCount',
      header: 'Escuelas',
      cell: ({ row }) => (
        <Badge variant='secondary' className='gap-1'>
          <SchoolIcon className='size-3' />
          {row.original.schoolCount}
        </Badge>
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

export default function SchoolLevelsPage() {
  // ── Query state ────────────────────────────────────
  const [queryParams, setQueryParams] = useState<SchoolLevelQueryParams>({
    page: 1,
    pageSize: 10,
    sortBy: 'name',
    sortDescending: false,
  })
  const [searchInput, setSearchInput] = useState('')

  // ── Dialog state ───────────────────────────────────
  const [formOpen, setFormOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)
  const [selectedLevel, setSelectedLevel] =
    useState<SchoolLevelResponse | null>(null)

  // ── Data fetching ──────────────────────────────────
  const { data, isLoading, isFetching } = useQuery({
    queryKey: ['schoolLevels', queryParams],
    queryFn: () => getSchoolLevels(queryParams),
    placeholderData: (previousData) => previousData,
  })

  const levels = data?.items ?? []
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

  const handlePageChange = useCallback((page: number) => {
    setQueryParams((prev) => ({ ...prev, page }))
  }, [])

  const handlePageSizeChange = useCallback((value: string) => {
    setQueryParams((prev) => ({ ...prev, pageSize: Number(value), page: 1 }))
  }, [])

  const handleCreate = useCallback(() => {
    setSelectedLevel(null)
    setFormOpen(true)
  }, [])

  const handleEdit = useCallback((level: SchoolLevelResponse) => {
    setSelectedLevel(level)
    setFormOpen(true)
  }, [])

  const handleDelete = useCallback((level: SchoolLevelResponse) => {
    setSelectedLevel(level)
    setDeleteOpen(true)
  }, [])

  // ── Table ──────────────────────────────────────────

  const columns = createColumns(handleEdit, handleDelete)

  const table = useReactTable({
    data: levels,
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
            <h1 className='text-2xl font-bold tracking-tight'>
              Niveles Escolares
            </h1>
            <p className='text-muted-foreground text-sm'>
              Gestiona los niveles escolares del sistema.
            </p>
          </div>
          <Button onClick={handleCreate} className='gap-2'>
            <PlusIcon className='size-4' />
            Nuevo Nivel
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
                    {totalCount === 1
                      ? 'nivel encontrado'
                      : 'niveles encontrados'}
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
                            No se encontraron niveles escolares.
                          </p>
                          <Button
                            variant='link'
                            size='sm'
                            onClick={handleCreate}
                          >
                            Crear el primer nivel
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
      <SchoolLevelFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        level={selectedLevel}
      />
      <SchoolLevelDeleteDialog
        open={deleteOpen}
        onOpenChange={setDeleteOpen}
        level={selectedLevel}
      />
    </DashboardLayout>
  )
}
