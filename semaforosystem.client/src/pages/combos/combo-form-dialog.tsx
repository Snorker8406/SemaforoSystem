import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'

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
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { SearchIcon, XIcon } from 'lucide-react'

import type { ProductComboResponse } from '@/services/combo-service'
import { getSchoolsLookup } from '@/services/school-service'
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
  active: boolean
  schoolIds: number[]
}

const emptyForm: FormData = {
  name: '',
  description: '',
  active: true,
  schoolIds: [],
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
  const [schoolSearch, setSchoolSearch] = useState('')

  // ── Schools lookup ────────────────────────────────

  const { data: schools = [] } = useQuery({
    queryKey: ['schools', 'lookup'],
    queryFn: getSchoolsLookup,
    staleTime: 10 * 60 * 1000,
  })

  const filteredSchools = schoolSearch
    ? schools.filter((s) =>
        s.name.toLowerCase().includes(schoolSearch.toLowerCase()),
      )
    : schools

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
            active: combo.active ?? true,
            schoolIds: combo.schools?.map((s) => s.schoolId) ?? [],
          }
        : emptyForm,
    )
    setErrors({})
    setSchoolSearch('')
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
      active: form.active,
      schoolIds: form.schoolIds,
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
      <DialogContent className='sm:max-w-[560px]'>
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

            {/* Active */}
            <div className='flex items-center justify-between rounded-lg border p-3'>
              <div className='space-y-0.5'>
                <Label htmlFor='combo-active'>Activo</Label>
                <p className='text-muted-foreground text-xs'>
                  Define si el combo está disponible.
                </p>
              </div>
              <Switch
                id='combo-active'
                checked={form.active}
                onCheckedChange={(checked) =>
                  setForm((prev) => ({ ...prev, active: checked }))
                }
              />
            </div>

            {/* Schools */}
            <div className='grid gap-2'>
              <div className='flex items-center justify-between'>
                <Label>Escuelas</Label>
                {form.schoolIds.length > 0 && (
                  <Badge variant='secondary' className='text-xs'>
                    {form.schoolIds.length} seleccionada{form.schoolIds.length !== 1 ? 's' : ''}
                  </Badge>
                )}
              </div>
              <div className='relative'>
                <SearchIcon className='text-muted-foreground absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2' />
                <Input
                  placeholder='Buscar escuela...'
                  value={schoolSearch}
                  onChange={(e) => setSchoolSearch(e.target.value)}
                  className='h-8 pl-8 pr-8 text-sm'
                />
                {schoolSearch && (
                  <button
                    type='button'
                    onClick={() => setSchoolSearch('')}
                    className='text-muted-foreground hover:text-foreground absolute right-2.5 top-1/2 -translate-y-1/2'
                  >
                    <XIcon className='size-3.5' />
                  </button>
                )}
              </div>
              <div className='max-h-40 overflow-y-auto rounded-md border'>
                {filteredSchools.length === 0 ? (
                  <p className='text-muted-foreground p-3 text-center text-xs'>
                    No se encontraron escuelas.
                  </p>
                ) : (
                  filteredSchools.map((school) => (
                    <label
                      key={school.schoolId}
                      className='hover:bg-muted/50 flex cursor-pointer items-center gap-2 px-3 py-1.5 text-sm'
                    >
                      <Checkbox
                        checked={form.schoolIds.includes(school.schoolId)}
                        onCheckedChange={(checked) => {
                          setForm((prev) => ({
                            ...prev,
                            schoolIds: checked
                              ? [...prev.schoolIds, school.schoolId]
                              : prev.schoolIds.filter((id) => id !== school.schoolId),
                          }))
                        }}
                      />
                      {school.name}
                    </label>
                  ))
                )}
              </div>
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
