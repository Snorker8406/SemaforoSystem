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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'

import type { ProductResponse, CreateProductRequest } from '@/services/product-service'
import { useBrands, useCategories, useCreateProduct, useUpdateProduct } from '@/hooks/use-products'

// ── Types ────────────────────────────────────────────────

interface ProductFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  product?: ProductResponse | null
}

interface FormData {
  name: string
  barcode: string
  description: string
  model: string
  comments: string
  serialCount: string
  serialize: boolean
  brandId: string
  categoryIds: number[]
}

const emptyForm: FormData = {
  name: '',
  barcode: '',
  description: '',
  model: '',
  comments: '',
  serialCount: '',
  serialize: false,
  brandId: '',
  categoryIds: [],
}

// ── Component ────────────────────────────────────────────

export default function ProductFormDialog({
  open,
  onOpenChange,
  product,
}: ProductFormDialogProps) {
  const isEditing = !!product

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)

  // Fetch lookups via hooks
  const { data: brands = [] } = useBrands()
  const { data: categories = [] } = useCategories()

  // Populate form when dialog opens (adjust state during render — React 19 pattern)
  if (open && !prevOpen) {
    setPrevOpen(true)
    setForm(
      product
        ? {
            name: product.name ?? '',
            barcode: product.barcode ?? '',
            description: product.description ?? '',
            model: product.model ?? '',
            comments: product.comments ?? '',
            serialCount: product.serialCount != null ? String(product.serialCount) : '',
            serialize: product.serialize ?? false,
            brandId: product.brandId != null ? String(product.brandId) : '',
            categoryIds: product.categories.map((c) => c.categoryId),
          }
        : emptyForm,
    )
    setErrors({})
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // ── Mutations ─────────────────────────────────────

  const createMutation = useCreateProduct({
    onSuccess: () => onOpenChange(false),
  })

  const updateMutation = useUpdateProduct({
    onSuccess: () => onOpenChange(false),
  })

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}

    if (!form.name.trim()) newErrors.name = 'El nombre es requerido'

    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Submit ────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!validate()) return

    const payload: CreateProductRequest = {
      name: form.name.trim() || null,
      barcode: form.barcode.trim() || null,
      description: form.description.trim() || null,
      model: form.model.trim() || null,
      comments: form.comments.trim() || null,
      serialCount: form.serialCount ? Number(form.serialCount) : null,
      serialize: form.serialize,
      brandId: form.brandId ? Number(form.brandId) : null,
      categoryIds: form.categoryIds,
    }

    if (isEditing) {
      updateMutation.mutate({ id: product!.productId, data: payload })
    } else {
      createMutation.mutate(payload)
    }
  }

  // ── Helpers ───────────────────────────────────────

  function setField<K extends keyof FormData>(key: K, value: FormData[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
    if (errors[key]) setErrors((prev) => ({ ...prev, [key]: undefined }))
  }

  function toggleCategory(categoryId: number) {
    setForm((prev) => ({
      ...prev,
      categoryIds: prev.categoryIds.includes(categoryId)
        ? prev.categoryIds.filter((id) => id !== categoryId)
        : [...prev.categoryIds, categoryId],
    }))
  }

  // ── Render ────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-h-[90vh] overflow-y-auto sm:max-w-[550px]'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar Producto' : 'Nuevo Producto'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifica los campos del producto.'
              : 'Completa los datos para crear un nuevo producto.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-4'>
          {/* Name */}
          <div className='space-y-2'>
            <Label htmlFor='name'>Nombre *</Label>
            <Input
              id='name'
              value={form.name}
              onChange={(e) => setField('name', e.target.value)}
              placeholder='Nombre del producto'
              disabled={isPending}
            />
            {errors.name && (
              <p className='text-destructive text-xs'>{errors.name}</p>
            )}
          </div>

          {/* Barcode + Model row */}
          <div className='grid grid-cols-2 gap-4'>
            <div className='space-y-2'>
              <Label htmlFor='barcode'>Código de barras</Label>
              <Input
                id='barcode'
                value={form.barcode}
                onChange={(e) => setField('barcode', e.target.value)}
                placeholder='Código'
                disabled={isPending}
              />
            </div>
            <div className='space-y-2'>
              <Label htmlFor='model'>Modelo</Label>
              <Input
                id='model'
                value={form.model}
                onChange={(e) => setField('model', e.target.value)}
                placeholder='Modelo'
                disabled={isPending}
              />
            </div>
          </div>

          {/* Brand */}
          <div className='space-y-2'>
            <Label>Marca</Label>
            <Select
              value={form.brandId}
              onValueChange={(v) => setField('brandId', v === 'none' ? '' : v)}
              disabled={isPending}
            >
              <SelectTrigger>
                <SelectValue placeholder='Seleccionar marca' />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value='none'>Sin marca</SelectItem>
                {brands.map((b) => (
                  <SelectItem key={b.brandId} value={String(b.brandId)}>
                    {b.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Serial count + Serialize row */}
          <div className='grid grid-cols-2 gap-4'>
            <div className='space-y-2'>
              <Label htmlFor='serialCount'>Cantidad (Series)</Label>
              <Input
                id='serialCount'
                type='number'
                value={form.serialCount}
                onChange={(e) => setField('serialCount', e.target.value)}
                placeholder='0'
                disabled={isPending}
              />
            </div>
            <div className='flex items-end space-x-2 pb-1'>
              <Checkbox
                id='serialize'
                checked={form.serialize}
                onCheckedChange={(checked) =>
                  setField('serialize', checked === true)
                }
                disabled={isPending}
              />
              <Label htmlFor='serialize' className='text-sm font-normal'>
                Serializar
              </Label>
            </div>
          </div>

          {/* Description */}
          <div className='space-y-2'>
            <Label htmlFor='description'>Descripción</Label>
            <Textarea
              id='description'
              value={form.description}
              onChange={(e) => setField('description', e.target.value)}
              placeholder='Descripción del producto'
              rows={2}
              disabled={isPending}
            />
          </div>

          {/* Comments */}
          <div className='space-y-2'>
            <Label htmlFor='comments'>Comentarios</Label>
            <Textarea
              id='comments'
              value={form.comments}
              onChange={(e) => setField('comments', e.target.value)}
              placeholder='Comentarios adicionales'
              rows={2}
              disabled={isPending}
            />
          </div>

          {/* Categories */}
          <div className='space-y-2'>
            <Label>Categorías</Label>
            {form.categoryIds.length > 0 && (
              <div className='flex flex-wrap gap-1 mb-2'>
                {form.categoryIds.map((id) => {
                  const cat = categories.find((c) => c.categoryId === id)
                  return (
                    <Badge
                      key={id}
                      variant='secondary'
                      className='cursor-pointer'
                      onClick={() => toggleCategory(id)}
                    >
                      {cat?.name ?? id} ×
                    </Badge>
                  )
                })}
              </div>
            )}
            <div className='max-h-[120px] overflow-y-auto rounded-md border p-2 space-y-1'>
              {categories.map((cat) => (
                <div key={cat.categoryId} className='flex items-center space-x-2'>
                  <Checkbox
                    id={`cat-${cat.categoryId}`}
                    checked={form.categoryIds.includes(cat.categoryId)}
                    onCheckedChange={() => toggleCategory(cat.categoryId)}
                    disabled={isPending}
                  />
                  <Label
                    htmlFor={`cat-${cat.categoryId}`}
                    className='text-sm font-normal'
                  >
                    {cat.name}
                  </Label>
                </div>
              ))}
              {categories.length === 0 && (
                <p className='text-muted-foreground text-xs'>No hay categorías disponibles.</p>
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
                  ? 'Guardar cambios'
                  : 'Crear producto'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
