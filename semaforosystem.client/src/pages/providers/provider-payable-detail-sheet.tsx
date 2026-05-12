import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  EditIcon,
  Trash2Icon,
  AlertTriangleIcon,
  XCircleIcon,
  CheckCircle2Icon,
} from 'lucide-react'

import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'

import {
  deleteProviderPayable,
  deleteProviderPayableTransaction,
  getProviderPayable,
  type ProviderPayableTransactionResponse,
} from '@/services/provider-payable-service'
import { ApiError } from '@/lib/api-client'

import ProviderPayableFormDialog from './provider-payable-form-dialog'
import ProviderPayableTransactionDialog from './provider-payable-transaction-dialog'

interface ProviderPayableDetailSheetProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  payableId: number | null
  providerId: number
}

const TYPE_LABELS: Record<string, string> = {
  CHARGE: 'Cargo',
  PAYMENT: 'Pago',
  DISCOUNT: 'Descuento',
  INTEREST: 'Interés',
  ADJUSTMENT: 'Ajuste',
  CANCELLATION: 'Cancelación',
  CREDIT_NOTE_APPLIED: 'Nota de crédito',
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

export default function ProviderPayableDetailSheet({
  open,
  onOpenChange,
  payableId,
  providerId,
}: ProviderPayableDetailSheetProps) {
  const qc = useQueryClient()

  const { data, isLoading } = useQuery({
    queryKey: ['provider-payable', payableId],
    queryFn: () => getProviderPayable(payableId!),
    enabled: open && payableId !== null,
  })

  const [editOpen, setEditOpen] = useState(false)
  const [txOpen, setTxOpen] = useState(false)
  const [txDefaultType, setTxDefaultType] = useState<string>('PAYMENT')

  const [deletePayableOpen, setDeletePayableOpen] = useState(false)
  const [txToDelete, setTxToDelete] = useState<ProviderPayableTransactionResponse | null>(
    null,
  )

  const deletePayableMutation = useMutation({
    mutationFn: () => deleteProviderPayable(payableId!),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-payables', providerId] })
      toast.success('Cuenta eliminada')
      setDeletePayableOpen(false)
      onOpenChange(false)
    },
    onError: (err: Error) => {
      if (err instanceof ApiError) {
        const body = err.body as { message?: string }
        toast.error(body?.message ?? 'No se pudo eliminar.')
      } else toast.error('Error al eliminar')
    },
  })

  const deleteTxMutation = useMutation({
    mutationFn: () =>
      deleteProviderPayableTransaction(
        payableId!,
        txToDelete!.providerPayableTransactionId,
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-payable', payableId] })
      qc.invalidateQueries({ queryKey: ['provider-payables', providerId] })
      toast.success('Movimiento eliminado')
      setTxToDelete(null)
    },
    onError: (err: Error) => {
      if (err instanceof ApiError) {
        const body = err.body as { message?: string }
        toast.error(body?.message ?? 'No se pudo eliminar el movimiento.')
      } else toast.error('Error al eliminar el movimiento')
    },
  })

  const isTerminal =
    data && ['PAID', 'CANCELED', 'CLOSED'].includes(data.statusCode)

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className='w-full sm:max-w-3xl overflow-y-auto'>
        <SheetHeader>
          <SheetTitle>Detalle de cuenta por pagar</SheetTitle>
          <SheetDescription>
            Movimientos, líneas, balance derivado del ledger.
          </SheetDescription>
        </SheetHeader>

        <div className='p-4 space-y-5'>
          {isLoading || !data ? (
            <div className='space-y-3'>
              <Skeleton className='h-8 w-1/2' />
              <Skeleton className='h-32 w-full' />
              <Skeleton className='h-64 w-full' />
            </div>
          ) : (
            <>
              {/* Header summary */}
              <div className='flex items-start justify-between gap-3 flex-wrap'>
                <div className='min-w-0'>
                  <div className='flex items-center gap-2 flex-wrap'>
                    <h2 className='text-lg font-semibold truncate'>
                      {data.documentNumber ?? `#${data.providerPayableId}`}
                    </h2>
                    <Badge variant={statusVariant(data.statusCode)}>
                      {data.statusName}
                    </Badge>
                    {data.isOverdue && (
                      <Badge
                        variant='outline'
                        className='gap-1 border-destructive text-destructive'
                      >
                        <AlertTriangleIcon className='size-3' />
                        Vencida
                      </Badge>
                    )}
                  </div>
                  <p className='text-muted-foreground text-sm'>
                    {data.typeName} · {formatDate(data.documentDate)}
                    {data.dueDate && ` · vence ${formatDate(data.dueDate)}`}
                  </p>
                </div>
                <div className='flex gap-2'>
                  <Button
                    variant='outline'
                    size='sm'
                    onClick={() => setEditOpen(true)}
                    disabled={isTerminal}
                  >
                    <EditIcon className='size-4' />
                    Editar
                  </Button>
                  <Button
                    variant='outline'
                    size='sm'
                    className='text-destructive'
                    onClick={() => setDeletePayableOpen(true)}
                  >
                    <Trash2Icon className='size-4' />
                    Eliminar
                  </Button>
                </div>
              </div>

              {/* Balance card */}
              <Card>
                <CardContent className='p-4 grid grid-cols-2 sm:grid-cols-4 gap-4'>
                  <Metric
                    label='Total'
                    value={formatMoney(data.total, data.currencyCode)}
                  />
                  <Metric
                    label='Cargado'
                    value={formatMoney(data.chargedAmount, data.currencyCode)}
                  />
                  <Metric
                    label='Pagado'
                    value={formatMoney(data.paidAmount, data.currencyCode)}
                    className='text-green-600'
                  />
                  <Metric
                    label='Saldo'
                    value={formatMoney(data.balance, data.currencyCode)}
                    className={
                      data.balance <= 0
                        ? 'text-green-600'
                        : data.isOverdue
                          ? 'text-destructive'
                          : ''
                    }
                  />
                </CardContent>
              </Card>

              {/* Quick actions */}
              <div className='flex flex-wrap gap-2'>
                <Button
                  size='sm'
                  onClick={() => {
                    setTxDefaultType('PAYMENT')
                    setTxOpen(true)
                  }}
                  disabled={isTerminal}
                  className='gap-2'
                >
                  <CheckCircle2Icon className='size-4' />
                  Registrar pago
                </Button>
                <Button
                  size='sm'
                  variant='outline'
                  onClick={() => {
                    setTxDefaultType('DISCOUNT')
                    setTxOpen(true)
                  }}
                  disabled={isTerminal}
                >
                  Descuento
                </Button>
                <Button
                  size='sm'
                  variant='outline'
                  onClick={() => {
                    setTxDefaultType('INTEREST')
                    setTxOpen(true)
                  }}
                  disabled={isTerminal}
                >
                  Interés
                </Button>
                <Button
                  size='sm'
                  variant='outline'
                  onClick={() => {
                    setTxDefaultType('ADJUSTMENT')
                    setTxOpen(true)
                  }}
                  disabled={isTerminal}
                >
                  Ajuste
                </Button>
                <Button
                  size='sm'
                  variant='outline'
                  className='text-destructive'
                  onClick={() => {
                    setTxDefaultType('CANCELLATION')
                    setTxOpen(true)
                  }}
                  disabled={isTerminal}
                >
                  <XCircleIcon className='size-4' />
                  Cancelar
                </Button>
              </div>

              <Separator />

              {/* Tabs */}
              <Tabs defaultValue='ledger' className='space-y-3'>
                <TabsList>
                  <TabsTrigger value='ledger'>
                    Movimientos ({data.transactions.length})
                  </TabsTrigger>
                  <TabsTrigger value='lines'>
                    Líneas ({data.lines.length})
                  </TabsTrigger>
                  <TabsTrigger value='info'>Información</TabsTrigger>
                </TabsList>

                <TabsContent value='ledger'>
                  {data.transactions.length === 0 ? (
                    <p className='text-muted-foreground text-center text-sm py-8'>
                      Sin movimientos registrados.
                    </p>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead>Tipo</TableHead>
                          <TableHead>Fecha</TableHead>
                          <TableHead>Método</TableHead>
                          <TableHead>Referencia</TableHead>
                          <TableHead>Empleado</TableHead>
                          <TableHead className='text-right'>Monto</TableHead>
                          <TableHead className='w-10' />
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {data.transactions.map((tx) => {
                          const isPayLike = [
                            'PAYMENT',
                            'DISCOUNT',
                            'CREDIT_NOTE_APPLIED',
                            'CANCELLATION',
                          ].includes(tx.transactionType)
                          return (
                            <TableRow key={tx.providerPayableTransactionId}>
                              <TableCell>
                                <Badge variant={isPayLike ? 'secondary' : 'default'}>
                                  {TYPE_LABELS[tx.transactionType] ?? tx.transactionType}
                                </Badge>
                              </TableCell>
                              <TableCell>{formatDate(tx.transactionDate)}</TableCell>
                              <TableCell>
                                {tx.providerPaymentMethodName ?? '—'}
                              </TableCell>
                              <TableCell className='text-muted-foreground text-sm'>
                                {tx.reference ?? '—'}
                              </TableCell>
                              <TableCell className='text-sm'>
                                {tx.createdByEmployeeName ?? `#${tx.createdByEmployeeId}`}
                              </TableCell>
                              <TableCell
                                className={`text-right font-medium ${
                                  isPayLike ? 'text-green-600' : ''
                                }`}
                              >
                                {isPayLike ? '-' : ''}
                                {formatMoney(tx.amount, data.currencyCode)}
                              </TableCell>
                              <TableCell>
                                <Button
                                  variant='ghost'
                                  size='icon'
                                  className='text-destructive size-7'
                                  onClick={() => setTxToDelete(tx)}
                                  disabled={isTerminal}
                                >
                                  <Trash2Icon className='size-3.5' />
                                </Button>
                              </TableCell>
                            </TableRow>
                          )
                        })}
                      </TableBody>
                    </Table>
                  )}
                </TabsContent>

                <TabsContent value='lines'>
                  {data.lines.length === 0 ? (
                    <div className='text-center py-8'>
                      <p className='text-muted-foreground text-sm mb-2'>
                        Sin líneas registradas.
                      </p>
                      <p className='text-muted-foreground text-xs'>
                        Las líneas se capturan al crear la cuenta. Para añadir más usa el
                        endpoint de líneas.
                      </p>
                    </div>
                  ) : (
                    <Table>
                      <TableHeader>
                        <TableRow>
                          <TableHead className='w-12'>#</TableHead>
                          <TableHead>Descripción</TableHead>
                          <TableHead className='text-right'>Cant.</TableHead>
                          <TableHead className='text-right'>Costo unit.</TableHead>
                          <TableHead className='text-right'>Desc.</TableHead>
                          <TableHead className='text-right'>Imp.</TableHead>
                          <TableHead className='text-right'>Total</TableHead>
                        </TableRow>
                      </TableHeader>
                      <TableBody>
                        {data.lines.map((l) => (
                          <TableRow key={l.providerPayableLineId}>
                            <TableCell>{l.lineNumber}</TableCell>
                            <TableCell>{l.descriptionSnapshot}</TableCell>
                            <TableCell className='text-right'>
                              {l.quantity.toFixed(2)}
                            </TableCell>
                            <TableCell className='text-right'>
                              {l.unitCost.toFixed(2)}
                            </TableCell>
                            <TableCell className='text-right'>
                              {l.discountAmount.toFixed(2)}
                            </TableCell>
                            <TableCell className='text-right'>
                              {l.taxAmount.toFixed(2)}
                            </TableCell>
                            <TableCell className='text-right font-medium'>
                              {l.lineTotal.toFixed(2)}
                            </TableCell>
                          </TableRow>
                        ))}
                      </TableBody>
                    </Table>
                  )}
                </TabsContent>

                <TabsContent value='info'>
                  <div className='grid grid-cols-2 gap-4 text-sm'>
                    <Info label='Sitio' value={data.siteName} />
                    <Info
                      label='Abierta por'
                      value={
                        data.openedByEmployeeName ?? `#${data.openedByEmployeeId}`
                      }
                    />
                    <Info label='Referencia' value={data.reference ?? '—'} />
                    <Info label='Moneda' value={data.currencyCode} />
                    <Info
                      label='Orden de compra'
                      value={data.purchaseOrderId ? `#${data.purchaseOrderId}` : '—'}
                    />
                    <Info
                      label='Recibo de compra'
                      value={
                        data.purchaseReceiptId ? `#${data.purchaseReceiptId}` : '—'
                      }
                    />
                    <Info label='Creada' value={formatDate(data.createdAt)} />
                    <Info label='Actualizada' value={formatDate(data.updatedAt)} />
                  </div>
                  {data.notes && (
                    <>
                      <Separator className='my-4' />
                      <p className='text-muted-foreground text-xs font-medium uppercase'>
                        Notas
                      </p>
                      <p className='text-sm whitespace-pre-line mt-1'>{data.notes}</p>
                    </>
                  )}
                </TabsContent>
              </Tabs>
            </>
          )}
        </div>
      </SheetContent>

      {/* Edit dialog */}
      {data && (
        <ProviderPayableFormDialog
          open={editOpen}
          onOpenChange={setEditOpen}
          providerId={providerId}
          payable={data}
        />
      )}

      {/* Transaction dialog */}
      {data && payableId !== null && (
        <ProviderPayableTransactionDialog
          open={txOpen}
          onOpenChange={setTxOpen}
          payableId={payableId}
          providerId={providerId}
          defaultType={txDefaultType}
          suggestedAmount={txDefaultType === 'PAYMENT' ? data.balance : undefined}
          currencyCode={data.currencyCode}
        />
      )}

      {/* Delete payable */}
      <AlertDialog open={deletePayableOpen} onOpenChange={setDeletePayableOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Eliminar cuenta por pagar?</AlertDialogTitle>
            <AlertDialogDescription>
              Esta acción no se puede deshacer. Se eliminarán también las líneas y
              movimientos asociados.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deletePayableMutation.isPending}>
              Cancelar
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={(e) => {
                e.preventDefault()
                deletePayableMutation.mutate()
              }}
              disabled={deletePayableMutation.isPending}
              className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
            >
              {deletePayableMutation.isPending ? 'Eliminando...' : 'Eliminar'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Delete transaction */}
      <AlertDialog
        open={txToDelete !== null}
        onOpenChange={(o) => !o && setTxToDelete(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Eliminar movimiento?</AlertDialogTitle>
            <AlertDialogDescription>
              El balance se recalculará automáticamente.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteTxMutation.isPending}>
              Cancelar
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={(e) => {
                e.preventDefault()
                deleteTxMutation.mutate()
              }}
              disabled={deleteTxMutation.isPending}
              className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
            >
              {deleteTxMutation.isPending ? 'Eliminando...' : 'Eliminar'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </Sheet>
  )
}

function Metric({
  label,
  value,
  className = '',
}: {
  label: string
  value: string
  className?: string
}) {
  return (
    <div>
      <p className='text-muted-foreground text-xs font-medium uppercase'>{label}</p>
      <p className={`text-base font-semibold mt-1 ${className}`}>{value}</p>
    </div>
  )
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className='text-muted-foreground text-xs font-medium uppercase'>{label}</p>
      <p className='mt-1'>{value}</p>
    </div>
  )
}
