import { useState } from 'react'

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

import type { SizeSystemResponse, CreateSizeSystemRequest } from '@/services/size-service'
import { useCreateSizeSystem, useUpdateSizeSystem } from '@/hooks/use-sizes'

// ── Types ────────────────────────────────────────────────

interface SizeSystemFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  sizeSystem?: SizeSystemResponse | null
}

interface FormData {
  name: string
  description: string
  sortOrder: string
}

const emptyForm: FormData = {
  name: '',
  description: '',
  sortOrder: '',
}

// ── Component ────────────────────────────────────────────

export default function SizeSystemFormDialog({
  open,
  onOpenChange,
  sizeSystem,
}: SizeSystemFormDialogProps) {
  const isEditing = !!sizeSystem

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Populate form when dialog opens (React 19 pattern)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      sizeSystem
        ? {
            name: sizeSystem.name ?? '',
            description: sizeSystem.description ?? '',
            sortOrder: sizeSystem.sortOrder != null ? String(sizeSystem.sortOrder) : '',
          }
        : emptyForm,
    )
    setErrors({})
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ─────────────────────────────────────

  const createMutation = useCreateSizeSystem({
    onSuccess: () => onOpenChange(false),
  })

  const updateMutation = useUpdateSizeSystem({
    onSuccess: () => onOpenChange(false),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.name.trim()) newErrors.name = 'El nombre es requerido'
    if (form.name.trim().length > 100) newErrors.name = 'Máximo 100 caracteres'
    if (form.description.length > 200) newErrors.description = 'Máximo 200 caracteres'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateSizeSystemRequest = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      sortOrder: form.sortOrder ? Number(form.sortOrder) : null,
    }

    if (isEditing) {
      updateMutation.mutate({ id: sizeSystem!.sizeSystemId, data: payload })
    } else {
      createMutation.mutate(payload)
    }
  }

  // ── Helpers ───────────────────────────────────────

  function setField<K extends keyof FormData>(key: K, value: FormData[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
    if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }))
  }

  // ── Render ────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[450px]'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Sistema de Tallas' : 'Nuevo Sistema de Tallas'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifica los campos del sistema de tallas.'
              : 'Completa los datos para crear un nuevo sistema de tallas.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Name */}
          <div className='space-y-2'>
            <Label htmlFor='ss-name'>Nombre *</Label>
            <Input
              id='ss-name'
              value={form.name}
              onChange={(e) => setField('name', e.target.value)}
              placeholder='Ej: Tallas numéricas, Tallas letra'
              disabled={isPending}
            />
            {errors.name && (
              <p className='text-destructive text-xs'>{errors.name}</p>
            )}
          </div>

          {/* Description */}
          <div className='space-y-2'>
            <Label htmlFor='ss-description'>Descripción</Label>
            <Textarea
              id='ss-description'
              value={form.description}
              onChange={(e) => setField('description', e.target.value)}
              placeholder='Descripción opcional del sistema'
              rows={2}
              disabled={isPending}
            />
            {errors.description && (
              <p className='text-destructive text-xs'>{errors.description}</p>
            )}
          </div>

          {/* Sort Order */}
          <div className='space-y-2'>
            <Label htmlFor='ss-sortOrder'>Orden de presentación</Label>
            <Input
              id='ss-sortOrder'
              type='number'
              value={form.sortOrder}
              onChange={(e) => setField('sortOrder', e.target.value)}
              placeholder='Ej: 1, 2, 3...'
              disabled={isPending}
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
              {isPending
                ? 'Guardando...'
                : isEditing
                  ? 'Actualizar'
                  : 'Crear'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
