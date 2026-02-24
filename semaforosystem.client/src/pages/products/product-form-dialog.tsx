import { useState, useMemo } from 'react'
import { useQueries, useQueryClient } from '@tanstack/react-query'
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
import { Badge } from '@/components/ui/badge'
import { Checkbox } from '@/components/ui/checkbox'
import { Separator } from '@/components/ui/separator'

import type { ProductResponse, CreateProductRequest } from '@/services/product-service'
import {
  createProduct,
  updateProduct,
  createProductPrice,
  updateProductPrice,
  deleteProductPrice,
  getProductPrices,
} from '@/services/product-service'
import {
  useBrands,
  useCategories,
  useProductPrices,
  productKeys,
} from '@/hooks/use-products'
import { useSizeSystemsLookup, useSizesBySystem } from '@/hooks/use-sizes'
import { useVariantSystemsLookup, variantKeys } from '@/hooks/use-variants'
import { getVariantsBySystem } from '@/services/variant-service'
import type { VariantResponse } from '@/services/variant-service'
import { ApiError } from '@/lib/api-client'

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
  sizeSystemId: string
  categoryIds: number[]
  variantSystemIds: number[]
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
  sizeSystemId: '',
  categoryIds: [],
  variantSystemIds: [],
}

// ── Component ────────────────────────────────────────────

export default function ProductFormDialog({
  open,
  onOpenChange,
  product,
}: ProductFormDialogProps) {
  const isEditing = !!product
  const queryClient = useQueryClient()

  const [form, setForm] = useState<FormData>(emptyForm)
  const [errors, setErrors] = useState<Partial<Record<keyof FormData, string>>>({})
  const [prevOpen, setPrevOpen] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  // ── Price state ───────────────────────────────────
  const [priceSizesChecked, setPriceSizesChecked] = useState(false)
  const [priceVsChecks, setPriceVsChecks] = useState<number[]>([])
  const [priceValues, setPriceValues] = useState<Record<string, string>>({})
  const [pricesPopulated, setPricesPopulated] = useState(false)
  const [updatePrices, setUpdatePrices] = useState(false)

  // ── Lookups ───────────────────────────────────────
  const { data: brands = [] } = useBrands()
  const { data: categories = [] } = useCategories()
  const { data: sizeSystems = [] } = useSizeSystemsLookup()
  const { data: variantSystems = [] } = useVariantSystemsLookup()

  // Fetch sizes for the selected size system
  const selectedSizeSystemId = form.sizeSystemId ? Number(form.sizeSystemId) : null
  const { data: sizes = [] } = useSizesBySystem(selectedSizeSystemId)

  // Fetch variants for each assigned variant system
  const variantQueries = useQueries({
    queries: form.variantSystemIds.map((vsId) => ({
      queryKey: [...variantKeys.bySystem(vsId)],
      queryFn: () => getVariantsBySystem(vsId),
      staleTime: 10 * 60 * 1000,
    })),
  })

  // Build variant map: systemId → variants[]
  const variantMap = useMemo(() => {
    const map = new Map<number, VariantResponse[]>()
    form.variantSystemIds.forEach((vsId, idx) => {
      const q = variantQueries[idx]
      if (q?.data) map.set(vsId, q.data)
    })
    return map
  }, [form.variantSystemIds, variantQueries])

  // Fetch existing prices (edit mode)
  const pricesQuery = useProductPrices(isEditing ? product?.productId : null)
  const existingPrices = pricesQuery.data ?? []

  // ── Populate form when dialog opens ───────────────

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
            sizeSystemId: product.sizeSystemId != null ? String(product.sizeSystemId) : '',
            categoryIds: product.categories.map((c) => c.categoryId),
            variantSystemIds: product.variantSystems.map((vs) => vs.productVariantId),
          }
        : emptyForm,
    )
    setErrors({})
    setPriceSizesChecked(false)
    setPriceVsChecks([])
    setPriceValues({})
    setPricesPopulated(false)
    setUpdatePrices(false)
  }
  if (!open && prevOpen) {
    setPrevOpen(false)
  }

  // Populate prices from existing data once loaded (edit mode)
  const allVariantsLoaded =
    variantQueries.length === 0 || variantQueries.every((q) => q.isSuccess)

  if (open && isEditing && !pricesPopulated && pricesQuery.isSuccess && allVariantsLoaded) {
    setPricesPopulated(true)
    const values: Record<string, string> = {}
    let sizesUsed = false
    const vsUsed = new Set<number>()

    for (const pp of existingPrices) {
      if (pp.sizeId != null && pp.variantId == null) {
        values[`s${pp.sizeId}`] = String(pp.price)
        sizesUsed = true
      } else if (pp.sizeId != null && pp.variantId != null) {
        values[`s${pp.sizeId}_v${pp.variantId}`] = String(pp.price)
        sizesUsed = true
        for (const [vsId, variants] of variantMap) {
          if (variants.some((v) => v.productVariantId === pp.variantId)) {
            vsUsed.add(vsId)
          }
        }
      } else if (pp.sizeId == null && pp.variantId != null) {
        values[`v${pp.variantId}`] = String(pp.price)
        for (const [vsId, variants] of variantMap) {
          if (variants.some((v) => v.productVariantId === pp.variantId)) {
            vsUsed.add(vsId)
          }
        }
      }
    }

    if (sizesUsed) setPriceSizesChecked(true)
    if (vsUsed.size > 0) setPriceVsChecks(Array.from(vsUsed))
    if (Object.keys(values).length > 0) setPriceValues(values)
  }

  // ── Price fields generation ───────────────────────

  const priceFields = useMemo(() => {
    const fields: Array<{
      key: string
      label: string
      sizeId: number | null
      variantId: number | null
    }> = []

    const checkedVariants: VariantResponse[] = []
    for (const vsId of priceVsChecks) {
      const variants = variantMap.get(vsId)
      if (variants) checkedVariants.push(...variants)
    }

    const hasSizes = priceSizesChecked && sizes.length > 0

    if (hasSizes && checkedVariants.length === 0) {
      // Sizes only
      for (const size of sizes) {
        fields.push({
          key: `s${size.sizeId}`,
          label: size.sizeValue,
          sizeId: size.sizeId,
          variantId: null,
        })
      }
    } else if (!hasSizes && checkedVariants.length > 0) {
      // Variants only
      for (const variant of checkedVariants) {
        fields.push({
          key: `v${variant.productVariantId}`,
          label: variant.variantValue,
          sizeId: null,
          variantId: variant.productVariantId,
        })
      }
    } else if (hasSizes && checkedVariants.length > 0) {
      // Size × Variant
      for (const size of sizes) {
        for (const variant of checkedVariants) {
          fields.push({
            key: `s${size.sizeId}_v${variant.productVariantId}`,
            label: `${size.sizeValue} - ${variant.variantValue}`,
            sizeId: size.sizeId,
            variantId: variant.productVariantId,
          })
        }
      }
    }

    return fields
  }, [priceSizesChecked, priceVsChecks, sizes, variantMap])

  // ── Validation ────────────────────────────────────

  function validate(): boolean {
    const newErrors: Partial<Record<keyof FormData, string>> = {}
    if (!form.name.trim()) newErrors.name = 'El nombre es requerido'
    setErrors(newErrors)
    return Object.keys(newErrors).length === 0
  }

  // ── Price sync ────────────────────────────────────

  async function savePrices(productId: number) {
    const currentPrices = await getProductPrices(productId)

    const desired = new Map<
      string,
      { sizeId: number | null; variantId: number | null; price: number }
    >()
    for (const field of priceFields) {
      const val = priceValues[field.key]
      if (val && val.trim() !== '') {
        const price = parseFloat(val)
        if (!isNaN(price) && price >= 0) {
          desired.set(field.key, {
            sizeId: field.sizeId,
            variantId: field.variantId,
            price,
          })
        }
      }
    }

    const ops: Promise<unknown>[] = []

    if (updatePrices) {
      // Insert new records for every price (keeps history)
      for (const [, entry] of desired) {
        const existing = currentPrices.find(
          (p) => p.sizeId === entry.sizeId && p.variantId === entry.variantId,
        )
        // Only insert when value actually changed
        if (!existing || existing.price !== entry.price) {
          ops.push(
            createProductPrice(productId, {
              price: entry.price,
              sizeId: entry.sizeId,
              variantId: entry.variantId,
            }),
          )
        }
      }
    } else {
      // Default: update existing records in-place
      const matched = new Set<number>()

      for (const [, entry] of desired) {
        const existing = currentPrices.find(
          (p) => p.sizeId === entry.sizeId && p.variantId === entry.variantId,
        )
        if (existing) {
          matched.add(existing.priceId)
          if (existing.price !== entry.price) {
            ops.push(
              updateProductPrice(productId, existing.priceId, { price: entry.price }),
            )
          }
        } else {
          ops.push(
            createProductPrice(productId, {
              price: entry.price,
              sizeId: entry.sizeId,
              variantId: entry.variantId,
            }),
          )
        }
      }

      // Delete prices in scope that no longer have values
      for (const existing of currentPrices) {
        if (matched.has(existing.priceId)) continue
        if (existing.productComboId != null) continue

        const inGridScope = priceFields.some(
          (f) => f.sizeId === (existing.sizeId ?? null) && f.variantId === (existing.variantId ?? null),
        )

        if (inGridScope) {
          ops.push(deleteProductPrice(productId, existing.priceId))
        }
      }
    }

    await Promise.all(ops)
  }

  // ── Submit ────────────────────────────────────────

  async function handleSubmit(e: React.FormEvent) {
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
      sizeSystemId: form.sizeSystemId ? Number(form.sizeSystemId) : null,
      categoryIds: form.categoryIds,
      variantSystemIds: form.variantSystemIds,
    }

    try {
      setSubmitting(true)
      let productId: number

      if (isEditing) {
        await updateProduct(product!.productId, payload)
        productId = product!.productId
      } else {
        const result = await createProduct(payload)
        productId = result.productId
      }

      if (priceFields.length > 0) {
        await savePrices(productId)
      }

      queryClient.invalidateQueries({ queryKey: productKeys.all })
      toast.success(
        isEditing ? 'Producto actualizado exitosamente' : 'Producto creado exitosamente',
      )
      onOpenChange(false)
    } catch (error) {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al guardar el producto')
      } else {
        toast.error('Error al guardar el producto')
      }
    } finally {
      setSubmitting(false)
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

  function toggleVariantSystem(vsId: number) {
    setForm((prev) => ({
      ...prev,
      variantSystemIds: prev.variantSystemIds.includes(vsId)
        ? prev.variantSystemIds.filter((id) => id !== vsId)
        : [...prev.variantSystemIds, vsId],
    }))
  }

  function togglePriceVsCheck(vsId: number) {
    setPriceVsChecks((prev) =>
      prev.includes(vsId) ? prev.filter((id) => id !== vsId) : [...prev, vsId],
    )
  }

  function setPriceValue(key: string, value: string) {
    setPriceValues((prev) => ({ ...prev, [key]: value }))
  }

  const showPricing = !!form.sizeSystemId || form.variantSystemIds.length > 0

  // ── Render ────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-h-[90vh] overflow-y-auto sm:max-w-[800px]'>
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
              disabled={submitting}
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
                disabled={submitting}
              />
            </div>
            <div className='space-y-2'>
              <Label htmlFor='model'>Modelo</Label>
              <Input
                id='model'
                value={form.model}
                onChange={(e) => setField('model', e.target.value)}
                placeholder='Modelo'
                disabled={submitting}
              />
            </div>
          </div>

          {/* Brand + Size System row */}
          <div className='grid grid-cols-2 gap-4'>
            <div className='space-y-2'>
              <Label>Marca</Label>
              <Select
                value={form.brandId}
                onValueChange={(v) => setField('brandId', v === 'none' ? '' : v)}
                disabled={submitting}
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
            <div className='space-y-2'>
              <Label>Sistema de Tallas</Label>
              <Select
                value={form.sizeSystemId}
                onValueChange={(v) => setField('sizeSystemId', v === 'none' ? '' : v)}
                disabled={submitting}
              >
                <SelectTrigger>
                  <SelectValue placeholder='Seleccionar sistema' />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='none'>Sin sistema de tallas</SelectItem>
                  {sizeSystems.map((ss) => (
                    <SelectItem key={ss.sizeSystemId} value={String(ss.sizeSystemId)}>
                      {ss.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
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
                disabled={submitting}
              />
            </div>
            <div className='flex items-end space-x-2 pb-1'>
              <Checkbox
                id='serialize'
                checked={form.serialize}
                onCheckedChange={(checked) =>
                  setField('serialize', checked === true)
                }
                disabled={submitting}
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
              disabled={submitting}
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
              disabled={submitting}
            />
          </div>

          {/* Categories + Variant Systems side by side */}
          <div className='grid grid-cols-2 gap-4'>
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
                      disabled={submitting}
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

            {/* Variant Systems */}
            <div className='space-y-2'>
              <Label>Sistemas de Variantes</Label>
              {form.variantSystemIds.length > 0 && (
                <div className='flex flex-wrap gap-1 mb-2'>
                  {form.variantSystemIds.map((id) => {
                    const vs = variantSystems.find((v) => v.productVariantId === id)
                    return (
                      <Badge
                        key={id}
                        variant='secondary'
                        className='cursor-pointer'
                        onClick={() => toggleVariantSystem(id)}
                      >
                        {vs?.name ?? id} ×
                      </Badge>
                    )
                  })}
                </div>
              )}
              <div className='max-h-[120px] overflow-y-auto rounded-md border p-2 space-y-1'>
                {variantSystems.map((vs) => (
                  <div key={vs.productVariantId} className='flex items-center space-x-2'>
                    <Checkbox
                      id={`vs-${vs.productVariantId}`}
                      checked={form.variantSystemIds.includes(vs.productVariantId)}
                      onCheckedChange={() => toggleVariantSystem(vs.productVariantId)}
                      disabled={submitting}
                    />
                    <Label
                      htmlFor={`vs-${vs.productVariantId}`}
                      className='text-sm font-normal'
                    >
                      {vs.name}
                    </Label>
                  </div>
                ))}
                {variantSystems.length === 0 && (
                  <p className='text-muted-foreground text-xs'>No hay sistemas de variantes disponibles.</p>
                )}
              </div>
            </div>
          </div>

          {/* ── Precios section ── */}
          {showPricing && (
            <>
              <Separator />
              <div className='space-y-3'>
                <Label className='text-base font-semibold'>Precios</Label>

                {/* Dimension checkboxes */}
                <div className='flex flex-wrap gap-4'>
                  {!!form.sizeSystemId && (
                    <div className='flex items-center space-x-2'>
                      <Checkbox
                        id='price-sizes'
                        checked={priceSizesChecked}
                        onCheckedChange={(checked) =>
                          setPriceSizesChecked(checked === true)
                        }
                        disabled={submitting}
                      />
                      <Label htmlFor='price-sizes' className='text-sm font-normal'>
                        Tallas
                      </Label>
                    </div>
                  )}

                  {form.variantSystemIds.map((vsId) => {
                    const vs = variantSystems.find(
                      (v) => v.productVariantId === vsId,
                    )
                    return (
                      <div key={vsId} className='flex items-center space-x-2'>
                        <Checkbox
                          id={`price-vs-${vsId}`}
                          checked={priceVsChecks.includes(vsId)}
                          onCheckedChange={() => togglePriceVsCheck(vsId)}
                          disabled={submitting}
                        />
                        <Label
                          htmlFor={`price-vs-${vsId}`}
                          className='text-sm font-normal'
                        >
                          {vs?.name ?? `Sistema ${vsId}`}
                        </Label>
                      </div>
                    )
                  })}
                </div>

                {/* Price fields grid */}
                {priceFields.length > 0 && (
                  <div className='grid grid-cols-3 gap-3 rounded-md border p-3 max-h-[220px] overflow-y-auto'>
                    {priceFields.map((field) => (
                      <div key={field.key} className='space-y-1'>
                        <Label className='text-muted-foreground text-xs'>
                          {field.label}
                        </Label>
                        <Input
                          type='number'
                          step='0.01'
                          min='0'
                          placeholder='0.00'
                          value={priceValues[field.key] ?? ''}
                          onChange={(e) =>
                            setPriceValue(field.key, e.target.value)
                          }
                          disabled={submitting}
                          className='h-8 text-sm'
                        />
                      </div>
                    ))}
                  </div>
                )}

                {priceSizesChecked && sizes.length === 0 && (
                  <p className='text-muted-foreground text-xs'>
                    El sistema de tallas seleccionado no tiene tallas registradas.
                  </p>
                )}

                {/* Actualizar Precios toggle */}
                {priceFields.length > 0 && (
                  <div className='flex items-center space-x-2 pt-1'>
                    <Checkbox
                      id='update-prices'
                      checked={updatePrices}
                      onCheckedChange={(checked) =>
                        setUpdatePrices(checked === true)
                      }
                      disabled={submitting}
                    />
                    <Label htmlFor='update-prices' className='text-sm font-normal'>
                      Actualizar Precios
                    </Label>
                    <span className='text-muted-foreground text-xs'>
                      {updatePrices
                        ? '(Insertará nuevos registros para mantener historial)'
                        : '(Sobrescribirá los precios actuales)'}
                    </span>
                  </div>
                )}
              </div>
            </>
          )}

          <DialogFooter>
            <Button
              type='button'
              variant='outline'
              onClick={() => onOpenChange(false)}
              disabled={submitting}
            >
              Cancelar
            </Button>
            <Button type='submit' disabled={submitting}>
              {submitting
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
