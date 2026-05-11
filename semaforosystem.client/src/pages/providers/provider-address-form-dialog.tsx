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
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

import type {
  ProviderAddressResponse,
  CreateProviderAddressRequest,
} from '@/services/provider-service'
import {
  createProviderAddress,
  updateProviderAddress,
} from '@/services/provider-service'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  providerId: number
  address?: ProviderAddressResponse | null
}

interface FormData {
  addressType: string
  addressLine: string
  city: string
  state: string
  postalCode: string
  country: string
  isPrimary: boolean
}

const ADDRESS_TYPES = [
  { value: 'FISCAL', label: 'Fiscal' },
  { value: 'WAREHOUSE', label: 'Almacén' },
  { value: 'SHIPPING', label: 'Envío' },
  { value: 'OTHER', label: 'Otro' },
]

const empty: FormData = {
  addressType: 'FISCAL',
  addressLine: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'México',
  isPrimary: false,
}

export default function ProviderAddressFormDialog({
  open,
  onOpenChange,
  providerId,
  address,
}: Props) {
  const qc = useQueryClient()
  const isEditing = !!address

  const [form, setForm] = useState<FormData>(empty)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      address
        ? {
            addressType: address.addressType,
            addressLine: address.addressLine,
            city: address.city ?? '',
            state: address.state ?? '',
            postalCode: address.postalCode ?? '',
            country: address.country ?? '',
            isPrimary: address.isPrimary,
          }
        : empty,
    )
    setErrors({})
  } else if (!open && prevOpen) {
    setPrevOpen(false)
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateProviderAddressRequest) =>
      createProviderAddress(providerId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      qc.invalidateQueries({ queryKey: ['provider-addresses', providerId] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Dirección creada')
      onOpenChange(false)
    },
    onError: () => toast.error('Error al crear la dirección'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: CreateProviderAddressRequest) =>
      updateProviderAddress(providerId, address!.providerAddressId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      qc.invalidateQueries({ queryKey: ['provider-addresses', providerId] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Dirección actualizada')
      onOpenChange(false)
    },
    onError: () => toast.error('Error al actualizar la dirección'),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  function validate(): boolean {
    const next: Partial<Record<keyof FormData, string>> = {}
    if (!form.addressType.trim()) next.addressType = 'Tipo requerido.'
    if (!form.addressLine.trim()) next.addressLine = 'La dirección es requerida.'
    else if (form.addressLine.length > 300) next.addressLine = 'Máximo 300 caracteres.'
    setErrors(next)
    return Object.keys(next).length === 0
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateProviderAddressRequest = {
      addressType: form.addressType.trim(),
      addressLine: form.addressLine.trim(),
      city: form.city.trim() || null,
      state: form.state.trim() || null,
      postalCode: form.postalCode.trim() || null,
      country: form.country.trim() || null,
      isPrimary: form.isPrimary,
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
            {isEditing ? 'Editar Dirección' : 'Nueva Dirección'}
          </DialogTitle>
          <DialogDescription>
            Dirección fiscal, de almacén, envío u otra del proveedor.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2'>
              <Label htmlFor='addressType'>
                Tipo <span className='text-destructive'>*</span>
              </Label>
              <Select
                value={form.addressType}
                onValueChange={(v) => update('addressType', v)}
              >
                <SelectTrigger id='addressType'>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {ADDRESS_TYPES.map((t) => (
                    <SelectItem key={t.value} value={t.value}>
                      {t.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.addressType && (
                <p className='text-sm text-destructive'>{errors.addressType}</p>
              )}
            </div>

            <div className='flex items-center gap-3 pt-6'>
              <Switch
                id='isPrimary'
                checked={form.isPrimary}
                onCheckedChange={(v) => update('isPrimary', v)}
              />
              <Label htmlFor='isPrimary' className='cursor-pointer'>
                Dirección primaria
              </Label>
            </div>

            <div className='space-y-2 sm:col-span-2'>
              <Label htmlFor='addressLine'>
                Dirección <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='addressLine'
                value={form.addressLine}
                onChange={(e) => update('addressLine', e.target.value)}
                placeholder='Calle, número, colonia'
                maxLength={300}
              />
              {errors.addressLine && (
                <p className='text-sm text-destructive'>{errors.addressLine}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='city'>Ciudad</Label>
              <Input
                id='city'
                value={form.city}
                onChange={(e) => update('city', e.target.value)}
                maxLength={100}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='state'>Estado</Label>
              <Input
                id='state'
                value={form.state}
                onChange={(e) => update('state', e.target.value)}
                maxLength={100}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='postalCode'>Código Postal</Label>
              <Input
                id='postalCode'
                value={form.postalCode}
                onChange={(e) => update('postalCode', e.target.value)}
                maxLength={20}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='country'>País</Label>
              <Input
                id='country'
                value={form.country}
                onChange={(e) => update('country', e.target.value)}
                maxLength={100}
              />
            </div>
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
