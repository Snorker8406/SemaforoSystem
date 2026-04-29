import { useQuery } from '@tanstack/react-query'
import { useParams, useRouter, Link } from '@tanstack/react-router'
import {
  ArrowLeftIcon,
  BanknoteIcon,
  CalendarIcon,
  HashIcon,
  InfoIcon,
  PackageIcon,
  ReceiptIcon,
  StoreIcon,
  UserIcon,
  WalletIcon,
} from 'lucide-react'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

import { getSale } from '@/services/sale-service'
import type { SaleDetailResponse } from '@/services/sale-service'

// ── Helpers ───────────────────────────────────────────────

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('es-MX', {
    style: 'currency',
    currency: 'MXN',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(date: string | null | undefined) {
  if (!date) return '—'
  return new Date(date).toLocaleDateString('es-MX', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

function formatDateTime(date: string | null | undefined) {
  if (!date) return '—'
  return new Date(date).toLocaleString('es-MX', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

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

function lineTypeVariant(
  type: string,
): 'default' | 'secondary' | 'outline' {
  switch (type?.toUpperCase()) {
    case 'PRODUCT':
      return 'default'
    case 'COMBO':
      return 'secondary'
    case 'SERVICE':
    case 'EMBROIDERY':
      return 'outline'
    case 'FREE_TEXT':
      return 'outline'
    default:
      return 'outline'
  }
}

function lineTypeLabel(type: string) {
  const labels: Record<string, string> = {
    PRODUCT: 'Producto',
    COMBO: 'Combo',
    SERVICE: 'Servicio',
    EMBROIDERY: 'Bordado',
    FREE_TEXT: 'Texto libre',
  }
  return labels[type?.toUpperCase()] ?? type
}

// ── Header section ────────────────────────────────────────

function SaleHeader({ sale }: { sale: SaleDetailResponse }) {
  return (
    <div className='grid gap-4 md:grid-cols-2 lg:grid-cols-4'>
      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Total
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className='text-2xl font-bold'>{formatCurrency(sale.total)}</p>
          <p className='text-muted-foreground text-xs mt-1'>
            Subtotal: {formatCurrency(sale.subtotal)} · Imp.:{' '}
            {formatCurrency(sale.taxTotal)}
          </p>
          {sale.discountTotal > 0 && (
            <p className='text-muted-foreground text-xs'>
              Descuento: {formatCurrency(sale.discountTotal)}
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Saldo
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p
            className={`text-2xl font-bold ${
              sale.balance > 0 ? 'text-destructive' : 'text-green-600'
            }`}
          >
            {formatCurrency(sale.balance)}
          </p>
          <p className='text-muted-foreground text-xs mt-1'>
            Pagado: {formatCurrency(sale.paidTotal)}
          </p>
          {sale.accountId && (
            <Link
              to='/accounts/$accountId'
              params={{ accountId: String(sale.accountId) }}
              className='text-primary mt-1 inline-flex items-center gap-1 text-xs hover:underline'
            >
              <WalletIcon className='size-3' />
              Cuenta #{sale.accountId}
            </Link>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Cliente
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className='font-semibold flex items-center gap-2'>
            <UserIcon className='text-muted-foreground size-4' />
            {sale.clientName ?? 'Sin cliente'}
          </p>
          <p className='text-muted-foreground text-xs mt-1 flex items-center gap-2'>
            <StoreIcon className='size-3' /> {sale.siteName ?? '—'}
          </p>
          <p className='text-muted-foreground text-xs flex items-center gap-2'>
            <ReceiptIcon className='size-3' /> {sale.employeeName ?? '—'}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Estatus
          </CardTitle>
        </CardHeader>
        <CardContent className='space-y-2'>
          <div className='flex items-center gap-2'>
            <Badge variant={statusVariant(sale.saleStatusCode)}>
              {sale.saleStatusName ?? '—'}
            </Badge>
            <Badge variant='outline'>{sale.saleTypeName ?? '—'}</Badge>
          </div>
          <p className='text-muted-foreground text-xs flex items-center gap-2'>
            <CalendarIcon className='size-3' />
            {formatDate(sale.saleDate)}
          </p>
          {sale.folio && (
            <p className='text-muted-foreground text-xs flex items-center gap-2'>
              <HashIcon className='size-3' /> {sale.folio}
            </p>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

// ── Lines tab ─────────────────────────────────────────────

function LinesTab({ sale }: { sale: SaleDetailResponse }) {
  if (sale.lines.length === 0) {
    return (
      <p className='text-muted-foreground py-10 text-center text-sm'>
        Esta venta no tiene líneas registradas.
      </p>
    )
  }
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className='w-12'>#</TableHead>
          <TableHead>Tipo</TableHead>
          <TableHead>Descripción</TableHead>
          <TableHead className='text-right'>Cant.</TableHead>
          <TableHead className='text-right'>P. Unit.</TableHead>
          <TableHead className='text-right'>Desc.</TableHead>
          <TableHead className='text-right'>Imp.</TableHead>
          <TableHead className='text-right'>Total</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sale.lines.map((line) => (
          <TableRow key={line.saleLineId}>
            <TableCell className='text-muted-foreground'>
              {line.lineNumber}
            </TableCell>
            <TableCell>
              <Badge variant={lineTypeVariant(line.lineType)}>
                {lineTypeLabel(line.lineType)}
              </Badge>
            </TableCell>
            <TableCell>
              <div className='flex flex-col'>
                <span className='font-medium'>{line.descriptionSnapshot}</span>
                {line.notes && (
                  <span className='text-muted-foreground text-xs'>
                    {line.notes}
                  </span>
                )}
                {line.serialInventoryItemIds.length > 0 && (
                  <span className='text-muted-foreground text-xs'>
                    Series: {line.serialInventoryItemIds.join(', ')}
                  </span>
                )}
              </div>
            </TableCell>
            <TableCell className='text-right'>{line.quantity}</TableCell>
            <TableCell className='text-right'>
              {formatCurrency(line.unitPrice)}
            </TableCell>
            <TableCell className='text-right'>
              {line.discountAmount > 0
                ? formatCurrency(line.discountAmount)
                : '—'}
            </TableCell>
            <TableCell className='text-right'>
              {line.taxAmount > 0 ? formatCurrency(line.taxAmount) : '—'}
            </TableCell>
            <TableCell className='text-right font-medium'>
              {formatCurrency(line.lineTotal)}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

// ── Payments tab ──────────────────────────────────────────

function PaymentsTab({ sale }: { sale: SaleDetailResponse }) {
  if (sale.payments.length === 0) {
    return (
      <p className='text-muted-foreground py-10 text-center text-sm'>
        Esta venta no tiene pagos registrados.
      </p>
    )
  }
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Fecha</TableHead>
          <TableHead>Método</TableHead>
          <TableHead>Referencia</TableHead>
          <TableHead>Comentarios</TableHead>
          <TableHead className='text-right'>Monto</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sale.payments.map((p) => (
          <TableRow key={p.salePaymentId}>
            <TableCell>{formatDateTime(p.paymentDate)}</TableCell>
            <TableCell>
              <Badge variant='outline'>
                {p.paymentMethodName ?? p.paymentMethodCode ?? '—'}
              </Badge>
            </TableCell>
            <TableCell className='text-muted-foreground'>
              {p.reference ?? '—'}
            </TableCell>
            <TableCell className='text-muted-foreground'>
              {p.comments ?? '—'}
            </TableCell>
            <TableCell className='text-right font-medium text-green-600'>
              {formatCurrency(p.amount)}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

// ── Info tab ──────────────────────────────────────────────

function InfoRow({
  label,
  value,
}: {
  label: string
  value: React.ReactNode
}) {
  return (
    <div className='flex items-start justify-between gap-4 border-b py-2 last:border-b-0'>
      <span className='text-muted-foreground text-sm'>{label}</span>
      <span className='text-sm font-medium text-right'>{value}</span>
    </div>
  )
}

function InfoTab({ sale }: { sale: SaleDetailResponse }) {
  return (
    <div className='grid gap-6 md:grid-cols-2'>
      <Card>
        <CardHeader>
          <CardTitle className='text-base flex items-center gap-2'>
            <InfoIcon className='size-4' />
            General
          </CardTitle>
        </CardHeader>
        <CardContent className='space-y-1'>
          <InfoRow label='ID' value={`#${sale.saleId}`} />
          <InfoRow label='Folio' value={sale.folio ?? '—'} />
          <InfoRow
            label='Referencia externa'
            value={sale.externalReference ?? '—'}
          />
          <InfoRow
            label='ID legado'
            value={sale.legacySaleId ?? '—'}
          />
          <InfoRow
            label='Cuenta'
            value={
              sale.accountId ? (
                <Link
                  to='/accounts/$accountId'
                  params={{ accountId: String(sale.accountId) }}
                  className='text-primary hover:underline'
                >
                  #{sale.accountId}
                </Link>
              ) : (
                '—'
              )
            }
          />
          <InfoRow label='Creada' value={formatDateTime(sale.createdAt)} />
          <InfoRow
            label='Actualizada'
            value={formatDateTime(sale.updatedAt)}
          />
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className='text-base flex items-center gap-2'>
            <BanknoteIcon className='size-4' />
            Totales
          </CardTitle>
        </CardHeader>
        <CardContent className='space-y-1'>
          <InfoRow label='Subtotal' value={formatCurrency(sale.subtotal)} />
          <InfoRow
            label='Descuento'
            value={formatCurrency(sale.discountTotal)}
          />
          <InfoRow label='Impuestos' value={formatCurrency(sale.taxTotal)} />
          <InfoRow label='Total' value={formatCurrency(sale.total)} />
          <InfoRow label='Pagado' value={formatCurrency(sale.paidTotal)} />
          <InfoRow
            label='Saldo'
            value={
              <span
                className={
                  sale.balance > 0 ? 'text-destructive' : 'text-green-600'
                }
              >
                {formatCurrency(sale.balance)}
              </span>
            }
          />
        </CardContent>
      </Card>

      {sale.notes && (
        <Card className='md:col-span-2'>
          <CardHeader>
            <CardTitle className='text-base'>Notas</CardTitle>
          </CardHeader>
          <CardContent>
            <p className='text-sm whitespace-pre-wrap'>{sale.notes}</p>
          </CardContent>
        </Card>
      )}
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────

const SaleDetailPage = () => {
  const { saleId } = useParams({ from: '/sales/$saleId' })
  const router = useRouter()
  const id = Number(saleId)

  const { data: sale, isLoading } = useQuery({
    queryKey: ['sale', id],
    queryFn: () => getSale(id),
    enabled: !Number.isNaN(id),
  })

  return (
    <DashboardLayout>
      <div className='flex flex-col gap-6 p-6'>
        <div className='flex items-center gap-3'>
          <Button
            variant='ghost'
            size='icon'
            onClick={() => router.history.back()}
          >
            <ArrowLeftIcon className='size-4' />
          </Button>
          <div>
            <h1 className='text-2xl font-bold tracking-tight'>
              {sale?.folio
                ? `Venta ${sale.folio}`
                : `Venta #${id}`}
            </h1>
            <p className='text-muted-foreground text-sm'>
              Detalle e historial de la venta
            </p>
          </div>
        </div>

        {isLoading || !sale ? (
          <div className='grid gap-4 md:grid-cols-2 lg:grid-cols-4'>
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className='h-32 rounded-xl' />
            ))}
          </div>
        ) : (
          <>
            <SaleHeader sale={sale} />

            <Tabs defaultValue='lines'>
              <TabsList>
                <TabsTrigger value='lines'>
                  <PackageIcon className='size-4' />
                  Líneas ({sale.lines.length})
                </TabsTrigger>
                <TabsTrigger value='payments'>
                  <BanknoteIcon className='size-4' />
                  Pagos ({sale.payments.length})
                </TabsTrigger>
                <TabsTrigger value='info'>
                  <InfoIcon className='size-4' />
                  Información
                </TabsTrigger>
              </TabsList>
              <TabsContent value='lines'>
                <Card>
                  <CardContent className='p-0'>
                    <LinesTab sale={sale} />
                  </CardContent>
                </Card>
              </TabsContent>
              <TabsContent value='payments'>
                <Card>
                  <CardContent className='p-0'>
                    <PaymentsTab sale={sale} />
                  </CardContent>
                </Card>
              </TabsContent>
              <TabsContent value='info'>
                <InfoTab sale={sale} />
              </TabsContent>
            </Tabs>
          </>
        )}
      </div>
    </DashboardLayout>
  )
}

export default SaleDetailPage
