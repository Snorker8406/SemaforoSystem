import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

import {
  addProviderPayableTransaction,
  getPayablePaymentMethods,
  getPayableTransactionTypes,
  type CreateProviderPayableTransactionRequest,
} from '@/services/provider-payable-service'
import { ApiError } from '@/lib/api-client'

interface ProviderPayableTransactionDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  payableId: number
  providerId: number
  defaultType?: string
  suggestedAmount?: number
  currencyCode?: string
}

const TYPE_LABELS: Record<string, string> = {
  CHARGE: 'Cargo',
  PAYMENT: 'Pago',
  DISCOUNT: 'Descuento',
  INTEREST: 'Interés',
  ADJUSTMENT: 'Ajuste',
  CANCELLATION: 'Cancelación',
  CREDIT_NOTE_APPLIED: 'Nota de crédito aplicada',
}

const today = () => new Date().toISOString().slice(0, 10)

export default function ProviderPayableTransactionDialog({
  open,
  onOpenChange,
  payableId,
  providerId,
  defaultType = 'PAYMENT',
  suggestedAmount,
  currencyCode = 'MXN',
}: ProviderPayableTransactionDialogProps) {
  const qc = useQueryClient()

  const [transactionType, setTransactionType] = useState(defaultType)
  const [paymentMethodId, setPaymentMethodId] = useState<string>('')
  const [transactionDate, setTransactionDate] = useState(today())
  const [amount, setAmount] = useState<string>('')
  const [reference, setReference] = useState('')
  const [comments, setComments] = useState('')
  const [employeeId, setEmployeeId] = useState<string>(
    localStorage.getItem('default_employee_id') ?? '',
  )

  const { data: txTypes = [] } = useQuery({
    queryKey: ['payable-transaction-types'],
    queryFn: getPayableTransactionTypes,
    staleTime: 30 * 60 * 1000,
  })
  const { data: paymentMethods = [] } = useQuery({
    queryKey: ['payable-payment-methods'],
    queryFn: getPayablePaymentMethods,
    staleTime: 30 * 60 * 1000,
  })

  useEffect(() => {
    if (open) {
      setTransactionType(defaultType)
      setPaymentMethodId('')
      setTransactionDate(today())
      setAmount(
        suggestedAmount !== undefined && suggestedAmount > 0
          ? String(suggestedAmount.toFixed(2))
          : '',
      )
      setReference('')
      setComments('')
      setEmployeeId(localStorage.getItem('default_employee_id') ?? '')
    }
  }, [open, defaultType, suggestedAmount])

  const mutation = useMutation({
    mutationFn: (data: CreateProviderPayableTransactionRequest) =>
      addProviderPayableTransaction(payableId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-payable', payableId] })
      qc.invalidateQueries({ queryKey: ['provider-payables', providerId] })
      toast.success('Movimiento registrado')
      onOpenChange(false)
    },
    onError: (err: Error) => {
      if (err instanceof ApiError) {
        const body = err.body as { message?: string }
        toast.error(body?.message ?? 'No se pudo registrar el movimiento.')
      } else {
        toast.error('Error al registrar el movimiento')
      }
    },
  })

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!transactionType) {
      toast.error('Selecciona el tipo de movimiento.')
      return
    }
    const amt = Number(amount)
    if (!Number.isFinite(amt) || amt <= 0) {
      toast.error('El monto debe ser mayor a cero.')
      return
    }
    if (!employeeId) {
      toast.error('Indica el ID del empleado.')
      return
    }

    localStorage.setItem('default_employee_id', employeeId)

    mutation.mutate({
      transactionType,
      providerPaymentMethodId: paymentMethodId ? Number(paymentMethodId) : null,
      transactionDate,
      amount: amt,
      reference: reference.trim() || null,
      comments: comments.trim() || null,
      createdByEmployeeId: Number(employeeId),
    })
  }

  const showPaymentMethod = ['PAYMENT', 'CREDIT_NOTE_APPLIED'].includes(transactionType)

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-lg'>
        <DialogHeader>
          <DialogTitle>Registrar movimiento</DialogTitle>
          <DialogDescription>
            Agrega un movimiento al ledger de la cuenta. El balance se recalcula a partir
            de los movimientos.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2 sm:col-span-2'>
              <Label>
                Tipo de movimiento <span className='text-destructive'>*</span>
              </Label>
              <Select value={transactionType} onValueChange={setTransactionType}>
                <SelectTrigger>
                  <SelectValue placeholder='Seleccionar tipo' />
                </SelectTrigger>
                <SelectContent>
                  {txTypes.map((t) => (
                    <SelectItem key={t} value={t}>
                      {TYPE_LABELS[t] ?? t}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className='space-y-2'>
              <Label>
                Monto ({currencyCode}) <span className='text-destructive'>*</span>
              </Label>
              <Input
                type='number'
                step='0.0001'
                min='0.0001'
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
              />
            </div>

            <div className='space-y-2'>
              <Label>Fecha</Label>
              <Input
                type='date'
                value={transactionDate}
                onChange={(e) => setTransactionDate(e.target.value)}
              />
            </div>

            {showPaymentMethod && (
              <div className='space-y-2 sm:col-span-2'>
                <Label>Método de pago</Label>
                <Select value={paymentMethodId} onValueChange={setPaymentMethodId}>
                  <SelectTrigger>
                    <SelectValue placeholder='Opcional' />
                  </SelectTrigger>
                  <SelectContent>
                    {paymentMethods.map((m) => (
                      <SelectItem key={m.id} value={String(m.id)}>
                        {m.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className='space-y-2 sm:col-span-2'>
              <Label>Referencia</Label>
              <Input
                value={reference}
                onChange={(e) => setReference(e.target.value)}
                maxLength={100}
                placeholder='Folio, número de transferencia, etc.'
              />
            </div>

            <div className='space-y-2 sm:col-span-2'>
              <Label>Comentarios</Label>
              <Textarea
                value={comments}
                onChange={(e) => setComments(e.target.value)}
                rows={2}
              />
            </div>

            <div className='space-y-2 sm:col-span-2'>
              <Label>
                Empleado (ID) <span className='text-destructive'>*</span>
              </Label>
              <Input
                type='number'
                min={1}
                value={employeeId}
                onChange={(e) => setEmployeeId(e.target.value)}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              type='button'
              variant='outline'
              onClick={() => onOpenChange(false)}
              disabled={mutation.isPending}
            >
              Cancelar
            </Button>
            <Button type='submit' disabled={mutation.isPending}>
              {mutation.isPending ? 'Guardando...' : 'Registrar'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
