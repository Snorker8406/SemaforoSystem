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

import type { VariantSystemResponse, CreateVariantSystemRequest } from '@/services/variant-service'
import { useCreateVariantSystem, useUpdateVariantSystem } from '@/hooks/use-variants'

// ── Types ────────────────────────────────────────────────

interface VariantSystemFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  variantSystem?: VariantSystemResponse | null
}

interface FormData {
  name: string
  description: string
}

const emptyForm: FormData = {
  name: '',
  description: '',
}

// ── Component ────────────────────────────────────────────

export default function VariantSystemFormDialog({
  open,
  onOpenChange,
  variantSystem,
}: VariantSystemFormDialogProps) {
  const isEditing = !!variantSystem

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Populate form when dialog opens (React 19 pattern)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      variantSystem
        ? {
            name: variantSystem.name ?? '',
            description: variantSystem.description ?? '',
          }
        : emptyForm,
    )
    setErrors({})
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ─────────────────────────────────────

  const createMutation = useCreateVariantSystem({
    onSuccess: () => onOpenChange(false),
  })

  const updateMutation = useUpdateVariantSystem({
    onSuccess: () => onOpenChange(false),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.name.trim()) newErrors.name = 'El nombre es requerido'
    if (form.name.trim().length > 100) newErrors.name = 'Máximo 100 caracteres'
    if (form.description.length > 250) newErrors.description = 'Máximo 250 caracteres'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateVariantSystemRequest = {
      name: form.name.trim(),
      description: form.description.trim() || null,
    }

    if (isEditing) {
      updateMutation.mutate({ id: variantSystem!.productVariantId, data: payload })
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
            {isEditing ? 'Editar Sistema de Variantes' : 'Nuevo Sistema de Variantes'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifica los campos del sistema de variantes.'
              : 'Completa los datos para crear un nuevo sistema de variantes.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Name */}
          <div className='space-y-2'>
            <Label htmlFor='vs-name'>Nombre *</Label>
            <Input
              id='vs-name'
              value={form.name}
              onChange={(e) => setField('name', e.target.value)}
              placeholder='Ej: Colores, Estilos, Materiales'
              disabled={isPending}
            />
            {errors.name && (
              <p className='text-destructive text-xs'>{errors.name}</p>
            )}
          </div>

          {/* Description */}
          <div className='space-y-2'>
            <Label htmlFor='vs-description'>Descripción</Label>
            <Textarea
              id='vs-description'
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
