import { useState } from 'react'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
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
import { Separator } from '@/components/ui/separator'

import {
  PlusIcon,
  Trash2Icon,
  PackageIcon,
  PaletteIcon,
} from 'lucide-react'

import type {
  ProductComboResponse,
  ProductComboDetailResponse,
} from '@/services/combo-service'
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

// ── Component ────────────────────────────────────────────

export default function ComboDetailDialog({
  open,
  onOpenChange,
  combo,
}: ComboDetailDialogProps) {
  const comboId = combo?.productComboId ?? null

  // ── Data ───────────────────────────────────────────
  const { data: details = [], isLoading: detailsLoading } = useComboDetails(
    open ? comboId : null,
  )

  // Product lookup — fetch a page to use as picker source
  const { data: productsData } = useProducts(
    open ? { page: 1, pageSize: 100, sortBy: 'name' } : { page: 1, pageSize: 1 },
  )
  const products = open ? (productsData?.items ?? []) : []

  // ── Add detail form state ──────────────────────────
  const [addType, setAddType] = useState<'product' | 'embroidery'>('product')
  const [selectedProductId, setSelectedProductId] = useState<string>('')
  const [addError, setAddError] = useState('')

  // ── Mutations ──────────────────────────────────────
  const createDetail = useCreateComboDetail()
  const deleteDetail = useDeleteComboDetail()

  // ── Handlers ───────────────────────────────────────

  function handleAddDetail() {
    if (!comboId) return
    setAddError('')

    if (addType === 'product') {
      if (!selectedProductId) {
        setAddError('Seleccione un producto.')
        return
      }
      createDetail.mutate(
        {
          comboId,
          data: { productId: Number(selectedProductId) },
        },
        {
          onSuccess: () => {
            setSelectedProductId('')
          },
        },
      )
    } else {
      // Embroidery — not a common lookup yet, so just show a message
      setAddError('El selector de bordados se implementará próximamente.')
    }
  }

  function handleDeleteDetail(detail: ProductComboDetailResponse) {
    if (!comboId) return
    deleteDetail.mutate({ comboId, detailId: detail.productComboDetailId })
  }

  // ── Render ─────────────────────────────────────────

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[700px]'>
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

        {/* Add detail section */}
        <div className='space-y-3'>
          <Label className='text-sm font-medium'>Agregar elemento al combo</Label>
          <div className='flex flex-col gap-2 sm:flex-row sm:items-end'>
            <div className='grid gap-1.5'>
              <Label className='text-xs text-muted-foreground'>Tipo</Label>
              <Select
                value={addType}
                onValueChange={(v) => {
                  setAddType(v as 'product' | 'embroidery')
                  setSelectedProductId('')
                  setAddError('')
                }}
              >
                <SelectTrigger className='w-[160px]'>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value='product'>Producto</SelectItem>
                  <SelectItem value='embroidery'>Bordado</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {addType === 'product' && (
              <div className='flex-1 grid gap-1.5'>
                <Label className='text-xs text-muted-foreground'>Producto</Label>
                <Select
                  value={selectedProductId}
                  onValueChange={setSelectedProductId}
                >
                  <SelectTrigger>
                    <SelectValue placeholder='Seleccionar producto...' />
                  </SelectTrigger>
                  <SelectContent>
                    {products.map((p) => (
                      <SelectItem
                        key={p.productId}
                        value={String(p.productId)}
                      >
                        {p.name ?? `Producto #${p.productId}`}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {addType === 'embroidery' && (
              <div className='flex-1'>
                <p className='text-muted-foreground text-xs italic'>
                  Selector de bordados no disponible aún.
                </p>
              </div>
            )}

            <Button
              type='button'
              size='sm'
              className='gap-1'
              onClick={handleAddDetail}
              disabled={createDetail.isPending}
            >
              <PlusIcon className='size-3.5' />
              {createDetail.isPending ? 'Agregando...' : 'Agregar'}
            </Button>
          </div>
          {addError && (
            <p className='text-destructive text-xs'>{addError}</p>
          )}
        </div>

        <Separator />

        {/* Details table */}
        <div className='rounded-md border'>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Tipo</TableHead>
                <TableHead>Nombre</TableHead>
                <TableHead className='w-[60px]' />
              </TableRow>
            </TableHeader>
            <TableBody>
              {detailsLoading ? (
                Array.from({ length: 3 }).map((_, i) => (
                  <TableRow key={i}>
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
                  <TableCell colSpan={3} className='h-20 text-center'>
                    <p className='text-muted-foreground text-sm'>
                      Este combo no tiene elementos aún.
                    </p>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>

        {/* Summary */}
        <div className='flex items-center justify-between text-sm text-muted-foreground'>
          <span>{details.length} elemento(s) en el combo</span>
          <Button variant='outline' size='sm' onClick={() => onOpenChange(false)}>
            Cerrar
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  )
}
