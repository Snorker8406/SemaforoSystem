import { useState, useCallback, useMemo } from 'react'
import { useQueries } from '@tanstack/react-query'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

import {
  SearchIcon,
  XIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ImageOffIcon,
  PackageIcon,
  PackagePlusIcon,
  BarcodeIcon,
  CheckIcon,
  LoaderIcon,
  MapPinIcon,
} from 'lucide-react'

import type { ProductQueryParams, ProductResponse } from '@/services/product-service'
import { getProductPictureUrl } from '@/services/product-service'
import type { VariantResponse } from '@/services/variant-service'
import { getVariantsBySystem } from '@/services/variant-service'
import type { CreateEntryLineRequest } from '@/services/inventory-service'
import { useProducts } from '@/hooks/use-products'
import { useProductSchools } from '@/hooks/use-products'
import { useEmbroideries } from '@/hooks/use-embroideries'
import { useSizesBySystem } from '@/hooks/use-sizes'
import { variantKeys } from '@/hooks/use-variants'
import { useSites, useCreateEntry } from '@/hooks/use-inventory'

// ── Product Image ────────────────────────────────────────

function ProductImage({
  productId,
  hasPicture,
  size = 'sm',
}: {
  productId: number
  hasPicture: boolean
  size?: 'sm' | 'md'
}) {
  const [error, setError] = useState(false)
  const sizeClass = size === 'md' ? 'size-16' : 'size-10'

  if (!hasPicture || error) {
    return (
      <div className={`${sizeClass} bg-muted flex items-center justify-center rounded-md border`}>
        <ImageOffIcon className='text-muted-foreground size-4' />
      </div>
    )
  }

  return (
    <img
      src={getProductPictureUrl(productId)}
      alt='Producto'
      className={`${sizeClass} rounded-md border object-cover`}
      onError={() => setError(true)}
    />
  )
}

// ── Selected product type ────────────────────────────────

interface VariantSystemRef {
  productVariantId: number
  name: string
}

interface SelectedProduct {
  productId: number
  name: string
  model: string | null
  brandName: string | null
  hasPicture: boolean
  serialize: boolean
  sizeSystemId: number | null
  sizeSystemName: string | null
  schoolCount: number
  variantSystems: VariantSystemRef[]
}

// ── Per-product entry state ──────────────────────────────

interface ProductEntryState {
  sizesChecked: boolean
  variantChecks: number[]
  quantities: Record<string, string>
  embroideryChecked: boolean
  embroideryMode: 'escolar' | 'todos'
  embroiderySearch: string
  selectedSchoolLevelName: string | null
  selectedSchoolId: number | null
  selectedEmbroideryIds: number[]
}

// ── Tab Content Component ────────────────────────────────

function ProductTabContent({
  product,
  entryState,
  onEntryStateChange,
  siteId,
  onSubmit,
  isSubmitting,
}: {
  product: SelectedProduct
  entryState: ProductEntryState
  onEntryStateChange: (state: ProductEntryState) => void
  siteId: number | null
  onSubmit: (lines: CreateEntryLineRequest[]) => void
  isSubmitting: boolean
}) {
  const { sizesChecked, variantChecks, quantities, embroideryChecked, embroideryMode, embroiderySearch, selectedSchoolLevelName, selectedSchoolId, selectedEmbroideryIds } = entryState

  // Fetch schools for this product (only when embroidery is checked + escolar mode)
  const { data: schools = [], isLoading: schoolsLoading } = useProductSchools(
    embroideryChecked && embroideryMode === 'escolar' ? product.productId : null,
  )

  // Derive unique school levels from schools data
  const schoolLevels = useMemo(() => {
    const seen = new Map<string, string>()
    for (const s of schools) {
      if (!seen.has(s.schoolLevelName)) seen.set(s.schoolLevelName, s.schoolLevelName)
    }
    return [...seen.values()]
  }, [schools])

  // Auto-select when only 1 option in school levels
  const effectiveLevelName = schoolLevels.length === 1 ? schoolLevels[0] : selectedSchoolLevelName
  const levelReadOnly = schoolLevels.length === 1

  // Auto-select when only 1 school in filtered list
  const schoolsForLevel = useMemo(() => {
    if (!effectiveLevelName) return []
    return schools.filter((s) => s.schoolLevelName === effectiveLevelName)
  }, [schools, effectiveLevelName])
  const effectiveSchoolId = schoolsForLevel.length === 1 ? schoolsForLevel[0].schoolId : selectedSchoolId
  const schoolReadOnly = schoolsForLevel.length === 1

  // Sync auto-selections back to state
  // (We do this in a callback effect-like manner to avoid stale state)
  const needsLevelSync = embroideryChecked && embroideryMode === 'escolar' && !schoolsLoading && schoolLevels.length === 1 && selectedSchoolLevelName !== schoolLevels[0]
  const needsSchoolSync = embroideryChecked && embroideryMode === 'escolar' && !schoolsLoading && schoolsForLevel.length === 1 && selectedSchoolId !== schoolsForLevel[0].schoolId

  if (needsLevelSync || needsSchoolSync) {
    const updates: Partial<ProductEntryState> = {}
    if (needsLevelSync) updates.selectedSchoolLevelName = schoolLevels[0]
    if (needsSchoolSync) updates.selectedSchoolId = schoolsForLevel[0].schoolId
    onEntryStateChange({ ...entryState, ...updates })
  }

  // Fetch embroideries — "escolar" mode: by school, "todos" mode: by name search
  const { data: embroideriesData, isLoading: embroideriesLoading } = useEmbroideries(
    embroideryChecked && embroideryMode === 'escolar' && effectiveSchoolId != null
      ? { schoolId: effectiveSchoolId, pageSize: 100, sortBy: 'name' }
      : embroideryChecked && embroideryMode === 'todos' && embroiderySearch.trim().length >= 2
        ? { search: embroiderySearch.trim(), pageSize: 100, sortBy: 'name' }
        : { schoolId: -1 }, // impossible ID → won't match anything
  )
  const embroideries =
    embroideryChecked &&
    ((embroideryMode === 'escolar' && effectiveSchoolId != null) ||
     (embroideryMode === 'todos' && embroiderySearch.trim().length >= 2))
      ? (embroideriesData?.items ?? [])
      : []

  // Fetch sizes for this product's size system
  const { data: sizes = [] } = useSizesBySystem(product.sizeSystemId)

  // Fetch variants for each variant system
  const variantQueries = useQueries({
    queries: product.variantSystems.map((vs) => ({
      queryKey: [...variantKeys.bySystem(vs.productVariantId)],
      queryFn: () => getVariantsBySystem(vs.productVariantId),
      staleTime: 10 * 60 * 1000,
    })),
  })

  // Build variant map: systemId → variants[]
  const variantMap = useMemo(() => {
    const map = new Map<number, VariantResponse[]>()
    product.variantSystems.forEach((vs, idx) => {
      const q = variantQueries[idx]
      if (q?.data) map.set(vs.productVariantId, q.data)
    })
    return map
  }, [product.variantSystems, variantQueries])

  // Generate fields based on selections (like pricing)
  const fields = useMemo(() => {
    const result: Array<{
      key: string
      label: string
      sizeId: number | null
      variantId: number | null
    }> = []

    const checkedVariants: VariantResponse[] = []
    for (const vsId of variantChecks) {
      const variants = variantMap.get(vsId)
      if (variants) checkedVariants.push(...variants)
    }

    const hasSizes = sizesChecked && sizes.length > 0

    if (hasSizes && checkedVariants.length === 0) {
      for (const size of sizes) {
        result.push({
          key: `s${size.sizeId}`,
          label: size.sizeValue,
          sizeId: size.sizeId,
          variantId: null,
        })
      }
    } else if (!hasSizes && checkedVariants.length > 0) {
      for (const variant of checkedVariants) {
        result.push({
          key: `v${variant.productVariantId}`,
          label: variant.variantValue,
          sizeId: null,
          variantId: variant.productVariantId,
        })
      }
    } else if (hasSizes && checkedVariants.length > 0) {
      for (const size of sizes) {
        for (const variant of checkedVariants) {
          result.push({
            key: `s${size.sizeId}_v${variant.productVariantId}`,
            label: `${size.sizeValue} — ${variant.variantValue}`,
            sizeId: size.sizeId,
            variantId: variant.productVariantId,
          })
        }
      }
    }

    return result
  }, [sizesChecked, variantChecks, sizes, variantMap])

  // ── Handlers ───────────────────────────────────────

  function toggleSizes(checked: boolean) {
    onEntryStateChange({ ...entryState, sizesChecked: checked })
  }

  function toggleVariantCheck(vsId: number) {
    const next = variantChecks.includes(vsId)
      ? variantChecks.filter((id) => id !== vsId)
      : [...variantChecks, vsId]
    onEntryStateChange({ ...entryState, variantChecks: next })
  }

  function setQuantity(key: string, value: string) {
    onEntryStateChange({
      ...entryState,
      quantities: { ...quantities, [key]: value },
    })
  }

  // ── Build lines for submission ─────────────────────

  function handleSubmit() {
    if (!siteId) return

    const lines: CreateEntryLineRequest[] = []

    if (fields.length > 0) {
      // Dimensional: one line per field with qty > 0
      for (const field of fields) {
        const raw = quantities[field.key]
        const qty = raw ? parseInt(raw, 10) : 0
        if (qty <= 0) continue

        lines.push({
          productId: product.productId,
          sizeId: field.sizeId ?? undefined,
          variantIds: field.variantId ? [field.variantId] : undefined,
          isSerialized: product.serialize,
          quantity: qty,
        })
      }
    } else {
      // Single quantity (no dimensions)
      const raw = quantities['single']
      const qty = raw ? parseInt(raw, 10) : 0
      if (qty > 0) {
        lines.push({
          productId: product.productId,
          isSerialized: product.serialize,
          quantity: qty,
        })
      }
    }

    if (lines.length === 0) return
    onSubmit(lines)
  }

  const hasDimensions = product.sizeSystemId != null || product.variantSystems.length > 0

  // Compute total for submit button
  const totalQty = useMemo(() => {
    if (fields.length > 0) {
      return fields.reduce((sum, f) => {
        const raw = quantities[f.key]
        return sum + (raw ? parseInt(raw, 10) || 0 : 0)
      }, 0)
    }
    const raw = quantities['single']
    return raw ? parseInt(raw, 10) || 0 : 0
  }, [fields, quantities])

  return (
    <Card className='h-full overflow-auto'>
      <CardHeader className='pb-3'>
        <div className='flex items-center gap-3'>
          <ProductImage productId={product.productId} hasPicture={product.hasPicture} size='md' />
          <div className='flex-1 min-w-0'>
            <div className='flex items-center gap-2 flex-wrap'>
              <CardTitle className='text-lg'>{product.name}</CardTitle>
              <Badge
                variant={product.serialize ? 'default' : 'outline'}
                className='text-xs gap-1'
              >
                <BarcodeIcon className='size-3' />
                {product.serialize ? 'Serializado' : 'No serializado'}
              </Badge>
            </div>
            <div className='flex items-center gap-2 mt-0.5'>
              {product.model && (
                <span className='text-muted-foreground text-sm'>{product.model}</span>
              )}
              {product.brandName && (
                <Badge variant='outline' className='text-xs'>
                  {product.brandName}
                </Badge>
              )}
            </div>
          </div>
        </div>
      </CardHeader>

      <CardContent className='space-y-4'>
        {/* ── Embroidery + School selection ── */}
        <div className='space-y-3'>
          <div className='flex items-center gap-3'>
            <Checkbox
              id={`embroidery-${product.productId}`}
              checked={embroideryChecked}
              onCheckedChange={(checked) =>
                onEntryStateChange({
                  ...entryState,
                  embroideryChecked: checked === true,
                  // Reset selects when unchecking
                  ...(!checked && { selectedSchoolLevelName: null, selectedSchoolId: null, selectedEmbroideryIds: [], embroiderySearch: '' }),
                })
              }
            />
            <Label htmlFor={`embroidery-${product.productId}`} className='text-sm font-normal'>
              Bordado
            </Label>

            {embroideryChecked && (
              <div className='flex items-center rounded-md border bg-muted/40 p-0.5 ml-2'>
                <button
                  type='button'
                  className={`px-3 py-1 text-xs font-medium rounded transition-colors ${
                    embroideryMode === 'escolar'
                      ? 'bg-background shadow-sm text-foreground'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                  onClick={() => {
                    if (embroideryMode !== 'escolar') {
                      onEntryStateChange({
                        ...entryState,
                        embroideryMode: 'escolar',
                        embroiderySearch: '',
                        selectedEmbroideryIds: [],
                      })
                    }
                  }}
                  disabled={product.schoolCount === 0}
                >
                  Escolar
                </button>
                <button
                  type='button'
                  className={`px-3 py-1 text-xs font-medium rounded transition-colors ${
                    embroideryMode === 'todos'
                      ? 'bg-background shadow-sm text-foreground'
                      : 'text-muted-foreground hover:text-foreground'
                  }`}
                  onClick={() => {
                    if (embroideryMode !== 'todos') {
                      onEntryStateChange({
                        ...entryState,
                        embroideryMode: 'todos',
                        selectedSchoolLevelName: null,
                        selectedSchoolId: null,
                        selectedEmbroideryIds: [],
                      })
                    }
                  }}
                >
                  Todos
                </button>
              </div>
            )}
          </div>

          {embroideryChecked && (
            <div className='ml-6 space-y-3'>
              {/* ── Escolar mode: cascading selects ── */}
              {embroideryMode === 'escolar' && (
                <>
                  {schoolsLoading ? (
                    <div className='space-y-2'>
                      <Skeleton className='h-9 w-full max-w-xs' />
                      <Skeleton className='h-9 w-full max-w-xs' />
                    </div>
                  ) : schools.length > 0 ? (
                    <div className='grid grid-cols-2 gap-3 max-w-lg'>
                      {/* School Level select */}
                      <div className='space-y-1.5'>
                        <Label className='text-xs text-muted-foreground'>Nivel Escolar</Label>
                        {levelReadOnly ? (
                          <Input
                            value={effectiveLevelName ?? ''}
                            readOnly
                            className='h-9 text-sm bg-muted'
                          />
                        ) : (
                          <Select
                            value={effectiveLevelName ?? ''}
                            onValueChange={(v) =>
                              onEntryStateChange({
                                ...entryState,
                                selectedSchoolLevelName: v || null,
                                selectedSchoolId: null,
                                selectedEmbroideryIds: [],
                              })
                            }
                          >
                            <SelectTrigger className='h-9 text-sm'>
                              <SelectValue placeholder='Seleccionar nivel' />
                            </SelectTrigger>
                            <SelectContent>
                              {schoolLevels.map((lvl) => (
                                <SelectItem key={lvl} value={lvl}>
                                  {lvl}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </div>

                      {/* School select */}
                      <div className='space-y-1.5'>
                        <Label className='text-xs text-muted-foreground'>Escuela</Label>
                        {schoolReadOnly ? (
                          <Input
                            value={schoolsForLevel[0]?.name ?? ''}
                            readOnly
                            className='h-9 text-sm bg-muted'
                          />
                        ) : (
                          <Select
                            value={effectiveSchoolId != null ? String(effectiveSchoolId) : ''}
                            onValueChange={(v) =>
                              onEntryStateChange({
                                ...entryState,
                                selectedSchoolId: v ? Number(v) : null,
                                selectedEmbroideryIds: [],
                              })
                            }
                            disabled={!effectiveLevelName}
                          >
                            <SelectTrigger className='h-9 text-sm'>
                              <SelectValue placeholder='Seleccionar escuela' />
                            </SelectTrigger>
                            <SelectContent>
                              {schoolsForLevel.map((s) => (
                                <SelectItem key={s.schoolId} value={String(s.schoolId)}>
                                  {s.name}
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        )}
                      </div>
                    </div>
                  ) : (
                    <p className='text-muted-foreground text-xs'>Este producto no tiene escuelas asignadas.</p>
                  )}
                </>
              )}

              {/* ── Todos mode: text search ── */}
              {embroideryMode === 'todos' && (
                <div className='max-w-lg'>
                  <Label className='text-xs text-muted-foreground'>Buscar ponchado por nombre</Label>
                  <div className='relative mt-1.5'>
                    <SearchIcon className='absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-muted-foreground' />
                    <Input
                      value={embroiderySearch}
                      onChange={(e) =>
                        onEntryStateChange({
                          ...entryState,
                          embroiderySearch: e.target.value,
                          selectedEmbroideryIds: [],
                        })
                      }
                      placeholder='Escribe al menos 2 caracteres...'
                      className='h-9 text-sm pl-8'
                    />
                    {embroiderySearch && (
                      <button
                        type='button'
                        className='absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground'
                        onClick={() =>
                          onEntryStateChange({
                            ...entryState,
                            embroiderySearch: '',
                            selectedEmbroideryIds: [],
                          })
                        }
                      >
                        <XIcon className='size-3.5' />
                      </button>
                    )}
                  </div>
                </div>
              )}

              {/* ── Embroideries list (shared between both modes) ── */}
              {((embroideryMode === 'escolar' && effectiveSchoolId != null) ||
                (embroideryMode === 'todos' && embroiderySearch.trim().length >= 2)) && (
                <div className='mt-3'>
                  <Separator className='mb-3' />
                  <div className='flex items-center gap-2'>
                    <Label className='text-sm font-semibold'>Ponchados</Label>
                    {selectedEmbroideryIds.length > 0 && (
                      <Badge variant='secondary' className='text-[10px] px-1.5 py-0'>
                        {selectedEmbroideryIds.length} seleccionado{selectedEmbroideryIds.length > 1 ? 's' : ''}
                      </Badge>
                    )}
                  </div>
                    {embroideriesLoading ? (
                      <div className='mt-2 space-y-1.5'>
                        {Array.from({ length: 3 }).map((_, i) => (
                          <div key={i} className='flex items-center gap-3 rounded-md border p-2'>
                            <Skeleton className='size-10 rounded-md shrink-0' />
                            <div className='flex-1 space-y-1'>
                              <Skeleton className='h-4 w-32' />
                              <Skeleton className='h-3 w-20' />
                            </div>
                          </div>
                        ))}
                      </div>
                    ) : embroideries.length > 0 ? (
                      <div className='mt-2 space-y-1.5 max-h-52 overflow-y-auto'>
                        {embroideries.map((emb) => {
                          const isSelected = selectedEmbroideryIds.includes(emb.embroideryId)
                          return (
                            <div
                              key={emb.embroideryId}
                              className={`flex items-center gap-3 rounded-md border p-2 cursor-pointer transition-colors ${
                                isSelected
                                  ? 'bg-primary/5 border-primary/40'
                                  : 'hover:bg-muted/60 hover:border-muted-foreground/30'
                              }`}
                              onClick={() =>
                                onEntryStateChange({
                                  ...entryState,
                                  selectedEmbroideryIds: isSelected
                                    ? selectedEmbroideryIds.filter((id) => id !== emb.embroideryId)
                                    : [...selectedEmbroideryIds, emb.embroideryId],
                                })
                              }
                            >
                              {/* Thumbnail */}
                              {emb.imageDesignBase64 ? (
                                <img
                                  src={`data:image/png;base64,${emb.imageDesignBase64}`}
                                  alt={emb.name}
                                  className='size-10 rounded-md border object-cover shrink-0'
                                />
                              ) : (
                                <div className='size-10 bg-muted flex items-center justify-center rounded-md border shrink-0'>
                                  <ImageOffIcon className='text-muted-foreground size-4' />
                                </div>
                              )}
                              {/* Info */}
                              <div className='flex-1 min-w-0'>
                                <p className='text-sm font-medium leading-tight truncate'>
                                  {emb.name}
                                </p>
                                <div className='flex items-center gap-2 mt-0.5'>
                                  {emb.stiches && (
                                    <span className='text-muted-foreground text-xs'>
                                      {emb.stiches} puntadas
                                    </span>
                                  )}
                                  {emb.price != null && (
                                    <Badge variant='outline' className='text-[10px] px-1 py-0'>
                                      ${emb.price.toFixed(2)}
                                    </Badge>
                                  )}
                                </div>
                              </div>
                              {isSelected && (
                                <Badge variant='default' className='text-[10px] px-1.5 py-0 shrink-0'>
                                  <CheckIcon className='size-3' />
                                </Badge>
                              )}
                            </div>
                          )
                        })}
                      </div>
                  ) : (
                    <p className='text-muted-foreground text-xs mt-2'>
                      {embroideryMode === 'escolar'
                        ? 'No hay ponchados para esta escuela.'
                        : 'No se encontraron ponchados.'}
                    </p>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {hasDimensions ? (
          <>
            {/* ── Dimension checkboxes ── */}
            <div>
              <Label className='text-sm font-semibold'>Tallas y Variantes</Label>
              <div className='flex flex-wrap gap-4 mt-2'>
                {product.sizeSystemId != null && (
                  <div className='flex items-center space-x-2'>
                    <Checkbox
                      id={`sizes-${product.productId}`}
                      checked={sizesChecked}
                      onCheckedChange={(checked) => toggleSizes(checked === true)}
                    />
                    <Label htmlFor={`sizes-${product.productId}`} className='text-sm font-normal'>
                      Tallas
                    </Label>
                  </div>
                )}
                {product.variantSystems.map((vs) => (
                  <div key={vs.productVariantId} className='flex items-center space-x-2'>
                    <Checkbox
                      id={`vs-${product.productId}-${vs.productVariantId}`}
                      checked={variantChecks.includes(vs.productVariantId)}
                      onCheckedChange={() => toggleVariantCheck(vs.productVariantId)}
                    />
                    <Label
                      htmlFor={`vs-${product.productId}-${vs.productVariantId}`}
                      className='text-sm font-normal'
                    >
                      {vs.name}
                    </Label>
                  </div>
                ))}
              </div>
            </div>

            {/* ── Quantity fields ── */}
            {fields.length > 0 && (
              <>
                <Separator />
                <div>
                  <Label className='text-sm font-semibold'>Cantidades</Label>
                  <div className='grid grid-cols-3 gap-3 mt-2 rounded-md border p-3 max-h-80 overflow-y-auto lg:grid-cols-4 xl:grid-cols-5'>
                    {fields.map((field) => (
                      <div key={field.key} className='space-y-1'>
                        <Label className='text-muted-foreground text-xs'>
                          {field.label}
                        </Label>
                        <Input
                          type='number'
                          min='0'
                          placeholder='0'
                          value={quantities[field.key] ?? ''}
                          onChange={(e) => setQuantity(field.key, e.target.value)}
                          className='h-8 text-sm'
                        />
                      </div>
                    ))}
                  </div>
                </div>
              </>
            )}

            {sizesChecked && sizes.length === 0 && (
              <p className='text-muted-foreground text-xs'>
                El sistema de tallas no tiene tallas registradas.
              </p>
            )}

            {!sizesChecked && variantChecks.length === 0 && (
              <p className='text-muted-foreground text-xs mt-2'>
                Selecciona al menos una dimensión para ingresar cantidades.
              </p>
            )}
          </>
        ) : (
          <>
            {/* No dimensions → single quantity field */}
            <Separator />
            <div>
              <Label className='text-sm font-semibold'>Cantidad</Label>
              <div className='mt-2 max-w-48'>
                <Input
                  type='number'
                  min='0'
                  placeholder='0'
                  value={quantities['single'] ?? ''}
                  onChange={(e) => setQuantity('single', e.target.value)}
                  className='h-9'
                />
              </div>
            </div>
          </>
        )}

        {/* ── Action buttons ── */}
        <Separator />
        <div className='flex items-center gap-3'>
          <Button
            size='sm'
            disabled={!siteId || totalQty <= 0 || isSubmitting}
            onClick={handleSubmit}
          >
            {isSubmitting ? (
              <>
                <LoaderIcon className='size-3.5 animate-spin' />
                Procesando…
              </>
            ) : (
              <>
                <PackagePlusIcon className='size-3.5' />
                Agregar Existencias{totalQty > 0 ? ` (${totalQty})` : ''}
              </>
            )}
          </Button>
          {!siteId && (
            <p className='text-destructive text-xs'>Selecciona un sitio primero.</p>
          )}
        </div>
      </CardContent>
    </Card>
  )
}

// ── Page Component ───────────────────────────────────────

export default function StockEntryPage() {
  // ── Product search state ───────────────────────────
  const [searchInput, setSearchInput] = useState('')
  const [productQuery, setProductQuery] = useState<ProductQueryParams>({
    page: 1,
    pageSize: 20,
    sortBy: 'name',
  })

  const { data: productsData, isLoading: productsLoading } = useProducts(productQuery)
  const products = productsData?.items ?? []
  const totalPages = productsData?.totalPages ?? 0
  const currentPage = productsData?.page ?? 1
  const totalCount = productsData?.totalCount ?? 0

  // ── Site selector state ────────────────────────────
  const { data: sites = [], isLoading: sitesLoading } = useSites()
  const [selectedSiteId, setSelectedSiteId] = useState<number | null>(null)

  // Auto-select when only 1 site
  if (!sitesLoading && sites.length === 1 && selectedSiteId !== sites[0].siteId) {
    setSelectedSiteId(sites[0].siteId)
  }

  // ── Entry mutation ─────────────────────────────────
  const createEntryMutation = useCreateEntry()

  // ── Selected products (for tabs) ───────────────────
  const [selectedProducts, setSelectedProducts] = useState<SelectedProduct[]>([])
  const [activeTab, setActiveTab] = useState<string>('')
  const [entryStates, setEntryStates] = useState<Record<number, ProductEntryState>>({})

  // ── Handlers ───────────────────────────────────────

  const handleSearch = useCallback(() => {
    setProductQuery((prev) => ({
      ...prev,
      search: searchInput.trim() || undefined,
      page: 1,
    }))
  }, [searchInput])

  const handleClearSearch = useCallback(() => {
    setSearchInput('')
    setProductQuery((prev) => ({ ...prev, search: undefined, page: 1 }))
  }, [])

  const handlePageChange = useCallback((page: number) => {
    setProductQuery((prev) => ({ ...prev, page }))
  }, [])

  const handleSelectProduct = useCallback((product: ProductResponse) => {
    setSelectedProducts((prev) => {
      // Don't add duplicates
      if (prev.some((p) => p.productId === product.productId)) {
        // Just switch to that tab
        setActiveTab(String(product.productId))
        return prev
      }
      const newSelected: SelectedProduct = {
        productId: product.productId,
        name: product.name ?? `Producto #${product.productId}`,
        model: product.model,
        brandName: product.brandName,
        hasPicture: product.hasPicture,
        serialize: product.serialize ?? false,
        sizeSystemId: product.sizeSystemId ?? null,
        sizeSystemName: product.sizeSystemName ?? null,
        schoolCount: product.schoolCount,
        variantSystems: product.variantSystems.map((vs) => ({
          productVariantId: vs.productVariantId,
          name: vs.name,
        })),
      }
      // Initialize entry state for this product
      const firstVariantCheck = newSelected.sizeSystemId == null && newSelected.variantSystems.length > 0
        ? [newSelected.variantSystems[0].productVariantId]
        : []
      setEntryStates((prev) => ({
        ...prev,
        [product.productId]: {
          sizesChecked: newSelected.sizeSystemId != null,
          variantChecks: firstVariantCheck,
          quantities: {},
          embroideryChecked: false,
          embroideryMode: newSelected.schoolCount > 0 ? 'escolar' : 'todos',
          embroiderySearch: '',
          selectedSchoolLevelName: null,
          selectedSchoolId: null,
          selectedEmbroideryIds: [],
        },
      }))
      setActiveTab(String(product.productId))
      return [...prev, newSelected]
    })
  }, [])

  const handleRemoveTab = useCallback((productId: number) => {
    setSelectedProducts((prev) => {
      const next = prev.filter((p) => p.productId !== productId)
      setActiveTab((currentTab) => {
        if (currentTab === String(productId)) {
          return next.length > 0 ? String(next[next.length - 1].productId) : ''
        }
        return currentTab
      })
      return next
    })
    setEntryStates((prev) => {
      const next = { ...prev }
      delete next[productId]
      return next
    })
  }, [])

  const handleEntryStateChange = useCallback((productId: number, state: ProductEntryState) => {
    setEntryStates((prev) => ({ ...prev, [productId]: state }))
  }, [])

  const isProductSelected = useCallback(
    (productId: number) => selectedProducts.some((p) => p.productId === productId),
    [selectedProducts],
  )

  const handleSubmitEntry = useCallback(
    (productId: number, lines: CreateEntryLineRequest[]) => {
      if (!selectedSiteId || lines.length === 0) return

      createEntryMutation.mutate(
        {
          siteId: selectedSiteId,
          lines,
        },
        {
          onSuccess: () => {
            // Reset the quantities for this product on success
            setEntryStates((prev) => ({
              ...prev,
              [productId]: {
                ...prev[productId],
                quantities: {},
              },
            }))
          },
        },
      )
    },
    [selectedSiteId, createEntryMutation],
  )

  // ── Render ─────────────────────────────────────────

  return (
    <DashboardLayout>
      {/* Header */}
      <div className='mb-6 flex items-center justify-between'>
        <div>
          <h1 className='text-2xl font-bold tracking-tight'>Entrada de Mercancías</h1>
          <p className='text-muted-foreground text-sm'>
            Selecciona productos para registrar la entrada de mercancías.
          </p>
        </div>
        <div className='flex items-center gap-3'>
          {/* Site selector */}
          <div className='flex items-center gap-2'>
            <MapPinIcon className='size-4 text-muted-foreground' />
            {sitesLoading ? (
              <Skeleton className='h-9 w-40' />
            ) : sites.length === 1 ? (
              <Input value={sites[0].name} readOnly className='h-9 w-40 text-sm bg-muted' />
            ) : (
              <Select
                value={selectedSiteId != null ? String(selectedSiteId) : ''}
                onValueChange={(v) => setSelectedSiteId(v ? Number(v) : null)}
              >
                <SelectTrigger className='h-9 w-48 text-sm'>
                  <SelectValue placeholder='Seleccionar sitio' />
                </SelectTrigger>
                <SelectContent>
                  {sites.map((s) => (
                    <SelectItem key={s.siteId} value={String(s.siteId)}>
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            )}
          </div>
          {selectedProducts.length > 0 && (
            <Badge variant='secondary' className='text-sm gap-1.5 px-3 py-1'>
              <PackageIcon className='size-3.5' />
              {selectedProducts.length} producto{selectedProducts.length !== 1 ? 's' : ''} seleccionado{selectedProducts.length !== 1 ? 's' : ''}
            </Badge>
          )}
        </div>
      </div>

      <div className='flex gap-6 h-[calc(100vh-220px)]'>
        {/* ── Section 1: Product Search ── */}
        <Card className='w-85 shrink-0 flex flex-col'>
          <CardHeader className='pb-3'>
            <CardTitle className='text-base'>Buscar Productos</CardTitle>
          </CardHeader>
          <CardContent className='flex flex-1 flex-col gap-3 min-h-0'>
            {/* Search bar */}
            <div className='flex items-center gap-2'>
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-2.5 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Nombre, modelo, código...'
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleSearch()}
                  className='pl-8 pr-8 h-9 text-sm'
                />
                {searchInput && (
                  <button
                    type='button'
                    onClick={handleClearSearch}
                    className='text-muted-foreground hover:text-foreground absolute right-2 top-1/2 -translate-y-1/2'
                  >
                    <XIcon className='size-3.5' />
                  </button>
                )}
              </div>
              <Button variant='secondary' size='sm' onClick={handleSearch}>
                <SearchIcon className='size-3.5' />
              </Button>
            </div>

            {/* Product list */}
            <div className='flex-1 min-h-0 overflow-auto -mx-1 px-1 space-y-1.5'>
              {productsLoading ? (
                Array.from({ length: 6 }).map((_, i) => (
                  <div key={i} className='flex items-center gap-3 rounded-lg border p-2'>
                    <Skeleton className='size-10 rounded-md shrink-0' />
                    <div className='flex-1 space-y-1.5'>
                      <Skeleton className='h-4 w-3/4' />
                      <Skeleton className='h-3 w-1/2' />
                    </div>
                  </div>
                ))
              ) : products.length > 0 ? (
                products.map((p) => {
                  const selected = isProductSelected(p.productId)
                  return (
                    <div
                      key={p.productId}
                      className={`flex items-center gap-3 rounded-lg border p-2 transition-colors cursor-pointer ${
                        selected
                          ? 'bg-primary/5 border-primary/40'
                          : 'hover:bg-muted/60 hover:border-muted-foreground/30'
                      }`}
                      onClick={() => handleSelectProduct(p)}
                    >
                      <ProductImage productId={p.productId} hasPicture={p.hasPicture} />
                      <div className='flex-1 min-w-0'>
                        <p className='text-sm font-medium leading-tight truncate'>
                          {p.name ?? `Producto #${p.productId}`}
                        </p>
                        <div className='flex items-center gap-1.5 mt-0.5'>
                          {p.model && (
                            <span className='text-muted-foreground text-xs truncate'>
                              {p.model}
                            </span>
                          )}
                          {p.brandName && (
                            <Badge variant='outline' className='text-[10px] px-1 py-0 shrink-0'>
                              {p.brandName}
                            </Badge>
                          )}
                        </div>
                      </div>
                      {selected && (
                        <Badge variant='default' className='text-[10px] px-1.5 py-0 shrink-0'>
                          ✓
                        </Badge>
                      )}
                    </div>
                  )
                })
              ) : (
                <div className='flex flex-col items-center justify-center py-8'>
                  <PackageIcon className='text-muted-foreground mb-2 size-8' />
                  <p className='text-muted-foreground text-sm'>No se encontraron productos.</p>
                </div>
              )}
            </div>

            {/* Pagination */}
            {totalPages > 1 && (
              <div className='flex items-center justify-between border-t pt-2'>
                <span className='text-muted-foreground text-xs'>
                  {totalCount} resultado{totalCount !== 1 ? 's' : ''}
                </span>
                <div className='flex items-center gap-1.5'>
                  <span className='text-muted-foreground text-xs'>
                    {currentPage}/{totalPages}
                  </span>
                  <Button
                    variant='outline'
                    size='icon'
                    className='size-6'
                    onClick={() => handlePageChange(currentPage - 1)}
                    disabled={currentPage <= 1}
                  >
                    <ChevronLeftIcon className='size-3' />
                  </Button>
                  <Button
                    variant='outline'
                    size='icon'
                    className='size-6'
                    onClick={() => handlePageChange(currentPage + 1)}
                    disabled={currentPage >= totalPages}
                  >
                    <ChevronRightIcon className='size-3' />
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* ── Section 2: Product Tabs ── */}
        <div className='flex-1 min-w-0'>
          {selectedProducts.length > 0 ? (
            <Tabs value={activeTab} onValueChange={setActiveTab} className='flex h-full flex-col'>
              <div className='flex items-center gap-2'>
                <TabsList className='flex-wrap h-auto gap-1 justify-start'>
                  {selectedProducts.map((sp) => (
                    <TabsTrigger
                      key={sp.productId}
                      value={String(sp.productId)}
                      className='gap-1.5 pr-1 data-[state=active]:pr-1'
                    >
                      <span className='max-w-40 truncate text-xs'>
                        {sp.name}
                      </span>
                      <button
                        type='button'
                        className='text-muted-foreground hover:text-destructive hover:bg-destructive/10 ml-0.5 rounded p-0.5 transition-colors'
                        onClick={(e) => {
                          e.stopPropagation()
                          handleRemoveTab(sp.productId)
                        }}
                      >
                        <XIcon className='size-3' />
                      </button>
                    </TabsTrigger>
                  ))}
                </TabsList>
              </div>

              {selectedProducts.map((sp) => (
                <TabsContent
                  key={sp.productId}
                  value={String(sp.productId)}
                  className='flex-1 mt-3'
                >
                  <ProductTabContent
                    product={sp}
                    entryState={entryStates[sp.productId] ?? { sizesChecked: sp.sizeSystemId != null, variantChecks: sp.sizeSystemId == null && sp.variantSystems.length > 0 ? [sp.variantSystems[0].productVariantId] : [], quantities: {}, embroideryChecked: false, embroideryMode: sp.schoolCount > 0 ? 'escolar' : 'todos', embroiderySearch: '', selectedSchoolLevelName: null, selectedSchoolId: null, selectedEmbroideryIds: [] }}
                    onEntryStateChange={(state) => handleEntryStateChange(sp.productId, state)}
                    siteId={selectedSiteId}
                    onSubmit={(lines) => handleSubmitEntry(sp.productId, lines)}
                    isSubmitting={createEntryMutation.isPending}
                  />
                </TabsContent>
              ))}
            </Tabs>
          ) : (
            <Card className='h-full flex items-center justify-center'>
              <div className='flex flex-col items-center text-center p-8'>
                <PackagePlusIcon className='text-muted-foreground size-16 mb-4' />
                <h3 className='text-lg font-semibold mb-1'>Selecciona productos</h3>
                <p className='text-muted-foreground text-sm max-w-md'>
                  Busca y selecciona productos del panel izquierdo para comenzar a registrar la entrada de mercancías. Cada producto seleccionado generará una pestaña.
                </p>
              </div>
            </Card>
          )}
        </div>
      </div>
    </DashboardLayout>
  )
}
