import { useState, useCallback, useMemo } from 'react'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs'

import {
  PlusIcon,
  Trash2Icon,
  PackageIcon,
  PaletteIcon,
  SearchIcon,
  XIcon,
  ChevronLeftIcon,
  ChevronRightIcon,
  ImageOffIcon,
  CheckIcon,
} from 'lucide-react'

import type {
  ProductComboResponse,
  ProductComboDetailResponse,
} from '@/services/combo-service'
import type { ProductQueryParams } from '@/services/product-service'
import { getProductPictureUrl } from '@/services/product-service'
import {
  useComboDetails,
  useCreateComboDetail,
  useDeleteComboDetail,
} from '@/hooks/use-combos'
import { useProducts } from '@/hooks/use-products'

// ── Types ────────────────────────────────────────────────

interface ComboDetailDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  combo: ProductComboResponse | null
}

// ── Product Image Component ──────────────────────────────

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
  const sizeClass = size === 'md' ? 'size-14' : 'size-10'

  if (!hasPicture || error) {
    return (
      <div
        className={`${sizeClass} bg-muted flex items-center justify-center rounded-md border`}
      >
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

// ── Component ────────────────────────────────────────────

export default function ComboDetailDialog({
  open,
  onOpenChange,
  combo,
}: ComboDetailDialogProps) {
  const comboId = combo?.productComboId ?? null

  // ── Tab state ──────────────────────────────────────
  const [activeTab, setActiveTab] = useState<string>('details')

  // ── Data: current details ──────────────────────────
  const { data: details = [], isLoading: detailsLoading } = useComboDetails(
    open ? comboId : null,
  )

  // ── Product picker state ───────────────────────────
  const [productSearch, setProductSearch] = useState('')
  const [productQuery, setProductQuery] = useState<ProductQueryParams>({
    page: 1,
    pageSize: 12,
    sortBy: 'name',
  })

  const { data: productsData, isLoading: productsLoading } = useProducts(
    open && activeTab === 'add'
      ? { ...productQuery }
      : { page: 1, pageSize: 1 },
  )
  const products = open && activeTab === 'add' ? (productsData?.items ?? []) : []
  const productsTotalPages = productsData?.totalPages ?? 0
  const productsPage = productsData?.page ?? 1
  const productsTotalCount = productsData?.totalCount ?? 0

  // Set of product IDs already in the combo — to mark as "already added"
  const existingProductIds = useMemo(
    () => new Set(details.filter((d) => d.productId).map((d) => d.productId!)),
    [details],
  )

  // ── Mutations ──────────────────────────────────────
  const createDetail = useCreateComboDetail()
  const deleteDetail = useDeleteComboDetail()

  // ── Handlers ───────────────────────────────────────

  const handleProductSearch = useCallback(() => {
    setProductQuery((prev) => ({
      ...prev,
      search: productSearch.trim() || undefined,
      page: 1,
    }))
  }, [productSearch])

  const handleClearProductSearch = useCallback(() => {
    setProductSearch('')
    setProductQuery((prev) => ({ ...prev, search: undefined, page: 1 }))
  }, [])

  const handleProductPage = useCallback((page: number) => {
    setProductQuery((prev) => ({ ...prev, page }))
  }, [])

  function handleAddProduct(productId: number) {
    if (!comboId) return
    createDetail.mutate({ comboId, data: { productId } })
  }

  function handleDeleteDetail(detail: ProductComboDetailResponse) {
    if (!comboId) return
    deleteDetail.mutate({ comboId, detailId: detail.productComboDetailId })
  }

  // Reset picker state when switching tabs
  function handleTabChange(tab: string) {
    setActiveTab(tab)
    if (tab === 'add') {
      setProductSearch('')
      setProductQuery({ page: 1, pageSize: 12, sortBy: 'name' })
    }
  }

  // ── Render ─────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[900px] max-h-[85vh] flex flex-col'>
        <DialogHeader>
          <DialogTitle>Detalles del Combo</DialogTitle>
          <DialogDescription>
            {combo ? (
              <>
                Combo: <strong>{combo.name}</strong>
                {combo.description && (
                  <span className='text-muted-foreground'>
                    {' '}
                    — {combo.description}
                  </span>
                )}
              </>
            ) : (
              'Cargando...'
            )}
          </DialogDescription>
        </DialogHeader>

        <Tabs
          value={activeTab}
          onValueChange={handleTabChange}
          className='flex-1 flex flex-col min-h-0'
        >
          <TabsList className='w-full justify-start'>
            <TabsTrigger value='details' className='gap-1.5'>
              <PackageIcon className='size-3.5' />
              Elementos actuales
              {details.length > 0 && (
                <Badge variant='secondary' className='ml-1 h-5 px-1.5 text-xs'>
                  {details.length}
                </Badge>
              )}
            </TabsTrigger>
            <TabsTrigger value='add' className='gap-1.5'>
              <PlusIcon className='size-3.5' />
              Agregar producto
            </TabsTrigger>
          </TabsList>

          {/* ── TAB: Current details ── */}
          <TabsContent value='details' className='flex-1 min-h-0 overflow-auto mt-3'>
            <div className='rounded-md border'>
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className='w-[60px]' />
                    <TableHead>Tipo</TableHead>
                    <TableHead>Nombre</TableHead>
                    <TableHead className='w-[60px]' />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {detailsLoading ? (
                    Array.from({ length: 3 }).map((_, i) => (
                      <TableRow key={i}>
                        <TableCell><Skeleton className='size-10 rounded-md' /></TableCell>
                        <TableCell><Skeleton className='h-5 w-20' /></TableCell>
                        <TableCell><Skeleton className='h-5 w-40' /></TableCell>
                        <TableCell><Skeleton className='h-5 w-8' /></TableCell>
                      </TableRow>
                    ))
                  ) : details.length > 0 ? (
                    details.map((d) => (
                      <TableRow key={d.productComboDetailId}>
                        <TableCell>
                          {d.productId ? (
                            <ProductImage productId={d.productId} hasPicture />
                          ) : (
                            <div className='bg-muted flex size-10 items-center justify-center rounded-md border'>
                              <PaletteIcon className='text-muted-foreground size-4' />
                            </div>
                          )}
                        </TableCell>
                        <TableCell>
                          {d.productId ? (
                            <Badge variant='secondary' className='gap-1'>
                              <PackageIcon className='size-3' />
                              Producto
                            </Badge>
                          ) : d.embroideryId ? (
                            <Badge variant='outline' className='gap-1'>
                              <PaletteIcon className='size-3' />
                              Bordado
                            </Badge>
                          ) : (
                            <span className='text-muted-foreground'>—</span>
                          )}
                        </TableCell>
                        <TableCell className='font-medium'>
                          {d.productName ?? d.embroideryName ?? '—'}
                        </TableCell>
                        <TableCell>
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant='ghost'
                                size='icon'
                                className='text-destructive hover:text-destructive size-7'
                                onClick={() => handleDeleteDetail(d)}
                                disabled={deleteDetail.isPending}
                              >
                                <Trash2Icon className='size-3.5' />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>Eliminar</TooltipContent>
                          </Tooltip>
                        </TableCell>
                      </TableRow>
                    ))
                  ) : (
                    <TableRow>
                      <TableCell colSpan={4} className='h-20 text-center'>
                        <p className='text-muted-foreground text-sm'>
                          Este combo no tiene elementos aún.
                        </p>
                        <Button
                          variant='link'
                          size='sm'
                          onClick={() => handleTabChange('add')}
                        >
                          Agregar productos
                        </Button>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </TabsContent>

          {/* ── TAB: Add product ── */}
          <TabsContent value='add' className='flex-1 min-h-0 flex flex-col mt-3 gap-3'>
            {/* Search bar */}
            <div className='flex items-center gap-2'>
              <div className='relative flex-1'>
                <SearchIcon className='text-muted-foreground absolute left-3 top-1/2 size-4 -translate-y-1/2' />
                <Input
                  placeholder='Buscar producto por nombre, modelo o código...'
                  value={productSearch}
                  onChange={(e) => setProductSearch(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleProductSearch()}
                  className='pl-9 pr-9'
                />
                {productSearch && (
                  <button
                    type='button'
                    onClick={handleClearProductSearch}
                    className='text-muted-foreground hover:text-foreground absolute right-3 top-1/2 -translate-y-1/2'
                  >
                    <XIcon className='size-4' />
                  </button>
                )}
              </div>
              <Button variant='secondary' size='sm' onClick={handleProductSearch}>
                Buscar
              </Button>
            </div>

            {/* Product grid */}
            <div className='flex-1 min-h-0 overflow-auto'>
              {productsLoading ? (
                <div className='grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4'>
                  {Array.from({ length: 8 }).map((_, i) => (
                    <div key={i} className='rounded-lg border p-3 space-y-2'>
                      <Skeleton className='aspect-square w-full rounded-md' />
                      <Skeleton className='h-4 w-3/4' />
                    </div>
                  ))}
                </div>
              ) : products.length > 0 ? (
                <div className='grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4'>
                  {products.map((p) => {
                    const alreadyAdded = existingProductIds.has(p.productId)
                    return (
                      <div
                        key={p.productId}
                        className={`group relative rounded-lg border p-2.5 transition-colors ${
                          alreadyAdded
                            ? 'bg-primary/5 border-primary/30'
                            : 'hover:border-primary/50 hover:bg-muted/30 cursor-pointer'
                        }`}
                        onClick={() => {
                          if (!alreadyAdded && !createDetail.isPending) {
                            handleAddProduct(p.productId)
                          }
                        }}
                      >
                        {/* Image */}
                        <div className='relative mb-2 flex justify-center'>
                          <ProductImage
                            productId={p.productId}
                            hasPicture={p.hasPicture}
                            size='md'
                          />
                          {alreadyAdded && (
                            <div className='bg-primary absolute -right-1 -top-1 flex size-5 items-center justify-center rounded-full'>
                              <CheckIcon className='text-primary-foreground size-3' />
                            </div>
                          )}
                        </div>

                        {/* Info */}
                        <div className='space-y-0.5'>
                          <p className='text-sm font-medium leading-tight line-clamp-2'>
                            {p.name ?? `Producto #${p.productId}`}
                          </p>
                          {p.model && (
                            <p className='text-muted-foreground text-xs truncate'>
                              {p.model}
                            </p>
                          )}
                          {p.brandName && (
                            <Badge variant='outline' className='text-[10px] px-1.5 py-0'>
                              {p.brandName}
                            </Badge>
                          )}
                        </div>

                        {alreadyAdded && (
                          <p className='text-primary mt-1 text-[10px] font-medium'>
                            Ya en el combo
                          </p>
                        )}
                      </div>
                    )
                  })}
                </div>
              ) : (
                <div className='flex flex-col items-center justify-center py-12'>
                  <PackageIcon className='text-muted-foreground mb-2 size-8' />
                  <p className='text-muted-foreground text-sm'>
                    No se encontraron productos.
                  </p>
                </div>
              )}
            </div>

            {/* Pagination */}
            {productsTotalPages > 1 && (
              <div className='flex items-center justify-between border-t pt-3'>
                <span className='text-muted-foreground text-xs'>
                  {productsTotalCount} producto{productsTotalCount !== 1 ? 's' : ''}
                </span>
                <div className='flex items-center gap-2'>
                  <span className='text-muted-foreground text-xs'>
                    Página {productsPage} de {productsTotalPages}
                  </span>
                  <div className='flex gap-1'>
                    <Button
                      variant='outline'
                      size='icon'
                      className='size-7'
                      onClick={() => handleProductPage(productsPage - 1)}
                      disabled={productsPage <= 1}
                    >
                      <ChevronLeftIcon className='size-3.5' />
                    </Button>
                    <Button
                      variant='outline'
                      size='icon'
                      className='size-7'
                      onClick={() => handleProductPage(productsPage + 1)}
                      disabled={productsPage >= productsTotalPages}
                    >
                      <ChevronRightIcon className='size-3.5' />
                    </Button>
                  </div>
                </div>
              </div>
            )}
          </TabsContent>
        </Tabs>

        {/* Footer */}
        <div className='flex items-center justify-between text-sm text-muted-foreground pt-2'>
          <span>{details.length} elemento(s) en el combo</span>
          <Button variant='outline' size='sm' onClick={() => onOpenChange(false)}>
            Cerrar
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
