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

import type { VariantResponse, CreateVariantRequest } from '@/services/variant-service'
import { useCreateVariant, useUpdateVariant } from '@/hooks/use-variants'

// ── Types ────────────────────────────────────────────────

interface VariantFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  variant?: VariantResponse | null
  /** Required when creating a new variant */
  variantSystemId: number | null
  variantSystemName?: string
}

interface FormData {
  variantValue: string
  description: string
}

const emptyForm: FormData = {
  variantValue: '',
  description: '',
}

// ── Component ────────────────────────────────────────────

export default function VariantFormDialog({
  open,
  onOpenChange,
  variant,
  variantSystemId,
  variantSystemName,
}: VariantFormDialogProps) {
  const isEditing = !!variant

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Populate form when dialog opens (React 19 pattern)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      variant
        ? {
            variantValue: variant.variantValue ?? '',
            description: variant.description ?? '',
          }
        : emptyForm,
    )
    setErrors({})
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ─────────────────────────────────────

  const createMutation = useCreateVariant({
    onSuccess: () => onOpenChange(false),
  })

  const updateMutation = useUpdateVariant({
    onSuccess: () => onOpenChange(false),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.variantValue.trim()) newErrors.variantValue = 'El valor de variante es requerido'
    if (form.variantValue.trim().length > 150) newErrors.variantValue = 'Máximo 150 caracteres'
    if (form.description.length > 250) newErrors.description = 'Máximo 250 caracteres'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateVariantRequest = {
      variantValue: form.variantValue.trim(),
      description: form.description.trim() || null,
    }

    if (isEditing) {
      updateMutation.mutate({ id: variant!.productVariantId, data: payload })
    } else {
      if (!variantSystemId) return
      createMutation.mutate({ systemId: variantSystemId, data: payload })
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
      <DialogContent className='sm:max-w-[400px]'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Variante' : 'Nueva Variante'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? `Modifica la variante del sistema "${variant?.productVariantSystemName ?? ''}".`
              : `Agrega una variante al sistema "${variantSystemName ?? ''}".`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Variant Value */}
          <div className='space-y-2'>
            <Label htmlFor='variant-value'>Valor de variante *</Label>
            <Input
              id='variant-value'
              value={form.variantValue}
              onChange={(e) => setField('variantValue', e.target.value)}
              placeholder='Ej: Rojo, Azul, Algodón...'
              disabled={isPending}
            />
            {errors.variantValue && (
              <p className='text-destructive text-xs'>{errors.variantValue}</p>
            )}
          </div>

          {/* Description */}
          <div className='space-y-2'>
            <Label htmlFor='variant-description'>Descripción</Label>
            <Textarea
              id='variant-description'
              value={form.description}
              onChange={(e) => setField('description', e.target.value)}
              placeholder='Descripción opcional'
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
