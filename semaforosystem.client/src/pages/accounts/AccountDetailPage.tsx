import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useRouter } from '@tanstack/react-router'
import {
  ArrowLeftIcon,
  BanknoteIcon,
  CalendarIcon,
  PackageIcon,
  ReceiptIcon,
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
import { toast } from 'sonner'

import {
  getAccount,
  deliverLayaway,
  markLayawayReady,
} from '@/services/account-service'
import type { AccountDetailResponse } from '@/services/account-service'

// ── Helpers ───────────────────────────────────────────────

function formatCurrency(amount: number, code = 'MXN') {
  return new Intl.NumberFormat('es-MX', {
    style: 'currency',
    currency: code,
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(date: string | null | undefined) {
  if (!date) return '—'
  return new Date(date).toLocaleDateString('es-MX')
}

function statusVariant(
  code: string | null,
): 'default' | 'secondary' | 'destructive' | 'outline' {
  switch (code?.toUpperCase()) {
    case 'OPEN':
      return 'default'
    case 'PAID':
      return 'secondary'
    case 'CANCELED':
    case 'OVERDUE':
      return 'destructive'
    default:
      return 'outline'
  }
}

function txTypeLabel(type: string) {
  const labels: Record<string, string> = {
    CHARGE: 'Cargo',
    PAYMENT: 'Pago',
    DISCOUNT: 'Descuento',
    INTEREST: 'Interés',
    ADJUSTMENT: 'Ajuste',
    CANCELLATION: 'Cancelación',
  }
  return labels[type] ?? type
}

// ── Header section ────────────────────────────────────────

function AccountHeader({ account }: { account: AccountDetailResponse }) {
  const qc = useQueryClient()
  const isLayaway = account.accountTypeCode === 'LAYAWAY'

  const readyMutation = useMutation({
    mutationFn: () => markLayawayReady(account.accountId),
    onSuccess: () => {
      toast.success('Apartado marcado como listo.')
      qc.invalidateQueries({ queryKey: ['account', account.accountId] })
    },
    onError: () => toast.error('No se pudo actualizar el apartado.'),
  })

  const deliverMutation = useMutation({
    mutationFn: () => deliverLayaway(account.accountId),
    onSuccess: () => {
      toast.success('Apartado marcado como entregado.')
      qc.invalidateQueries({ queryKey: ['account', account.accountId] })
    },
    onError: () => toast.error('No se pudo actualizar el apartado.'),
  })

  return (
    <div className='grid gap-4 md:grid-cols-2 lg:grid-cols-4'>
      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Saldo actual
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p
            className={`text-2xl font-bold ${account.balance > 0 ? 'text-destructive' : 'text-green-600'}`}
          >
            {formatCurrency(account.balance, account.currencyCode)}
          </p>
          <p className='text-muted-foreground text-xs mt-1'>
            Cargo: {formatCurrency(account.totalCharged, account.currencyCode)} ·
            Pagado: {formatCurrency(account.totalPaid, account.currencyCode)}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Cliente
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className='text-lg font-semibold'>
            {account.clientName ?? `Cliente #${account.clientId}`}
          </p>
          <p className='text-muted-foreground text-xs'>
            Sucursal: {account.siteName ?? account.siteId}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Fechas
          </CardTitle>
        </CardHeader>
        <CardContent>
          <p className='text-sm'>
            <span className='text-muted-foreground'>Apertura: </span>
            {formatDate(account.openingDate)}
          </p>
          <p className='text-sm'>
            <span className='text-muted-foreground'>Vencimiento: </span>
            {formatDate(account.dueDate)}
          </p>
        </CardContent>
      </Card>

      <Card>
        <CardHeader className='pb-2'>
          <CardTitle className='text-muted-foreground text-sm font-medium'>
            Estatus
          </CardTitle>
        </CardHeader>
        <CardContent className='flex flex-col gap-2'>
          <div className='flex gap-2'>
            <Badge variant={statusVariant(account.accountStatusCode)}>
              {account.accountStatusName}
            </Badge>
            <Badge variant='outline'>{account.accountTypeName}</Badge>
          </div>
          {isLayaway && account.accountStatusCode !== 'CANCELED' && (
            <div className='flex gap-2 flex-wrap'>
              {!account.layaway?.isReady && (
                <Button
                  size='sm'
                  variant='secondary'
                  disabled={readyMutation.isPending}
                  onClick={() => readyMutation.mutate()}
                >
                  Marcar listo
                </Button>
              )}
              {account.layaway?.isReady && !account.layaway.deliveryDate && (
                <Button
                  size='sm'
                  disabled={deliverMutation.isPending}
                  onClick={() => deliverMutation.mutate()}
                >
                  Marcar entregado
                </Button>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}

// ── Items tab ─────────────────────────────────────────────

function ItemsTab({ account }: { account: AccountDetailResponse }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Descripción</TableHead>
          <TableHead className='text-right'>Cant.</TableHead>
          <TableHead className='text-right'>Precio unit.</TableHead>
          <TableHead className='text-right'>Descuento</TableHead>
          <TableHead className='text-right'>Total línea</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {account.items.length === 0 ? (
          <TableRow>
            <TableCell
              colSpan={5}
              className='text-muted-foreground text-center py-8'
            >
              Sin artículos registrados.
            </TableCell>
          </TableRow>
        ) : (
          account.items.map((item) => (
            <TableRow key={item.accountItemId}>
              <TableCell>
                <span className='font-medium'>{item.descriptionSnapshot}</span>
              </TableCell>
              <TableCell className='text-right'>{item.quantity}</TableCell>
              <TableCell className='text-right'>
                {formatCurrency(item.unitPrice, account.currencyCode)}
              </TableCell>
              <TableCell className='text-right'>
                {item.discountAmount > 0
                  ? formatCurrency(item.discountAmount, account.currencyCode)
                  : '—'}
              </TableCell>
              <TableCell className='text-right font-medium'>
                {formatCurrency(item.lineTotal, account.currencyCode)}
              </TableCell>
            </TableRow>
          ))
        )}
        {account.items.length > 0 && (
          <TableRow>
            <TableCell colSpan={4} className='text-right font-semibold'>
              Total artículos
            </TableCell>
            <TableCell className='text-right font-semibold'>
              {formatCurrency(
                account.items.reduce((s, i) => s + i.lineTotal, 0),
                account.currencyCode,
              )}
            </TableCell>
          </TableRow>
        )}
      </TableBody>
    </Table>
  )
}

// ── Transactions tab ──────────────────────────────────────

function TransactionsTab({ account }: { account: AccountDetailResponse }) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Tipo</TableHead>
          <TableHead>Fecha</TableHead>
          <TableHead>Método pago</TableHead>
          <TableHead>Referencia</TableHead>
          <TableHead>Empleado</TableHead>
          <TableHead className='text-right'>Monto</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {account.transactions.length === 0 ? (
          <TableRow>
            <TableCell
              colSpan={6}
              className='text-muted-foreground text-center py-8'
            >
              Sin movimientos registrados.
            </TableCell>
          </TableRow>
        ) : (
          account.transactions.map((tx) => (
            <TableRow key={tx.accountTransactionId}>
              <TableCell>
                <Badge
                  variant={tx.amount < 0 ? 'secondary' : 'default'}
                >
                  {txTypeLabel(tx.transactionType)}
                </Badge>
              </TableCell>
              <TableCell>{formatDate(tx.transactionDate)}</TableCell>
              <TableCell>{tx.paymentMethod ?? '—'}</TableCell>
              <TableCell className='text-muted-foreground text-sm'>
                {tx.reference ?? '—'}
              </TableCell>
              <TableCell className='text-sm'>
                {tx.createdByEmployeeName ?? `#${tx.createdByEmployeeId}`}
              </TableCell>
              <TableCell
                className={`text-right font-medium ${tx.amount < 0 ? 'text-green-600' : ''}`}
              >
                {formatCurrency(tx.amount, account.currencyCode)}
              </TableCell>
            </TableRow>
          ))
        )}
      </TableBody>
    </Table>
  )
}

// ── Installments tab ──────────────────────────────────────

function InstallmentsTab({ account }: { account: AccountDetailResponse }) {
  if (account.installments.length === 0) {
    return (
      <p className='text-muted-foreground py-8 text-center text-sm'>
        Esta cuenta no tiene plan de mensualidades.
      </p>
    )
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>#</TableHead>
          <TableHead>Vencimiento</TableHead>
          <TableHead className='text-right'>Esperado</TableHead>
          <TableHead className='text-right'>Pagado</TableHead>
          <TableHead>Estatus</TableHead>
          <TableHead>F. Pago</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {account.installments.map((inst) => (
          <TableRow key={inst.accountInstallmentId}>
            <TableCell>{inst.installmentNumber}</TableCell>
            <TableCell>{inst.dueDate}</TableCell>
            <TableCell className='text-right'>
              {formatCurrency(inst.expectedAmount, account.currencyCode)}
            </TableCell>
            <TableCell className='text-right'>
              {formatCurrency(inst.paidAmount, account.currencyCode)}
            </TableCell>
            <TableCell>
              <Badge variant={inst.isPaid ? 'secondary' : 'outline'}>
                {inst.isPaid ? 'Pagada' : 'Pendiente'}
              </Badge>
            </TableCell>
            <TableCell>{formatDate(inst.paidDate)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

// ── Type-specific section ─────────────────────────────────

function TypeSpecificTab({ account }: { account: AccountDetailResponse }) {
  if (account.credit) {
    const c = account.credit
    return (
      <div className='grid gap-3 sm:grid-cols-2 lg:grid-cols-3'>
        <InfoField label='Días de crédito' value={c.creditDays ?? '—'} />
        <InfoField label='Gracia hasta' value={formatDate(c.graceUntil)} />
        <InfoField
          label='Fecha venc. original'
          value={formatDate(c.originalDueDate)}
        />
        <InfoField
          label='Fecha venc. extendida'
          value={formatDate(c.extendedDueDate)}
        />
        <InfoField
          label='Límite de crédito'
          value={
            c.creditLimitSnapshot != null
              ? formatCurrency(c.creditLimitSnapshot, account.currencyCode)
              : '—'
          }
        />
        <InfoField
          label='Requiere aval'
          value={c.requiresGuarantor ? 'Sí' : 'No'}
        />
      </div>
    )
  }

  if (account.layaway) {
    const l = account.layaway
    return (
      <div className='grid gap-3 sm:grid-cols-2 lg:grid-cols-3'>
        <InfoField
          label='Depósito inicial'
          value={formatCurrency(l.depositAmount, account.currencyCode)}
        />
        <InfoField
          label='Fecha esperada llegada'
          value={formatDate(l.expectedArrivalDate)}
        />
        <InfoField label='Fecha listo' value={formatDate(l.readyDate)} />
        <InfoField label='Fecha entrega' value={formatDate(l.deliveryDate)} />
        <InfoField label='Fecha vencimiento' value={formatDate(l.expirationDate)} />
        <InfoField label='¿Listo?' value={l.isReady ? 'Sí' : 'No'} />
        {l.cancellationPolicyNotes && (
          <div className='sm:col-span-2 lg:col-span-3'>
            <InfoField
              label='Política de cancelación'
              value={l.cancellationPolicyNotes}
            />
          </div>
        )}
      </div>
    )
  }

  return (
    <p className='text-muted-foreground text-sm py-4'>
      Sin datos de tipo específico.
    </p>
  )
}

function InfoField({
  label,
  value,
}: {
  label: string
  value: string | number
}) {
  return (
    <div className='flex flex-col gap-1'>
      <span className='text-muted-foreground text-xs'>{label}</span>
      <span className='text-sm font-medium'>{value}</span>
    </div>
  )
}

// ── Main page ─────────────────────────────────────────────

const AccountDetailPage = () => {
  const { accountId } = useParams({ from: '/accounts/$accountId' })
  const router = useRouter()
  const id = Number(accountId)

  const { data: account, isLoading, isError } = useQuery({
    queryKey: ['account', id],
    queryFn: () => getAccount(id),
    enabled: !isNaN(id),
  })

  if (isLoading) {
    return (
      <DashboardLayout>
        <div className='flex flex-col gap-4 p-6'>
          <Skeleton className='h-8 w-48' />
          <div className='grid gap-4 md:grid-cols-4'>
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className='h-28 rounded-xl' />
            ))}
          </div>
          <Skeleton className='h-64 rounded-xl' />
        </div>
      </DashboardLayout>
    )
  }

  if (isError || !account) {
    return (
      <DashboardLayout>
        <div className='flex flex-col items-center gap-4 p-12'>
          <WalletIcon className='text-muted-foreground size-12' />
          <p className='text-muted-foreground'>Cuenta no encontrada.</p>
          <Button variant='outline' onClick={() => router.history.back()}>
            <ArrowLeftIcon className='size-4 mr-2' />
            Regresar
          </Button>
        </div>
      </DashboardLayout>
    )
  }

  const tabsConfig = [
    {
      value: 'items',
      label: 'Artículos',
      icon: PackageIcon,
      content: <ItemsTab account={account} />,
    },
    {
      value: 'transactions',
      label: 'Movimientos',
      icon: ReceiptIcon,
      content: <TransactionsTab account={account} />,
    },
    {
      value: 'installments',
      label: 'Mensualidades',
      icon: CalendarIcon,
      content: <InstallmentsTab account={account} />,
    },
    {
      value: 'type',
      label: account.accountTypeCode === 'LAYAWAY' ? 'Apartado' : 'Crédito',
      icon: BanknoteIcon,
      content: <TypeSpecificTab account={account} />,
    },
  ]

  return (
    <DashboardLayout>
      <div className='flex flex-col gap-6 p-6'>
        {/* Header */}
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
              Cuenta #{account.accountId}
            </h1>
            {account.reference && (
              <p className='text-muted-foreground text-sm'>
                Ref: {account.reference}
              </p>
            )}
          </div>
        </div>

        {/* Summary cards */}
        <AccountHeader account={account} />

        {/* Notes */}
        {account.notes && (
          <Card>
            <CardHeader className='pb-2'>
              <CardTitle className='text-sm'>Observaciones</CardTitle>
            </CardHeader>
            <CardContent>
              <p className='text-muted-foreground text-sm whitespace-pre-wrap'>
                {account.notes}
              </p>
            </CardContent>
          </Card>
        )}

        {/* Tabs */}
        <Tabs defaultValue='items'>
          <TabsList>
            {tabsConfig.map((t) => (
              <TabsTrigger key={t.value} value={t.value} className='gap-1.5'>
                <t.icon className='size-4' />
                {t.label}
              </TabsTrigger>
            ))}
          </TabsList>
          {tabsConfig.map((t) => (
            <TabsContent key={t.value} value={t.value}>
              <Card>
                <CardContent className='p-0'>{t.content}</CardContent>
              </Card>
            </TabsContent>
          ))}
        </Tabs>
      </div>
    </DashboardLayout>
  )
}

export default AccountDetailPage
