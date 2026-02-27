import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
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
  EmbroideryResponse,
  CreateEmbroideryRequest,
} from '@/services/embroidery-service'
import {
  createEmbroidery,
  updateEmbroidery,
  uploadEmbroideryImage,
  uploadEmbroideryImageDesign,
} from '@/services/embroidery-service'
import { getSchoolsLookup } from '@/services/school-service'
import type { SchoolLookupItem } from '@/services/school-service'
import { embroideryKeys } from '@/hooks/use-embroideries'
import { ApiError } from '@/lib/api-client'
import { useQuery } from '@tanstack/react-query'

// ── Types ────────────────────────────────────────────────

interface EmbroideryFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  embroidery?: EmbroideryResponse | null
}

interface FormData {
  name: string
  schoolId: string
  description: string
  stiches: string
  colorSecuence: string
  price: string
}

const emptyForm: FormData = {
  name: '',
  schoolId: '',
  description: '',
  stiches: '',
  colorSecuence: '',
  price: '',
}

// ── Component ────────────────────────────────────────────

export default function EmbroideryFormDialog({
  open,
  onOpenChange,
  embroidery,
}: EmbroideryFormDialogProps) {
  const isEditing = !!embroidery
  const queryClient = useQueryClient()

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [imageFile, setImageFile] = useState<File | null>(null)
  const [imageDesignFile, setImageDesignFile] = useState<File | null>(null)

  // ── Lookups ───────────────────────────────────────
  const { data: schools = [] } = useQuery<SchoolLookupItem[]>({
    queryKey: ['schools-lookup'],
    queryFn: getSchoolsLookup,
    staleTime: 10 * 60 * 1000,
  })

  // ── Populate form when dialog opens (React 19 pattern) ─
  if (open && !prevOpen) {
    setPrevOpen(true)
    setImageFile(null)
    setImageDesignFile(null)
    setForm(
      embroidery
        ? {
            name: embroidery.name ?? '',
            schoolId: embroidery.schoolId ? String(embroidery.schoolId) : '',
            description: embroidery.description ?? '',
            stiches: embroidery.stiches ?? '',
            colorSecuence: embroidery.colorSecuence ?? '',
            price: embroidery.price != null ? String(embroidery.price) : '',
          }
        : emptyForm,
    )
    setErrors({})
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Field helpers ──────────────────────────────────
  const setField = <K extends keyof FormData>(key: K, value: FormData[K]) => {
    setForm((prev) => ({ ...prev, [key]: value }))
    if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }))
  }

  // ── Validation ─────────────────────────────────────
  function validate(): boolean {
    const errs: Partial<Record<keyof FormData, string>> = {}
    if (!form.name.trim()) errs.name = 'El nombre es obligatorio.'
    if (form.price && isNaN(Number(form.price))) errs.price = 'Precio inválido.'
    setErrors(errs)
    return Object.keys(errs).length === 0
  }

  // ── Submit ─────────────────────────────────────────
  async function handleSubmit() {
    if (!validate()) return
    setSubmitting(true)

    const payload: CreateEmbroideryRequest = {
      name: form.name.trim(),
      schoolId: form.schoolId ? Number(form.schoolId) : null,
      description: form.description.trim() || null,
      stiches: form.stiches.trim() || null,
      colorSecuence: form.colorSecuence.trim() || null,
      price: form.price ? Number(form.price) : null,
    }

    try {
      let result: EmbroideryResponse
      if (isEditing) {
        result = await updateEmbroidery(embroidery!.embroideryId, payload)
        toast.success('Ponchado actualizado exitosamente')
      } else {
        result = await createEmbroidery(payload)
        toast.success('Ponchado creado exitosamente')
      }

      // Upload image if selected
      if (imageFile) {
        await uploadEmbroideryImage(result.embroideryId, imageFile)
      }

      // Upload image design if selected
      if (imageDesignFile) {
        await uploadEmbroideryImageDesign(result.embroideryId, imageDesignFile)
      }

      queryClient.invalidateQueries({ queryKey: embroideryKeys.all })
      onOpenChange(false)
    } catch (error) {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al guardar el ponchado')
      } else {
        toast.error('Error al guardar el ponchado')
      }
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-lg'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Ponchado' : 'Nuevo Ponchado'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifica los campos y guarda los cambios.'
              : 'Completa los campos para crear un nuevo ponchado.'}
          </DialogDescription>
        </DialogHeader>

        <div className='grid gap-4 py-4'>
          {/* Name */}
          <div className='grid gap-2'>
            <Label htmlFor='emb-name'>Nombre *</Label>
            <Input
              id='emb-name'
              value={form.name}
              onChange={(e) => setField('name', e.target.value)}
              placeholder='Nombre del ponchado'
            />
            {errors.name && (
              <p className='text-destructive text-xs'>{errors.name}</p>
            )}
          </div>

          {/* School */}
          <div className='grid gap-2'>
            <Label>Escuela</Label>
            <Select
              value={form.schoolId}
              onValueChange={(v) => setField('schoolId', v === 'none' ? '' : v)}
            >
              <SelectTrigger>
                <SelectValue placeholder='Seleccionar escuela' />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value='none'>Sin escuela</SelectItem>
                {schools.map((s) => (
                  <SelectItem key={s.schoolId} value={String(s.schoolId)}>
                    {s.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Description */}
          <div className='grid gap-2'>
            <Label htmlFor='emb-desc'>Descripción</Label>
            <Textarea
              id='emb-desc'
              value={form.description}
              onChange={(e) => setField('description', e.target.value)}
              placeholder='Descripción del ponchado'
              rows={2}
            />
          </div>

          {/* ImageDesign image upload + Stiches row */}
          <div className='grid grid-cols-2 gap-4'>
            <div className='grid gap-2'>
              <Label htmlFor='emb-imagedesign'>Imagen Diseño</Label>
              <Input
                id='emb-imagedesign'
                type='file'
                accept='image/*'
                onChange={(e) => setImageDesignFile(e.target.files?.[0] ?? null)}
              />
              {isEditing && embroidery?.imageDesignBase64 && !imageDesignFile && (
                <p className='text-muted-foreground text-xs'>
                  Ya tiene imagen de diseño. Selecciona otra para reemplazarla.
                </p>
              )}
            </div>
            <div className='grid gap-2'>
              <Label htmlFor='emb-stiches'>Puntadas</Label>
              <Input
                id='emb-stiches'
                value={form.stiches}
                onChange={(e) => setField('stiches', e.target.value)}
                placeholder='Puntadas'
                maxLength={50}
              />
            </div>
          </div>

          {/* Color Sequence + Price row */}
          <div className='grid grid-cols-2 gap-4'>
            <div className='grid gap-2'>
              <Label htmlFor='emb-color'>Secuencia Colores</Label>
              <Input
                id='emb-color'
                value={form.colorSecuence}
                onChange={(e) => setField('colorSecuence', e.target.value)}
                placeholder='Secuencia de colores'
              />
            </div>
            <div className='grid gap-2'>
              <Label htmlFor='emb-price'>Precio</Label>
              <Input
                id='emb-price'
                type='number'
                step='0.01'
                value={form.price}
                onChange={(e) => setField('price', e.target.value)}
                placeholder='0.00'
              />
              {errors.price && (
                <p className='text-destructive text-xs'>{errors.price}</p>
              )}
            </div>
          </div>

          {/* Image upload */}
          <div className='grid gap-2'>
            <Label htmlFor='emb-image'>Imagen</Label>
            <Input
              id='emb-image'
              type='file'
              accept='image/*'
              onChange={(e) => setImageFile(e.target.files?.[0] ?? null)}
            />
            {isEditing && embroidery?.hasImage && !imageFile && (
              <p className='text-muted-foreground text-xs'>
                Ya tiene una imagen cargada. Selecciona otra para reemplazarla.
              </p>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button
            variant='outline'
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            Cancelar
          </Button>
          <Button onClick={handleSubmit} disabled={submitting}>
            {submitting
              ? 'Guardando...'
              : isEditing
                ? 'Guardar Cambios'
                : 'Crear Ponchado'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
