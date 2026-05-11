import { useState } from 'react'
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

import type {
  ProviderResponse,
  CreateProviderRequest,
  UpdateProviderRequest,
} from '@/services/provider-service'
import {
  createProvider,
  updateProvider,
  getProviderStatuses,
} from '@/services/provider-service'
import { ApiError } from '@/lib/api-client'

interface ProviderFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  provider?: ProviderResponse | null
}

interface FormData {
  providerStatusId: string
  legalName: string
  tradeName: string
  taxId: string
  website: string
  notes: string
}

const emptyForm: FormData = {
  providerStatusId: '',
  legalName: '',
  tradeName: '',
  taxId: '',
  website: '',
  notes: '',
}

export default function ProviderFormDialog({
  open,
  onOpenChange,
  provider,
}: ProviderFormDialogProps) {
  const qc = useQueryClient()
  const isEditing = !!provider

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  const { data: statuses = [] } = useQuery({
    queryKey: ['provider-statuses'],
    queryFn: getProviderStatuses,
    staleTime: 10 * 60 * 1000,
  })

  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      provider
        ? {
            providerStatusId: String(provider.providerStatusId),
            legalName: provider.legalName,
            tradeName: provider.tradeName ?? '',
            taxId: provider.taxId ?? '',
            website: provider.website ?? '',
            notes: provider.notes ?? '',
          }
        : {
            ...emptyForm,
            providerStatusId: statuses.find((s) => s.code === 'ACTIVE')
              ? String(statuses.find((s) => s.code === 'ACTIVE')!.providerStatusId)
              : statuses[0]
                ? String(statuses[0].providerStatusId)
                : '',
          },
    )
    setErrors({})
  } else if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: (data: CreateProviderRequest) => createProvider(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Proveedor creado exitosamente')
      onOpenChange(false)
    },
    onError: (err: Error) => handleConflict(err, 'crear'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateProviderRequest) => updateProvider(provider!.providerId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['providers'] })
      qc.invalidateQueries({ queryKey: ['provider', provider!.providerId] })
      toast.success('Proveedor actualizado exitosamente')
      onOpenChange(false)
    },
    onError: (err: Error) => handleConflict(err, 'actualizar'),
  })

  function handleConflict(err: Error, action: string) {
    if (err instanceof ApiError && err.status === 409) {
      const body = err.body as { message?: string }
      toast.error(body?.message ?? `No se pudo ${action}: conflicto.`)
    } else if (err instanceof ApiError && err.status === 422) {
      const body = err.body as { message?: string }
      toast.error(body?.message ?? `No se pudo ${action}: datos inválidos.`)
    } else {
      toast.error(`Error al ${action} el proveedor`)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ───────────────────────────────────────

  function validate(): boolean {
    const next: Partial<Record<keyof FormData, string>> = {}
    if (!form.providerStatusId) next.providerStatusId = 'Seleccione un estado.'
    if (!form.legalName.trim()) next.legalName = 'La razón social es requerida.'
    else if (form.legalName.length > 200) next.legalName = 'Máximo 200 caracteres.'
    if (form.tradeName.length > 200) next.tradeName = 'Máximo 200 caracteres.'
    if (form.taxId.length > 50) next.taxId = 'Máximo 50 caracteres.'
    if (form.website && form.website.length > 250)
      next.website = 'Máximo 250 caracteres.'
    if (form.website && !/^https?:\/\//i.test(form.website))
      next.website = 'Debe iniciar con http:// o https://'

    setErrors(next)
    return Object.keys(next).length === 0
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateProviderRequest = {
      providerStatusId: Number(form.providerStatusId),
      legalName: form.legalName.trim(),
      tradeName: form.tradeName.trim() || null,
      taxId: form.taxId.trim() || null,
      website: form.website.trim() || null,
      notes: form.notes.trim() || null,
    }

    if (isEditing) updateMutation.mutate(payload)
    else createMutation.mutate(payload)
  }

  function updateField(field: keyof FormData, value: string) {
    setForm((prev) => ({ ...prev, [field]: value }))
    if (errors[field]) {
      setErrors((prev) => {
        const next = { ...prev }
        delete next[field]
        return next
      })
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-2xl max-h-[90vh] overflow-y-auto'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Proveedor' : 'Nuevo Proveedor'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifique los datos maestros del proveedor.'
              : 'Registre los datos básicos del proveedor. Contactos, direcciones y cuentas bancarias se administran en el detalle.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2 sm:col-span-2'>
              <Label htmlFor='legalName'>
                Razón social <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='legalName'
                value={form.legalName}
                onChange={(e) => updateField('legalName', e.target.value)}
                placeholder='Nombre legal del proveedor'
                maxLength={200}
              />
              {errors.legalName && (
                <p className='text-sm text-destructive'>{errors.legalName}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='tradeName'>Nombre comercial</Label>
              <Input
                id='tradeName'
                value={form.tradeName}
                onChange={(e) => updateField('tradeName', e.target.value)}
                placeholder='Nombre comercial'
                maxLength={200}
              />
              {errors.tradeName && (
                <p className='text-sm text-destructive'>{errors.tradeName}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='taxId'>RFC / Tax ID</Label>
              <Input
                id='taxId'
                value={form.taxId}
                onChange={(e) => updateField('taxId', e.target.value)}
                placeholder='RFC o identificador fiscal'
                maxLength={50}
              />
              {errors.taxId && (
                <p className='text-sm text-destructive'>{errors.taxId}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='providerStatusId'>
                Estado <span className='text-destructive'>*</span>
              </Label>
              <Select
                value={form.providerStatusId}
                onValueChange={(v) => updateField('providerStatusId', v)}
              >
                <SelectTrigger id='providerStatusId'>
                  <SelectValue placeholder='Seleccionar estado' />
                </SelectTrigger>
                <SelectContent>
                  {statuses.map((s) => (
                    <SelectItem
                      key={s.providerStatusId}
                      value={String(s.providerStatusId)}
                    >
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.providerStatusId && (
                <p className='text-sm text-destructive'>{errors.providerStatusId}</p>
              )}
            </div>

            <div className='space-y-2'>
              <Label htmlFor='website'>Sitio web</Label>
              <Input
                id='website'
                value={form.website}
                onChange={(e) => updateField('website', e.target.value)}
                placeholder='https://...'
                maxLength={250}
              />
              {errors.website && (
                <p className='text-sm text-destructive'>{errors.website}</p>
              )}
            </div>
          </div>

          <div className='space-y-2'>
            <Label htmlFor='notes'>Notas</Label>
            <Textarea
              id='notes'
              value={form.notes}
              onChange={(e) => updateField('notes', e.target.value)}
              placeholder='Notas internas sobre el proveedor'
              rows={3}
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
              {isPending ? 'Guardando...' : isEditing ? 'Guardar cambios' : 'Crear proveedor'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
