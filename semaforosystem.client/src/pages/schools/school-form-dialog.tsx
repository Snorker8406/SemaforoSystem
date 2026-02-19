import { useState, useRef } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import {
  ImageIcon,
  UploadIcon,
  Trash2Icon,
  CameraIcon,
} from 'lucide-react'

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
  SchoolResponse,
  CreateSchoolRequest,
  UpdateSchoolRequest,
} from '@/services/school-service'
import {
  createSchool,
  updateSchool,
  getSchoolLevels,
  uploadSchoolLogo,
  uploadSchoolPhoto,
  deleteSchoolLogo,
  deleteSchoolPhoto,
  getSchoolLogoUrl,
  getSchoolPhotoUrl,
} from '@/services/school-service'
import { ApiError } from '@/lib/api-client'

// ── Types ────────────────────────────────────────────────

interface SchoolFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  school?: SchoolResponse | null
}

interface FormData {
  schoolLevelId: string
  name: string
  address: string
  ciudad: string
  state: string
  phoneNumber: string
  principalInfo: string
  email: string
  description: string
}

const emptyForm: FormData = {
  schoolLevelId: '',
  name: '',
  address: '',
  ciudad: '',
  state: '',
  phoneNumber: '',
  principalInfo: '',
  email: '',
  description: '',
}

// ── Component ────────────────────────────────────────────

export default function SchoolFormDialog({
  open,
  onOpenChange,
  school,
}: SchoolFormDialogProps) {
  const queryClient = useQueryClient()
  const isEditing = !!school

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Fetch school levels for select
  const { data: levels = [] } = useQuery({
    queryKey: ['school-levels'],
    queryFn: getSchoolLevels,
    staleTime: 10 * 60 * 1000,
  })

  // Populate form when dialog opens (adjust state during render)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      school
        ? {
            schoolLevelId: String(school.schoolLevelId),
            name: school.name,
            address: school.address,
            ciudad: school.ciudad ?? '',
            state: school.state ?? '',
            phoneNumber: school.phoneNumber ?? '',
            principalInfo: school.principalInfo ?? '',
            email: school.email ?? '',
            description: school.description ?? '',
          }
        : emptyForm,
    )
    setErrors({})
  } else if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: (data: CreateSchoolRequest) => createSchool(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      toast.success('Escuela creada exitosamente')
      onOpenChange(false)
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        setErrors({ name: 'Ya existe una escuela con este nombre.' })
      } else {
        toast.error('Error al crear la escuela')
      }
    },
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateSchoolRequest) => updateSchool(school!.schoolId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      toast.success('Escuela actualizada exitosamente')
      onOpenChange(false)
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        setErrors({ name: 'Ya existe una escuela con este nombre.' })
      } else {
        toast.error('Error al actualizar la escuela')
      }
    },
  })

  // ── Image mutations (edit mode only) ─────────────────

  const logoInputRef = useRef<HTMLInputElement>(null)
  const photoInputRef = useRef<HTMLInputElement>(null)
  const [logoPreview, setLogoPreview] = useState<string | null>(null)
  const [photoPreview, setPhotoPreview] = useState<string | null>(null)

  const uploadLogoMutation = useMutation({
    mutationFn: (file: File) => uploadSchoolLogo(school!.schoolId, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      toast.success('Logo actualizado')
    },
    onError: () => toast.error('Error al subir el logo'),
  })

  const uploadPhotoMutation = useMutation({
    mutationFn: (file: File) => uploadSchoolPhoto(school!.schoolId, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      toast.success('Foto actualizada')
    },
    onError: () => toast.error('Error al subir la foto'),
  })

  const deleteLogoMutation = useMutation({
    mutationFn: () => deleteSchoolLogo(school!.schoolId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      setLogoPreview(null)
      toast.success('Logo eliminado')
    },
    onError: () => toast.error('Error al eliminar el logo'),
  })

  const deletePhotoMutation = useMutation({
    mutationFn: () => deleteSchoolPhoto(school!.schoolId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      setPhotoPreview(null)
      toast.success('Foto eliminada')
    },
    onError: () => toast.error('Error al eliminar la foto'),
  })

  function handleFileSelect(
    e: React.ChangeEvent<HTMLInputElement>,
    type: 'logo' | 'photo',
  ) {
    const file = e.target.files?.[0]
    if (!file) return

    if (!file.type.startsWith('image/')) {
      toast.error('Solo se permiten archivos de imagen.')
      return
    }

    const maxSize = type === 'logo' ? 2 * 1024 * 1024 : 5 * 1024 * 1024
    if (file.size > maxSize) {
      toast.error(`El archivo excede el tamaño máximo (${type === 'logo' ? '2 MB' : '5 MB'}).`)
      return
    }

    // Show preview
    const url = URL.createObjectURL(file)
    if (type === 'logo') {
      setLogoPreview(url)
      uploadLogoMutation.mutate(file)
    } else {
      setPhotoPreview(url)
      uploadPhotoMutation.mutate(file)
    }

    // Reset input so the same file can be selected again
    e.target.value = ''
  }

  const isImageUploading =
    uploadLogoMutation.isPending ||
    uploadPhotoMutation.isPending ||
    deleteLogoMutation.isPending ||
    deletePhotoMutation.isPending

  const isPending = createMutation.isPending || updateMutation.isPending || isImageUploading

  // ── Validation ───────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.schoolLevelId) newErrors.schoolLevelId = 'Seleccione un nivel escolar.'
    if (!form.name.trim()) newErrors.name = 'El nombre es requerido.'
    else if (form.name.length > 200) newErrors.name = 'Máximo 200 caracteres.'
    if (!form.address.trim()) newErrors.address = 'La dirección es requerida.'
    else if (form.address.length > 150) newErrors.address = 'Máximo 150 caracteres.'
    if (form.email && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email))
      newErrors.email = 'Email inválido.'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ───────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload = {
      schoolLevelId: Number(form.schoolLevelId),
      name: form.name.trim(),
      address: form.address.trim(),
      ciudad: form.ciudad.trim() || null,
      state: form.state.trim() || null,
      phoneNumber: form.phoneNumber.trim() || null,
      principalInfo: form.principalInfo.trim() || null,
      email: form.email.trim() || null,
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
      <DialogContent className='max-w-2xl max-h-[90vh] overflow-y-auto'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Escuela' : 'Nueva Escuela'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifique los datos de la escuela y guarde los cambios.'
              : 'Complete los datos para registrar una nueva escuela.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Row 1: Name + Level */}
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2'>
              <Label htmlFor='name'>
                Nombre <span className='text-destructive'>*</span>
              </Label>
              <Input
                id='name'
                value={form.name}
                onChange={(e) => updateField('name', e.target.value)}
                placeholder='Nombre de la escuela'
                maxLength={200}
              />
              {errors.name && (
                <p className='text-sm text-destructive'>{errors.name}</p>
              )}
            </div>
            <div className='space-y-2'>
              <Label htmlFor='schoolLevelId'>
                Nivel Escolar <span className='text-destructive'>*</span>
              </Label>
              <Select
                value={form.schoolLevelId}
                onValueChange={(value) => updateField('schoolLevelId', value)}
              >
                <SelectTrigger id='schoolLevelId'>
                  <SelectValue placeholder='Seleccionar nivel' />
                </SelectTrigger>
                <SelectContent>
                  {levels.map((level) => (
                    <SelectItem
                      key={level.schoolLevelId}
                      value={String(level.schoolLevelId)}
                    >
                      {level.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.schoolLevelId && (
                <p className='text-sm text-destructive'>{errors.schoolLevelId}</p>
              )}
            </div>
          </div>

          {/* Row 2: Address */}
          <div className='space-y-2'>
            <Label htmlFor='address'>
              Dirección <span className='text-destructive'>*</span>
            </Label>
            <Input
              id='address'
              value={form.address}
              onChange={(e) => updateField('address', e.target.value)}
              placeholder='Dirección completa'
              maxLength={150}
            />
            {errors.address && (
              <p className='text-sm text-destructive'>{errors.address}</p>
            )}
          </div>

          {/* Row 3: Ciudad + State */}
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2'>
              <Label htmlFor='ciudad'>Ciudad</Label>
              <Input
                id='ciudad'
                value={form.ciudad}
                onChange={(e) => updateField('ciudad', e.target.value)}
                placeholder='Ciudad'
                maxLength={100}
              />
            </div>
            <div className='space-y-2'>
              <Label htmlFor='state'>Estado</Label>
              <Input
                id='state'
                value={form.state}
                onChange={(e) => updateField('state', e.target.value)}
                placeholder='Estado'
                maxLength={30}
              />
            </div>
          </div>

          {/* Row 4: Phone + Email */}
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
            <div className='space-y-2'>
              <Label htmlFor='phoneNumber'>Teléfono</Label>
              <Input
                id='phoneNumber'
                value={form.phoneNumber}
                onChange={(e) => updateField('phoneNumber', e.target.value)}
                placeholder='(000) 000-0000'
                maxLength={50}
              />
            </div>
            <div className='space-y-2'>
              <Label htmlFor='email'>Email</Label>
              <Input
                id='email'
                type='email'
                value={form.email}
                onChange={(e) => updateField('email', e.target.value)}
                placeholder='correo@escuela.edu'
                maxLength={150}
              />
              {errors.email && (
                <p className='text-sm text-destructive'>{errors.email}</p>
              )}
            </div>
          </div>

          {/* Row 5: Principal Info */}
          <div className='space-y-2'>
            <Label htmlFor='principalInfo'>Director / Responsable</Label>
            <Input
              id='principalInfo'
              value={form.principalInfo}
              onChange={(e) => updateField('principalInfo', e.target.value)}
              placeholder='Nombre del director o responsable'
              maxLength={300}
            />
          </div>

          {/* Row 6: Description */}
          <div className='space-y-2'>
            <Label htmlFor='description'>Descripción</Label>
            <Textarea
              id='description'
              value={form.description}
              onChange={(e) => updateField('description', e.target.value)}
              placeholder='Descripción de la escuela'
              maxLength={500}
              rows={3}
            />
          </div>

          {/* Row 7: Logo + Photo uploads */}
          {isEditing ? (
            <div className='grid grid-cols-1 gap-4 sm:grid-cols-2'>
              {/* Logo */}
              <div className='space-y-2'>
                <Label>Logo</Label>
                <div className='border-input bg-background flex flex-col items-center gap-2 rounded-lg border p-3'>
                  {(logoPreview || school?.hasLogo) ? (
                    <img
                      src={logoPreview ?? getSchoolLogoUrl(school!.schoolId)}
                      alt='Logo'
                      className='size-20 rounded-lg border object-contain'
                      onError={(e) => {
                        e.currentTarget.style.display = 'none'
                      }}
                    />
                  ) : (
                    <div className='bg-muted flex size-20 items-center justify-center rounded-lg'>
                      <ImageIcon className='text-muted-foreground size-8' />
                    </div>
                  )}
                  <div className='flex gap-2'>
                    <Button
                      type='button'
                      variant='outline'
                      size='sm'
                      className='gap-1'
                      disabled={isImageUploading}
                      onClick={() => logoInputRef.current?.click()}
                    >
                      <UploadIcon className='size-3' />
                      Subir
                    </Button>
                    {(logoPreview || school?.hasLogo) && (
                      <Button
                        type='button'
                        variant='outline'
                        size='sm'
                        className='text-destructive hover:text-destructive gap-1'
                        disabled={isImageUploading}
                        onClick={() => deleteLogoMutation.mutate()}
                      >
                        <Trash2Icon className='size-3' />
                        Quitar
                      </Button>
                    )}
                  </div>
                  <p className='text-muted-foreground text-xs'>Máx. 2 MB</p>
                  <input
                    ref={logoInputRef}
                    type='file'
                    accept='image/*'
                    className='hidden'
                    onChange={(e) => handleFileSelect(e, 'logo')}
                  />
                </div>
              </div>

              {/* Photo */}
              <div className='space-y-2'>
                <Label>Foto</Label>
                <div className='border-input bg-background flex flex-col items-center gap-2 rounded-lg border p-3'>
                  {(photoPreview || school?.hasPhoto) ? (
                    <img
                      src={photoPreview ?? getSchoolPhotoUrl(school!.schoolId)}
                      alt='Foto'
                      className='h-20 w-full rounded-lg border object-cover'
                      onError={(e) => {
                        e.currentTarget.style.display = 'none'
                      }}
                    />
                  ) : (
                    <div className='bg-muted flex h-20 w-full items-center justify-center rounded-lg'>
                      <CameraIcon className='text-muted-foreground size-8' />
                    </div>
                  )}
                  <div className='flex gap-2'>
                    <Button
                      type='button'
                      variant='outline'
                      size='sm'
                      className='gap-1'
                      disabled={isImageUploading}
                      onClick={() => photoInputRef.current?.click()}
                    >
                      <UploadIcon className='size-3' />
                      Subir
                    </Button>
                    {(photoPreview || school?.hasPhoto) && (
                      <Button
                        type='button'
                        variant='outline'
                        size='sm'
                        className='text-destructive hover:text-destructive gap-1'
                        disabled={isImageUploading}
                        onClick={() => deletePhotoMutation.mutate()}
                      >
                        <Trash2Icon className='size-3' />
                        Quitar
                      </Button>
                    )}
                  </div>
                  <p className='text-muted-foreground text-xs'>Máx. 5 MB</p>
                  <input
                    ref={photoInputRef}
                    type='file'
                    accept='image/*'
                    className='hidden'
                    onChange={(e) => handleFileSelect(e, 'photo')}
                  />
                </div>
              </div>
            </div>
          ) : (
            <p className='text-muted-foreground text-xs italic'>
              Las imágenes (logo y foto) podrán ser cargadas después de crear la escuela.
            </p>
          )}

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
                  : 'Crear Escuela'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
