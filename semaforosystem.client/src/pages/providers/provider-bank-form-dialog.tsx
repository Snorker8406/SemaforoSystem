import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
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
import { Switch } from '@/components/ui/switch'

import type {
  ProviderBankAccountResponse,
  CreateProviderBankAccountRequest,
} from '@/services/provider-service'
import {
  createProviderBankAccount,
  updateProviderBankAccount,
} from '@/services/provider-service'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  providerId: number
  bankAccount?: ProviderBankAccountResponse | null
}

interface FormData {
  bankName: string
  accountHolder: string
  accountNumber: string
  clabe: string
  currencyCode: string
  isPrimary: boolean
  notes: string
}

const empty: FormData = {
  bankName: '',
  accountHolder: '',
  accountNumber: '',
  clabe: '',
  currencyCode: 'MXN',
  isPrimary: false,
  notes: '',
}

export default function ProviderBankFormDialog({
  open,
  onOpenChange,
  providerId,
  bankAccount,
}: Props) {
  const qc = useQueryClient()
  const isEditing = !!bankAccount

  const [form, setForm] = useState<FormData>(empty)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      bankAccount
        ? {
            bankName: bankAccount.bankName,
            accountHolder: bankAccount.accountHolder ?? '',
            accountNumber: bankAccount.accountNumber ?? '',
            clabe: bankAccount.clabe ?? '',
            currencyCode: bankAccount.currencyCode,
            isPrimary: bankAccount.isPrimary,
            notes: bankAccount.notes ?? '',
          }
        : empty,
    )
    setErrors({})
  } else if (!open && prevOpen) {
    setPrevOpen(false)
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateProviderBankAccountRequest) =>
      createProviderBankAccount(providerId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      qc.invalidateQueries({ queryKey: ['provider-banks', providerId] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Cuenta bancaria creada')
      onOpenChange(false)
    },
    onError: () => toast.error('Error al crear la cuenta bancaria'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: CreateProviderBankAccountRequest) =>
      updateProviderBankAccount(providerId, bankAccount!.providerBankAccountId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      qc.invalidateQueries({ queryKey: ['provider-banks', providerId] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Cuenta bancaria actualizada')
      onOpenChange(false)
    },
    onError: () => toast.error('Error al actualizar la cuenta bancaria'),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  function validate(): boolean {
    const next: Partial<Record<keyof FormData, string>> = {}
    if (!form.bankName.trim()) next.bankName = 'El banco es requerido.'
    else if (form.bankName.length > 120) next.bankName = 'Máximo 120 caracteres.'
    if (!form.currencyCode.trim()) next.currencyCode = 'La moneda es requerida.'
    else if (form.currencyCode.length > 10) next.currencyCode = 'Máximo 10 caracteres.'
    if (form.clabe && !/^\d{18}$/.test(form.clabe))
      next.clabe = 'La CLABE debe tener 18 dígitos.'
    setErrors(next)
    return Object.keys(next).length === 0
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateProviderBankAccountRequest = {
      bankName: form.bankName.trim(),
      accountHolder: form.accountHolder.trim() || null,
      accountNumber: form.accountNumber.trim() || null,
      clabe: form.clabe.trim() || null,
      currencyCode: form.currencyCode.trim().toUpperCase(),
      isPrimary: form.isPrimary,
      notes: form.notes.trim() || null,
    }

    if (isEditing) updateMutation.mutate(payload)
    else createMutation.mutate(payload)
  }

  function update(field: keyof FormData, value: string | boolean) {
    setForm((p) => ({ ...p, [field]: value }))
    if (errors[field]) {
      setErrors((p) => {
        const n = { ...p }
        delete n[field]
        return n
      })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-xl max-h-[90vh] overflow-y-auto'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Cuenta Bancaria' : 'Nueva Cuenta Bancaria'}
          </DialogTitle>
          <DialogDescription>
            Cuenta bancaria del proveedor para pagos y transferencias.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2'>
              <Label htmlFor='bankName'>
                Banco <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='bankName'
                value={form.bankName}
                onChange={(e) => update('bankName', e.target.value)}
                maxLength={120}
              />
              {errors.bankName && (
                <p className='text-sm text-destructive'>{errors.bankName}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='accountHolder'>Titular</Label>
              <Input
                id='accountHolder'
                value={form.accountHolder}
                onChange={(e) => update('accountHolder', e.target.value)}
                maxLength={200}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='accountNumber'>No. de cuenta</Label>
              <Input
                id='accountNumber'
                value={form.accountNumber}
                onChange={(e) => update('accountNumber', e.target.value)}
                maxLength={60}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='clabe'>CLABE</Label>
              <Input
                id='clabe'
                value={form.clabe}
                onChange={(e) => update('clabe', e.target.value)}
                maxLength={30}
              />
              {errors.clabe && (
                <p className='text-sm text-destructive'>{errors.clabe}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='currencyCode'>
                Moneda <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='currencyCode'
                value={form.currencyCode}
                onChange={(e) => update('currencyCode', e.target.value.toUpperCase())}
                placeholder='MXN, USD...'
                maxLength={10}
              />
              {errors.currencyCode && (
                <p className='text-sm text-destructive'>{errors.currencyCode}</p>
              )}
            </div>

            <div className='flex items-center gap-3 pt-6'>
              <Switch
                id='isPrimary'
                checked={form.isPrimary}
                onCheckedChange={(v) => update('isPrimary', v)}
              />
              <Label htmlFor='isPrimary' className='cursor-pointer'>
                Cuenta primaria
              </Label>
            </div>
          </div>

          <div className='space-y-2'>
            <Label htmlFor='notes'>Notas</Label>
            <Textarea
              id='notes'
              value={form.notes}
              onChange={(e) => update('notes', e.target.value)}
              rows={2}
              maxLength={250}
            />
          </div>

          <DialogFooter>
            <Button
              type='button'
              variant='outline'
              onClick={() => onOpenChange(false)}
              disabled={isPending}
            >
              Cancelar
            </Button>
            <Button type='submit' disabled={isPending}>
              {isPending ? 'Guardando...' : isEditing ? 'Guardar' : 'Crear'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
