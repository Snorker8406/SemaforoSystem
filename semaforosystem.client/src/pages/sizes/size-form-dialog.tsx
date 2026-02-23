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

import type { SizeResponse, CreateSizeRequest } from '@/services/size-service'
import { useCreateSize, useUpdateSize } from '@/hooks/use-sizes'

// ── Types ────────────────────────────────────────────────

interface SizeFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  size?: SizeResponse | null
  /** Required when creating a new size */
  sizeSystemId: number | null
  sizeSystemName?: string
}

interface FormData {
  sizeValue: string
  description: string
  sizeOrder: string
}

const emptyForm: FormData = {
  sizeValue: '',
  description: '',
  sizeOrder: '',
}

// ── Component ────────────────────────────────────────────

export default function SizeFormDialog({
  open,
  onOpenChange,
  size,
  sizeSystemId,
  sizeSystemName,
}: SizeFormDialogProps) {
  const isEditing = !!size

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Populate form when dialog opens (React 19 pattern)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      size
        ? {
            sizeValue: size.sizeValue ?? '',
            description: size.description ?? '',
            sizeOrder: size.sizeOrder != null ? String(size.sizeOrder) : '',
          }
        : emptyForm,
    )
    setErrors({})
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ─────────────────────────────────────

  const createMutation = useCreateSize({
    onSuccess: () => onOpenChange(false),
  })

  const updateMutation = useUpdateSize({
    onSuccess: () => onOpenChange(false),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.sizeValue.trim()) newErrors.sizeValue = 'El valor de talla es requerido'
    if (form.sizeValue.trim().length > 50) newErrors.sizeValue = 'Máximo 50 caracteres'
    if (form.description.length > 250) newErrors.description = 'Máximo 250 caracteres'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateSizeRequest = {
      sizeValue: form.sizeValue.trim(),
      description: form.description.trim() || null,
      sizeOrder: form.sizeOrder ? Number(form.sizeOrder) : null,
    }

    if (isEditing) {
      updateMutation.mutate({ id: size!.sizeId, data: payload })
    } else {
      if (!sizeSystemId) return
      createMutation.mutate({ systemId: sizeSystemId, data: payload })
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
            {isEditing ? 'Editar Talla' : 'Nueva Talla'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? `Modifica la talla del sistema "${size?.sizeSystemName ?? ''}".`
              : `Agrega una talla al sistema "${sizeSystemName ?? ''}".`}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Size Value */}
          <div className='space-y-2'>
            <Label htmlFor='size-value'>Valor de talla *</Label>
            <Input
              id='size-value'
              value={form.sizeValue}
              onChange={(e) => setField('sizeValue', e.target.value)}
              placeholder='Ej: S, M, L, XL, 28, 30...'
              disabled={isPending}
            />
            {errors.sizeValue && (
              <p className='text-destructive text-xs'>{errors.sizeValue}</p>
            )}
          </div>

          {/* Description */}
          <div className='space-y-2'>
            <Label htmlFor='size-description'>Descripción</Label>
            <Textarea
              id='size-description'
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

          {/* Size Order */}
          <div className='space-y-2'>
            <Label htmlFor='size-order'>Orden</Label>
            <Input
              id='size-order'
              type='number'
              value={form.sizeOrder}
              onChange={(e) => setField('sizeOrder', e.target.value)}
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
