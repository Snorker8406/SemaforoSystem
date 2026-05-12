import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { PlusIcon, Trash2Icon } from 'lucide-react'

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
import { Separator } from '@/components/ui/separator'

import type {
  CreateProviderPayableLineRequest,
  CreateProviderPayableRequest,
  ProviderPayableDetailResponse,
  UpdateProviderPayableRequest,
} from '@/services/provider-payable-service'
import {
  createProviderPayable,
  getPayableStatuses,
  getPayableTypes,
  updateProviderPayable,
} from '@/services/provider-payable-service'
import { getProviders } from '@/services/provider-service'
import { ApiError } from '@/lib/api-client'

interface ProviderPayableFormDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  providerId: number
  payable?: ProviderPayableDetailResponse | null
}

interface LineForm {
  lineNumber: number
  descriptionSnapshot: string
  quantity: string
  unitCost: string
  discountAmount: string
  taxAmount: string
  notes: string
}

interface HeaderForm {
  providerPayableTypeId: string
  providerPayableStatusId: string
  siteId: string
  openedByEmployeeId: string
  documentNumber: string
  reference: string
  documentDate: string
  dueDate: string
  currencyCode: string
  notes: string
  purchaseOrderId: string
  purchaseReceiptId: string
}

const emptyLine = (lineNumber: number): LineForm => ({
  lineNumber,
  descriptionSnapshot: '',
  quantity: '1',
  unitCost: '0',
  discountAmount: '0',
  taxAmount: '0',
  notes: '',
})

const today = () => new Date().toISOString().slice(0, 10)

const emptyHeader = (): HeaderForm => ({
  providerPayableTypeId: '',
  providerPayableStatusId: '',
  siteId: localStorage.getItem('default_site_id') ?? '',
  openedByEmployeeId: localStorage.getItem('default_employee_id') ?? '',
  documentNumber: '',
  reference: '',
  documentDate: today(),
  dueDate: '',
  currencyCode: 'MXN',
  notes: '',
  purchaseOrderId: '',
  purchaseReceiptId: '',
})

function num(v: string): number {
  const n = Number(v)
  return Number.isFinite(n) ? n : 0
}

function lineTotal(l: LineForm): number {
  return num(l.quantity) * num(l.unitCost) - num(l.discountAmount) + num(l.taxAmount)
}

export default function ProviderPayableFormDialog({
  open,
  onOpenChange,
  providerId,
  payable,
}: ProviderPayableFormDialogProps) {
  const qc = useQueryClient()
  const isEditing = !!payable
  const needsProviderPick = !isEditing && (!providerId || providerId <= 0)

  const [header, setHeader] = useState<HeaderForm>(emptyHeader)
  const [lines, setLines] = useState<LineForm[]>([])
  const [prevOpen, setPrevOpen] = useState(false)
  const [selectedProviderId, setSelectedProviderId] = useState<string>('')

  const { data: providersData } = useQuery({
    queryKey: ['providers-picker'],
    queryFn: () => getProviders({ page: 1, pageSize: 200, sortBy: 'legalName' }),
    enabled: open && needsProviderPick,
    staleTime: 5 * 60 * 1000,
  })

  const { data: types = [] } = useQuery({
    queryKey: ['payable-types'],
    queryFn: getPayableTypes,
    staleTime: 10 * 60 * 1000,
  })
  const { data: statuses = [] } = useQuery({
    queryKey: ['payable-statuses'],
    queryFn: getPayableStatuses,
    staleTime: 10 * 60 * 1000,
  })

  useEffect(() => {
    if (open && !prevOpen) {
      setPrevOpen(true)
      if (payable) {
        setHeader({
          providerPayableTypeId: String(payable.providerPayableTypeId),
          providerPayableStatusId: String(payable.providerPayableStatusId),
          siteId: String(payable.siteId),
          openedByEmployeeId: String(payable.openedByEmployeeId),
          documentNumber: payable.documentNumber ?? '',
          reference: payable.reference ?? '',
          documentDate: payable.documentDate.slice(0, 10),
          dueDate: payable.dueDate ? payable.dueDate.slice(0, 10) : '',
          currencyCode: payable.currencyCode,
          notes: payable.notes ?? '',
          purchaseOrderId: payable.purchaseOrderId ? String(payable.purchaseOrderId) : '',
          purchaseReceiptId: payable.purchaseReceiptId ? String(payable.purchaseReceiptId) : '',
        })
        setLines([]) // lines are managed in detail dialog when editing
      } else {
        setHeader(emptyHeader())
        setLines([])
        setSelectedProviderId('')
      }
    } else if (!open && prevOpen) {
      setPrevOpen(false)
    }
  }, [open, prevOpen, payable])

  // Default type/status when opening create
  useEffect(() => {
    if (open && !isEditing) {
      if (!header.providerPayableTypeId && types.length) {
        const preferred =
          types.find((t) => t.code === 'SUPPLIER_INVOICE') ?? types[0]
        setHeader((h) => ({ ...h, providerPayableTypeId: String(preferred.id) }))
      }
      if (!header.providerPayableStatusId && statuses.length) {
        const preferred = statuses.find((s) => s.code === 'OPEN') ?? statuses[0]
        setHeader((h) => ({ ...h, providerPayableStatusId: String(preferred.id) }))
      }
    }
  }, [
    open,
    isEditing,
    types,
    statuses,
    header.providerPayableTypeId,
    header.providerPayableStatusId,
  ])

  // ── Totals (derived from lines) ──────────────────────
  const totals = useMemo(() => {
    const subtotal = lines.reduce((s, l) => s + num(l.quantity) * num(l.unitCost), 0)
    const discountTotal = lines.reduce((s, l) => s + num(l.discountAmount), 0)
    const taxTotal = lines.reduce((s, l) => s + num(l.taxAmount), 0)
    const total = lines.reduce((s, l) => s + lineTotal(l), 0)
    return { subtotal, discountTotal, taxTotal, total }
  }, [lines])

  // ── Mutations ────────────────────────────────────────

  const createMutation = useMutation({
    mutationFn: (data: CreateProviderPayableRequest) => createProviderPayable(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-payables'] })
      if (providerId) qc.invalidateQueries({ queryKey: ['provider-detail', providerId] })
      toast.success('Cuenta por pagar creada')
      onOpenChange(false)
    },
    onError: (err: Error) => handleErr(err, 'crear'),
  })

  const updateMutation = useMutation({
    mutationFn: (data: UpdateProviderPayableRequest) =>
      updateProviderPayable(payable!.providerPayableId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-payables'] })
      qc.invalidateQueries({ queryKey: ['provider-payable', payable!.providerPayableId] })
      toast.success('Cuenta por pagar actualizada')
      onOpenChange(false)
    },
    onError: (err: Error) => handleErr(err, 'actualizar'),
  })

  function handleErr(err: Error, action: string) {
    if (err instanceof ApiError) {
      const body = err.body as { message?: string }
      toast.error(body?.message ?? `No se pudo ${action} la cuenta.`)
    } else {
      toast.error(`Error al ${action} la cuenta`)
    }
  }

  const isPending = createMutation.isPending || updateMutation.isPending

  // ── Lines manipulation ───────────────────────────────

  function addLine() {
    setLines((prev) => [...prev, emptyLine((prev[prev.length - 1]?.lineNumber ?? 0) + 1)])
  }
  function removeLine(index: number) {
    setLines((prev) => prev.filter((_, i) => i !== index))
  }
  function updateLine<K extends keyof LineForm>(index: number, field: K, value: LineForm[K]) {
    setLines((prev) =>
      prev.map((l, i) => (i === index ? { ...l, [field]: value } : l)),
    )
  }

  // ── Submit ───────────────────────────────────────────

  function validate(): string | null {
    if (needsProviderPick && !selectedProviderId) return 'Selecciona el proveedor.'
    if (!header.providerPayableTypeId) return 'Selecciona el tipo de cuenta.'
    if (!header.siteId) return 'El ID de sitio es requerido.'
    if (!header.openedByEmployeeId) return 'El empleado responsable es requerido.'
    if (!header.documentDate) return 'La fecha del documento es requerida.'
    if (!header.currencyCode) return 'La moneda es requerida.'
    if (!isEditing) {
      for (const l of lines) {
        if (!l.descriptionSnapshot.trim()) return `La línea ${l.lineNumber} requiere descripción.`
        if (num(l.quantity) <= 0) return `Cantidad inválida en línea ${l.lineNumber}.`
        if (num(l.unitCost) < 0) return `Costo unitario inválido en línea ${l.lineNumber}.`
      }
    }
    return null
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    const err = validate()
    if (err) {
      toast.error(err)
      return
    }

    // remember defaults
    localStorage.setItem('default_site_id', header.siteId)
    localStorage.setItem('default_employee_id', header.openedByEmployeeId)

    if (isEditing) {
      const payload: UpdateProviderPayableRequest = {
        providerPayableTypeId: Number(header.providerPayableTypeId),
        providerPayableStatusId: Number(header.providerPayableStatusId),
        purchaseOrderId: header.purchaseOrderId ? Number(header.purchaseOrderId) : null,
        purchaseReceiptId: header.purchaseReceiptId ? Number(header.purchaseReceiptId) : null,
        documentNumber: header.documentNumber.trim() || null,
        reference: header.reference.trim() || null,
        documentDate: header.documentDate,
        dueDate: header.dueDate || null,
        currencyCode: header.currencyCode.trim().toUpperCase(),
        subtotal: payable!.subtotal,
        discountTotal: payable!.discountTotal,
        taxTotal: payable!.taxTotal,
        total: payable!.total,
        notes: header.notes.trim() || null,
      }
      updateMutation.mutate(payload)
    } else {
      const linesPayload: CreateProviderPayableLineRequest[] = lines.map((l) => ({
        lineNumber: l.lineNumber,
        descriptionSnapshot: l.descriptionSnapshot.trim(),
        quantity: num(l.quantity),
        unitCost: num(l.unitCost),
        discountAmount: num(l.discountAmount),
        taxAmount: num(l.taxAmount),
        lineTotal: lineTotal(l),
        notes: l.notes.trim() || null,
      }))
      const payload: CreateProviderPayableRequest = {
        providerId: needsProviderPick ? Number(selectedProviderId) : providerId,
        providerPayableTypeId: Number(header.providerPayableTypeId),
        providerPayableStatusId: header.providerPayableStatusId
          ? Number(header.providerPayableStatusId)
          : null,
        siteId: Number(header.siteId),
        openedByEmployeeId: Number(header.openedByEmployeeId),
        purchaseOrderId: header.purchaseOrderId ? Number(header.purchaseOrderId) : null,
        purchaseReceiptId: header.purchaseReceiptId
          ? Number(header.purchaseReceiptId)
          : null,
        documentNumber: header.documentNumber.trim() || null,
        reference: header.reference.trim() || null,
        documentDate: header.documentDate,
        dueDate: header.dueDate || null,
        currencyCode: header.currencyCode.trim().toUpperCase(),
        notes: header.notes.trim() || null,
        lines: linesPayload,
      }
      createMutation.mutate(payload)
    }
  }

  function setField<K extends keyof HeaderForm>(field: K, value: HeaderForm[K]) {
    setHeader((prev) => ({ ...prev, [field]: value }))
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='max-w-4xl max-h-[90vh] overflow-y-auto'>
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Editar cuenta por pagar' : 'Nueva cuenta por pagar'}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? 'Modifica los datos generales. Las líneas y movimientos se administran en el detalle.'
              : 'Registra el documento financiero del proveedor. El sistema generará un cargo (CHARGE) inicial automáticamente.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className='space-y-5'>
          {needsProviderPick && (
            <div className='space-y-2'>
              <Label>
                Proveedor <span className='text-destructive'>*</span>
              </Label>
              <Select
                value={selectedProviderId}
                onValueChange={setSelectedProviderId}
              >
                <SelectTrigger>
                  <SelectValue placeholder='Seleccionar proveedor' />
                </SelectTrigger>
                <SelectContent>
                  {(providersData?.items ?? []).map((p) => (
                    <SelectItem key={p.providerId} value={String(p.providerId)}>
                      {p.tradeName ?? p.legalName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {/* Header */}
          <div className='grid grid-cols-1 gap-4 sm:grid-cols-3'>
            <div className='space-y-2'>
              <Label>
                Tipo <span className='text-destructive'>*</span>
              </Label>
              <Select
                value={header.providerPayableTypeId}
                onValueChange={(v) => setField('providerPayableTypeId', v)}
              >
                <SelectTrigger>
                  <SelectValue placeholder='Seleccionar tipo' />
                </SelectTrigger>
                <SelectContent>
                  {types.map((t) => (
                    <SelectItem key={t.id} value={String(t.id)}>
                      {t.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className='space-y-2'>
              <Label>Estado</Label>
              <Select
                value={header.providerPayableStatusId}
                onValueChange={(v) => setField('providerPayableStatusId', v)}
              >
                <SelectTrigger>
                  <SelectValue placeholder='Estado' />
                </SelectTrigger>
                <SelectContent>
                  {statuses.map((s) => (
                    <SelectItem key={s.id} value={String(s.id)}>
                      {s.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className='space-y-2'>
              <Label>Moneda</Label>
              <Input
                value={header.currencyCode}
                onChange={(e) => setField('currencyCode', e.target.value.toUpperCase())}
                maxLength={10}
              />
            </div>

            <div className='space-y-2'>
              <Label>
                Sitio (ID) <span className='text-destructive'>*</span>
              </Label>
              <Input
                type='number'
                min={1}
                value={header.siteId}
                onChange={(e) => setField('siteId', e.target.value)}
                disabled={isEditing}
                placeholder='1'
              />
            </div>

            <div className='space-y-2'>
              <Label>
                Empleado (ID) <span className='text-destructive'>*</span>
              </Label>
              <Input
                type='number'
                min={1}
                value={header.openedByEmployeeId}
                onChange={(e) => setField('openedByEmployeeId', e.target.value)}
                disabled={isEditing}
              />
            </div>

            <div className='space-y-2'>
              <Label>No. documento / folio</Label>
              <Input
                value={header.documentNumber}
                onChange={(e) => setField('documentNumber', e.target.value)}
                maxLength={100}
                placeholder='Folio del proveedor'
              />
            </div>

            <div className='space-y-2'>
              <Label>
                Fecha documento <span className='text-destructive'>*</span>
              </Label>
              <Input
                type='date'
                value={header.documentDate}
                onChange={(e) => setField('documentDate', e.target.value)}
              />
            </div>

            <div className='space-y-2'>
              <Label>Fecha vencimiento</Label>
              <Input
                type='date'
                value={header.dueDate}
                onChange={(e) => setField('dueDate', e.target.value)}
              />
            </div>

            <div className='space-y-2'>
              <Label>Referencia</Label>
              <Input
                value={header.reference}
                onChange={(e) => setField('reference', e.target.value)}
                maxLength={100}
              />
            </div>

            <div className='space-y-2'>
              <Label>Orden de compra (ID)</Label>
              <Input
                type='number'
                value={header.purchaseOrderId}
                onChange={(e) => setField('purchaseOrderId', e.target.value)}
                placeholder='Opcional'
              />
            </div>

            <div className='space-y-2'>
              <Label>Recibo de compra (ID)</Label>
              <Input
                type='number'
                value={header.purchaseReceiptId}
                onChange={(e) => setField('purchaseReceiptId', e.target.value)}
                placeholder='Opcional'
              />
            </div>
          </div>

          <div className='space-y-2'>
            <Label>Notas</Label>
            <Textarea
              value={header.notes}
              onChange={(e) => setField('notes', e.target.value)}
              rows={2}
            />
          </div>

          {/* Lines */}
          {!isEditing && (
            <>
              <Separator />
              <div className='flex items-center justify-between'>
                <h3 className='text-sm font-semibold'>Líneas del documento</h3>
                <Button type='button' size='sm' variant='outline' onClick={addLine}>
                  <PlusIcon className='size-4' />
                  Agregar línea
                </Button>
              </div>

              {lines.length === 0 ? (
                <p className='text-muted-foreground text-center text-sm py-4'>
                  Sin líneas. Puedes crear la cuenta solo con el total general o agregar
                  el detalle.
                </p>
              ) : (
                <div className='space-y-3'>
                  {lines.map((l, idx) => (
                    <div
                      key={idx}
                      className='border rounded-md p-3 grid grid-cols-12 gap-2 items-end'
                    >
                      <div className='col-span-1'>
                        <Label className='text-xs'>#</Label>
                        <Input
                          type='number'
                          min={1}
                          value={l.lineNumber}
                          onChange={(e) =>
                            updateLine(idx, 'lineNumber', Number(e.target.value))
                          }
                        />
                      </div>
                      <div className='col-span-4'>
                        <Label className='text-xs'>Descripción *</Label>
                        <Input
                          value={l.descriptionSnapshot}
                          onChange={(e) =>
                            updateLine(idx, 'descriptionSnapshot', e.target.value)
                          }
                          maxLength={500}
                        />
                      </div>
                      <div className='col-span-1'>
                        <Label className='text-xs'>Cant.</Label>
                        <Input
                          type='number'
                          step='0.0001'
                          value={l.quantity}
                          onChange={(e) => updateLine(idx, 'quantity', e.target.value)}
                        />
                      </div>
                      <div className='col-span-2'>
                        <Label className='text-xs'>Costo unit.</Label>
                        <Input
                          type='number'
                          step='0.0001'
                          value={l.unitCost}
                          onChange={(e) => updateLine(idx, 'unitCost', e.target.value)}
                        />
                      </div>
                      <div className='col-span-1'>
                        <Label className='text-xs'>Desc.</Label>
                        <Input
                          type='number'
                          step='0.0001'
                          value={l.discountAmount}
                          onChange={(e) =>
                            updateLine(idx, 'discountAmount', e.target.value)
                          }
                        />
                      </div>
                      <div className='col-span-1'>
                        <Label className='text-xs'>Imp.</Label>
                        <Input
                          type='number'
                          step='0.0001'
                          value={l.taxAmount}
                          onChange={(e) => updateLine(idx, 'taxAmount', e.target.value)}
                        />
                      </div>
                      <div className='col-span-1 text-right text-sm font-medium'>
                        {lineTotal(l).toFixed(2)}
                      </div>
                      <div className='col-span-1 flex justify-end'>
                        <Button
                          type='button'
                          size='icon'
                          variant='ghost'
                          className='text-destructive size-8'
                          onClick={() => removeLine(idx)}
                        >
                          <Trash2Icon className='size-4' />
                        </Button>
                      </div>
                    </div>
                  ))}

                  {/* Totals */}
                  <div className='flex justify-end'>
                    <div className='w-72 space-y-1 text-sm'>
                      <div className='flex justify-between'>
                        <span className='text-muted-foreground'>Subtotal</span>
                        <span>{totals.subtotal.toFixed(2)}</span>
                      </div>
                      <div className='flex justify-between'>
                        <span className='text-muted-foreground'>Descuento</span>
                        <span>-{totals.discountTotal.toFixed(2)}</span>
                      </div>
                      <div className='flex justify-between'>
                        <span className='text-muted-foreground'>Impuestos</span>
                        <span>{totals.taxTotal.toFixed(2)}</span>
                      </div>
                      <Separator />
                      <div className='flex justify-between font-semibold'>
                        <span>Total</span>
                        <span>
                          {totals.total.toFixed(2)} {header.currencyCode}
                        </span>
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </>
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
                  ? 'Guardar cambios'
                  : 'Crear cuenta'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
