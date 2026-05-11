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
  ProviderContactResponse,
  CreateProviderContactRequest,
} from '@/services/provider-service'
import {
  createProviderContact,
  updateProviderContact,
} from '@/services/provider-service'

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
  providerId: number
  contact?: ProviderContactResponse | null
}

interface FormData {
  name: string
  role: string
  email: string
  phone: string
  cellphone: string
  whatsapp: boolean
  isPrimary: boolean
  notes: string
}

const empty: FormData = {
  name: '',
  role: '',
  email: '',
  phone: '',
  cellphone: '',
  whatsapp: false,
  isPrimary: false,
  notes: '',
}

export default function ProviderContactFormDialog({
  open,
  onOpenChange,
  providerId,
  contact,
}: Props) {
  const qc = useQueryClient()
  const isEditing = !!contact

  const [form, setForm] = useState<FormData>(empty)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      contact
        ? {
            name: contact.name,
            role: contact.role ?? '',
            email: contact.email ?? '',
            phone: contact.phone ?? '',
            cellphone: contact.cellphone ?? '',
            whatsapp: contact.whatsapp,
            isPrimary: contact.isPrimary,
            notes: contact.notes ?? '',
          }
        : empty,
    )
    setErrors({})
  } else if (!open && prevOpen) {
    setPrevOpen(false)
  }

  const createMutation = useMutation({
    mutationFn: (data: CreateProviderContactRequest) =>
      createProviderContact(providerId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      qc.invalidateQueries({ queryKey: ['provider-contacts', providerId] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Contacto creado')
      onOpenChange(false)
    },
    onError: () => toast.error('Error al crear el contacto'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: CreateProviderContactRequest) =>
      updateProviderContact(providerId, contact!.providerContactId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      qc.invalidateQueries({ queryKey: ['provider-contacts', providerId] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Contacto actualizado')
      onOpenChange(false)
    },
    onError: () => toast.error('Error al actualizar el contacto'),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  function validate(): boolean {
    const next: Partial<Record<keyof FormData, string>> = {}
    if (!form.name.trim()) next.name = 'El nombre es requerido.'
    else if (form.name.length > 150) next.name = 'Máximo 150 caracteres.'
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
      next.email = 'Email inválido.'
    setErrors(next)
    return Object.keys(next).length === 0
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateProviderContactRequest = {
      name: form.name.trim(),
      role: form.role.trim() || null,
      email: form.email.trim() || null,
      phone: form.phone.trim() || null,
      cellphone: form.cellphone.trim() || null,
      whatsapp: form.whatsapp,
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
          <DialogTitle>{isEditing ? 'Editar Contacto' : 'Nuevo Contacto'}</DialogTitle>
          <DialogDescription>
            Datos de contacto del proveedor (compras, cobranza, ventas, logística…).
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2 sm:col-span-2'>
              <Label htmlFor='name'>
                Nombre <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='name'
                value={form.name}
                onChange={(e) => update('name', e.target.value)}
                maxLength={150}
              />
              {errors.name && <p className='text-sm text-destructive'>{errors.name}</p>}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='role'>Rol / Puesto</Label>
              <Input
                id='role'
                value={form.role}
                onChange={(e) => update('role', e.target.value)}
                placeholder='Compras, cobranza, ventas...'
                maxLength={100}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='email'>Email</Label>
              <Input
                id='email'
                type='email'
                value={form.email}
                onChange={(e) => update('email', e.target.value)}
                maxLength={150}
              />
              {errors.email && <p className='text-sm text-destructive'>{errors.email}</p>}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='phone'>Teléfono</Label>
              <Input
                id='phone'
                value={form.phone}
                onChange={(e) => update('phone', e.target.value)}
                maxLength={30}
              />
            </div>

            <div className='space-y-2'>
              <Label htmlFor='cellphone'>Celular</Label>
              <Input
                id='cellphone'
                value={form.cellphone}
                onChange={(e) => update('cellphone', e.target.value)}
                maxLength={30}
              />
            </div>

            <div className='flex items-center gap-3 pt-6'>
              <Switch
                id='whatsapp'
                checked={form.whatsapp}
                onCheckedChange={(v) => update('whatsapp', v)}
              />
              <Label htmlFor='whatsapp' className='cursor-pointer'>
                Tiene WhatsApp
              </Label>
            </div>

            <div className='flex items-center gap-3 pt-6'>
              <Switch
                id='isPrimary'
                checked={form.isPrimary}
                onCheckedChange={(v) => update('isPrimary', v)}
              />
              <Label htmlFor='isPrimary' className='cursor-pointer'>
                Contacto primario
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
