import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  AlertTriangleIcon,
  FilterIcon,
  PlusIcon,
  ReceiptIcon,
  SearchIcon,
} from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
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
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination'

import {
  getPayableStatuses,
  getProviderPayables,
  type ProviderPayableListItem,
  type ProviderPayableQueryParams,
} from '@/services/provider-payable-service'

import ProviderPayableFormDialog from './provider-payable-form-dialog'
import ProviderPayableDetailSheet from './provider-payable-detail-sheet'

interface ProviderPayablesTabProps {
  providerId?: number
  providerCurrency?: string
  title?: string
}

function statusVariant(code: string): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (code) {
    case 'PAID':
      return 'default'
    case 'OVERDUE':
      return 'destructive'
    case 'CANCELED':
    case 'CLOSED':
      return 'outline'
    default:
      return 'secondary'
  }
}

function formatMoney(amount: number, currency: string) {
  return `${amount.toLocaleString('es-MX', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} ${currency}`
}

function formatDate(d?: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('es-MX')
}

export default function ProviderPayablesTab({
  providerId,
  title = 'Cuentas por pagar',
}: ProviderPayablesTabProps) {
  const isGlobal = providerId === undefined
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [search, setSearch] = useState('')
  const [statusId, setStatusId] = useState<string>('all')
  const [onlyOpen, setOnlyOpen] = useState(false)
  const [onlyOverdue, setOnlyOverdue] = useState(false)

  const [createOpen, setCreateOpen] = useState(false)
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [detailOpen, setDetailOpen] = useState(false)

  const { data: statuses = [] } = useQuery({
    queryKey: ['payable-statuses'],
    queryFn: getPayableStatuses,
    staleTime: 10 * 60 * 1000,
  })

  const params: ProviderPayableQueryParams = {
    providerId,
    page,
    pageSize,
    sortBy: 'documentDate',
    sortDescending: true,
    search: search.trim() || undefined,
    providerPayableStatusId:
      statusId && statusId !== 'all' ? Number(statusId) : undefined,
    onlyOpen: onlyOpen || undefined,
    onlyOverdue: onlyOverdue || undefined,
  }

  const { data, isLoading } = useQuery({
    queryKey: ['provider-payables', providerId ?? 'all', params],
    queryFn: () => getProviderPayables(params),
  })

  const totalPages = data?.totalPages ?? 1

  function openDetail(p: ProviderPayableListItem) {
    setSelectedId(p.providerPayableId)
    setDetailOpen(true)
  }

  return (
    <>
      <Card>
        <CardHeader className='flex flex-row items-center justify-between pb-3'>
          <CardTitle className='text-base font-medium'>{title}</CardTitle>
          <Button
            size='sm'
            onClick={() => setCreateOpen(true)}
            className='gap-2'
          >
            <PlusIcon className='size-4' />
            Nueva cuenta
          </Button>
        </CardHeader>
        <CardContent className='space-y-4'>
          {/* Filters */}
          <div className='grid grid-cols-1 md:grid-cols-4 gap-3 items-end'>
            <div className='relative md:col-span-2'>
              <SearchIcon className='absolute left-2 top-1/2 -translate-y-1/2 text-muted-foreground size-4' />
              <Input
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  setPage(1)
                }}
                placeholder='Buscar por folio, referencia, proveedor...'
                className='pl-8'
              />
            </div>
            <div>
              <Label className='text-xs flex items-center gap-1 mb-1'>
                <FilterIcon className='size-3' />
                Estado
              </Label>
              <Select
                value={statusId}
                onValueChange={(v) => {
                  setStatusId(v)
                  setPage(1)
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder='Todos' />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='all'>Todos</SelectItem>
                  {statuses.map((s) => (
                    <SelectItem key={s.id} value={String(s.id)}>
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className='flex items-center gap-4'>
              <div className='flex items-center gap-2'>
                <Switch
                  id='onlyOpen'
                  checked={onlyOpen}
                  onCheckedChange={(v) => {
                    setOnlyOpen(v)
                    setPage(1)
                  }}
                />
                <Label htmlFor='onlyOpen' className='text-xs'>
                  Abiertas
                </Label>
              </div>
              <div className='flex items-center gap-2'>
                <Switch
                  id='onlyOverdue'
                  checked={onlyOverdue}
                  onCheckedChange={(v) => {
                    setOnlyOverdue(v)
                    setPage(1)
                  }}
                />
                <Label htmlFor='onlyOverdue' className='text-xs'>
                  Vencidas
                </Label>
              </div>
            </div>
          </div>

          {/* Table */}
          {isLoading ? (
            <div className='space-y-2'>
              <Skeleton className='h-10 w-full' />
              <Skeleton className='h-10 w-full' />
              <Skeleton className='h-10 w-full' />
            </div>
          ) : !data || data.items.length === 0 ? (
            <div className='flex flex-col items-center gap-2 py-10 text-center'>
              <ReceiptIcon className='text-muted-foreground size-8' />
              <p className='text-muted-foreground text-sm'>
                {isGlobal
                  ? 'No hay cuentas por pagar registradas.'
                  : 'Este proveedor no tiene cuentas por pagar registradas.'}
              </p>
              {!isGlobal && (
                <Button variant='link' size='sm' onClick={() => setCreateOpen(true)}>
                  Crear la primera
                </Button>
              )}
            </div>
          ) : (
            <>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Folio</TableHead>
                    {isGlobal && <TableHead>Proveedor</TableHead>}
                    <TableHead>Tipo</TableHead>
                    <TableHead>Documento</TableHead>
                    <TableHead>Vencimiento</TableHead>
                    <TableHead>Estado</TableHead>
                    <TableHead className='text-right'>Total</TableHead>
                    <TableHead className='text-right'>Pagado</TableHead>
                    <TableHead className='text-right'>Saldo</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {data.items.map((p) => (
                    <TableRow
                      key={p.providerPayableId}
                      onClick={() => openDetail(p)}
                      className='cursor-pointer hover:bg-muted/50'
                    >
                      <TableCell className='font-medium'>
                        {p.documentNumber ?? `#${p.providerPayableId}`}
                      </TableCell>
                      {isGlobal && (
                        <TableCell className='text-sm'>
                          <div className='min-w-0'>
                            <p className='truncate font-medium'>
                              {p.providerTradeName ?? p.providerLegalName}
                            </p>
                            {p.providerTradeName && (
                              <p className='text-muted-foreground text-xs truncate'>
                                {p.providerLegalName}
                              </p>
                            )}
                          </div>
                        </TableCell>
                      )}
                      <TableCell className='text-sm'>{p.typeName}</TableCell>
                      <TableCell className='text-sm'>
                        {formatDate(p.documentDate)}
                      </TableCell>
                      <TableCell className='text-sm'>
                        <span
                          className={
                            p.isOverdue ? 'text-destructive font-medium' : ''
                          }
                        >
                          {formatDate(p.dueDate)}
                        </span>
                      </TableCell>
                      <TableCell>
                        <div className='flex items-center gap-1 flex-wrap'>
                          <Badge variant={statusVariant(p.statusCode)}>
                            {p.statusName}
                          </Badge>
                          {p.isOverdue && (
                            <Badge
                              variant='outline'
                              className='gap-1 border-destructive text-destructive text-[10px]'
                            >
                              <AlertTriangleIcon className='size-3' />
                              Vencida
                            </Badge>
                          )}
                        </div>
                      </TableCell>
                      <TableCell className='text-right'>
                        {formatMoney(p.total, p.currencyCode)}
                      </TableCell>
                      <TableCell className='text-right text-green-600'>
                        {formatMoney(p.paidAmount, p.currencyCode)}
                      </TableCell>
                      <TableCell
                        className={`text-right font-medium ${
                          p.balance <= 0
                            ? 'text-green-600'
                            : p.isOverdue
                              ? 'text-destructive'
                              : ''
                        }`}
                      >
                        {formatMoney(p.balance, p.currencyCode)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>

              {totalPages > 1 && (
                <Pagination>
                  <PaginationContent>
                    <PaginationItem>
                      <PaginationPrevious
                        onClick={() => setPage((p) => Math.max(1, p - 1))}
                        aria-disabled={page === 1}
                        className={
                          page === 1 ? 'pointer-events-none opacity-50' : 'cursor-pointer'
                        }
                      />
                    </PaginationItem>
                    <PaginationItem>
                      <PaginationLink isActive>
                        {page} / {totalPages}
                      </PaginationLink>
                    </PaginationItem>
                    <PaginationItem>
                      <PaginationNext
                        onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                        aria-disabled={page >= totalPages}
                        className={
                          page >= totalPages
                            ? 'pointer-events-none opacity-50'
                            : 'cursor-pointer'
                        }
                      />
                    </PaginationItem>
                  </PaginationContent>
                </Pagination>
              )}
            </>
          )}
        </CardContent>
      </Card>

      <ProviderPayableFormDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        providerId={providerId ?? 0}
      />

      <ProviderPayableDetailSheet
        open={detailOpen}
        onOpenChange={(o) => {
          setDetailOpen(o)
          if (!o) setSelectedId(null)
        }}
        payableId={selectedId}
        providerId={providerId ?? 0}
      />
    </>
  )
}
