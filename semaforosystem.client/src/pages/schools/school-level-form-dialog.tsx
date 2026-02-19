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

import type {
  SchoolLevelResponse,
  CreateSchoolLevelRequest,
  UpdateSchoolLevelRequest,
} from '@/services/school-level-service'
import {
  createSchoolLevel,
  updateSchoolLevel,
} from '@/services/school-level-service'
import { ApiError } from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

interface SchoolLevelFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  level?: SchoolLevelResponse | null
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

export default function SchoolLevelFormDialog({
  open,
  onOpenChange,
  level,
}: SchoolLevelFormDialogProps) {
  const queryClient = useQueryClient()
  const isEditing = !!level

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Populate form when dialog opens (adjust state during render)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      level
        ? {
            name: level.name,
            description: level.description ?? '',
          }
        : emptyForm,
    )
    setErrors({})
  } else if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: (data: CreateSchoolLevelRequest) => createSchoolLevel(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schoolLevels'] })
      queryClient.invalidateQueries({ queryKey: ['school-levels'] })
      toast.success('Nivel escolar creado exitosamente')
      onOpenChange(false)
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        setErrors({ name: 'Ya existe un nivel escolar con este nombre.' })
      } else {
        toast.error('Error al crear el nivel escolar')
      }
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateSchoolLevelRequest) =>
      updateSchoolLevel(level!.schoolLevelId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schoolLevels'] })
      queryClient.invalidateQueries({ queryKey: ['school-levels'] })
      toast.success('Nivel escolar actualizado exitosamente')
      onOpenChange(false)
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        setErrors({ name: 'Ya existe un nivel escolar con este nombre.' })
      } else {
        toast.error('Error al actualizar el nivel escolar')
      }
    },
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ───────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.name.trim()) newErrors.name = 'El nombre es requerido.'
    else if (form.name.length < 2) newErrors.name = 'Mínimo 2 caracteres.'
    else if (form.name.length > 50) newErrors.name = 'Máximo 50 caracteres.'

    if (form.description.length > 250)
      newErrors.description = 'Máximo 250 caracteres.'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ───────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload = {
      name: form.name.trim(),
      description: form.description.trim() || null,
    }

    if (isEditing) {
      updateMutation.mutate(payload)
    } else {
      createMutation.mutate(payload)
    }
  }

  // ── Field helpers ────────────────────────────────────

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

  // ── Render ───────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-md'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Nivel Escolar' : 'Nuevo Nivel Escolar'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifique los datos del nivel escolar y guarde los cambios.'
              : 'Complete los datos para registrar un nuevo nivel escolar.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Name */}
          <div className='space-y-2'>
            <Label htmlFor='levelName'>
              Nombre <span className='text-destructive'>*</span>
            </Label>
            <Input
              id='levelName'
              value={form.name}
              onChange={(e) => updateField('name', e.target.value)}
              placeholder='Ej: Primaria, Secundaria, Preparatoria...'
              maxLength={50}
            />
            {errors.name && (
              <p className='text-sm text-destructive'>{errors.name}</p>
            )}
          </div>

          {/* Description */}
          <div className='space-y-2'>
            <Label htmlFor='levelDescription'>Descripción</Label>
            <Textarea
              id='levelDescription'
              value={form.description}
              onChange={(e) => updateField('description', e.target.value)}
              placeholder='Descripción del nivel escolar (opcional)'
              maxLength={250}
              rows={3}
            />
            {errors.description && (
              <p className='text-sm text-destructive'>{errors.description}</p>
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
                  ? 'Guardar Cambios'
                  : 'Crear Nivel'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
