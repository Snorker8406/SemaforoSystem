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

import type { ProductComboResponse } from '@/services/combo-service'
import { useCreateCombo, useUpdateCombo } from '@/hooks/use-combos'

// ── Types ────────────────────────────────────────────────

interface ComboFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  combo?: ProductComboResponse | null
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

export default function ComboFormDialog({
  open,
  onOpenChange,
  combo,
}: ComboFormDialogProps) {
  const isEditing = !!combo

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // ── Mutations ─────────────────────────────────────

  const createMutation = useCreateCombo({
    onSuccess: () => onOpenChange(false),
  })

  const updateMutation = useUpdateCombo({
    onSuccess: () => onOpenChange(false),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Populate form when dialog opens ───────────────

  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      combo
        ? {
            name: combo.name,
            description: combo.description ?? '',
          }
        : emptyForm,
    )
    setErrors({})
  }

  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}
    if (!form.name.trim()) newErrors.name = 'El nombre es obligatorio.'
    if (form.name.length > 250) newErrors.name = 'Máximo 250 caracteres.'
    if (form.description.length > 250) newErrors.description = 'Máximo 250 caracteres.'
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || null,
    }

    if (isEditing) {
      updateMutation.mutate({ id: combo!.productComboId, data: payload })
    } else {
      createMutation.mutate(payload)
    }
  }

  // ── Field helper ──────────────────────────────────

  function field(key: keyof FormData) {
    return {
      value: form[key],
      onChange: (
        e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
      ) => setForm((prev) => ({ ...prev, [key]: e.target.value })),
    }
  }

  // ── Render ────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[500px]'>
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>
              {isEditing ? 'Editar Combo' : 'Nuevo Combo'}
            </DialogTitle>
            <DialogDescription>
              {isEditing
                ? 'Modifica los datos del combo.'
                : 'Completa los datos para crear un nuevo combo de productos.'}
            </DialogDescription>
          </DialogHeader>

          <div className='grid gap-4 py-4'>
            {/* Name */}
            <div className='grid gap-2'>
              <Label htmlFor='combo-name'>
                Nombre <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='combo-name'
                placeholder='Ej: Combo Escolar Básico'
                {...field('name')}
              />
              {errors.name && (
                <p className='text-destructive text-xs'>{errors.name}</p>
              )}
            </div>

            {/* Description */}
            <div className='grid gap-2'>
              <Label htmlFor='combo-description'>Descripción</Label>
              <Textarea
                id='combo-description'
                placeholder='Descripción opcional del combo...'
                rows={3}
                {...field('description')}
              />
              {errors.description && (
                <p className='text-destructive text-xs'>{errors.description}</p>
              )}
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
              {isPending
                ? isEditing
                  ? 'Guardando...'
                  : 'Creando...'
                : isEditing
                  ? 'Guardar Cambios'
                  : 'Crear Combo'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
