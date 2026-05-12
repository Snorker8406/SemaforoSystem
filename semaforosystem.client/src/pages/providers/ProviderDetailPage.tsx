import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useParams } from '@tanstack/react-router'
import { toast } from 'sonner'

import {
  ArrowLeftIcon,
  BuildingIcon,
  CreditCardIcon,
  EditIcon,
  GlobeIcon,
  MapPinIcon,
  PhoneIcon,
  PlusIcon,
  ReceiptIcon,
  StarIcon,
  Trash2Icon,
  UserIcon,
} from 'lucide-react'

import DashboardLayout from '@/components/layout/dashboard-layout'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'

import type {
  ProviderAddressResponse,
  ProviderBankAccountResponse,
  ProviderContactResponse,
} from '@/services/provider-service'
import {
  deleteProviderAddress,
  deleteProviderBankAccount,
  deleteProviderContact,
  getProviderDetail,
} from '@/services/provider-service'

import ProviderFormDialog from './provider-form-dialog'
import ProviderContactFormDialog from './provider-contact-form-dialog'
import ProviderAddressFormDialog from './provider-address-form-dialog'
import ProviderBankFormDialog from './provider-bank-form-dialog'
import ProviderPayablesTab from './provider-payables-tab'

// ── helpers ──────────────────────────────────────────────

function StatusBadge({ code, name }: { code: string; name: string }) {
  const variant =
    code?.toUpperCase() === 'ACTIVE'
      ? ('default' as const)
      : code?.toUpperCase() === 'BLOCKED'
        ? ('destructive' as const)
        : ('secondary' as const)
  return <Badge variant={variant}>{name}</Badge>
}

type DeleteTarget =
  | { kind: 'contact'; row: ProviderContactResponse }
  | { kind: 'address'; row: ProviderAddressResponse }
  | { kind: 'bank'; row: ProviderBankAccountResponse }

// ── Page ─────────────────────────────────────────────────

export default function ProviderDetailPage() {
  const { providerId } = useParams({ from: '/proveedores/$providerId' })
  const id = Number(providerId)
  const qc = useQueryClient()

  const { data, isLoading } = useQuery({
    queryKey: ['provider-detail', id],
    queryFn: () => getProviderDetail(id),
  })

  // ── Dialog state ─────────────────────────────────────
  const [editProviderOpen, setEditProviderOpen] = useState(false)

  const [contactDialogOpen, setContactDialogOpen] = useState(false)
  const [editingContact, setEditingContact] =
    useState<ProviderContactResponse | null>(null)

  const [addressDialogOpen, setAddressDialogOpen] = useState(false)
  const [editingAddress, setEditingAddress] =
    useState<ProviderAddressResponse | null>(null)

  const [bankDialogOpen, setBankDialogOpen] = useState(false)
  const [editingBank, setEditingBank] =
    useState<ProviderBankAccountResponse | null>(null)

  const [deleteTarget, setDeleteTarget] = useState<DeleteTarget | null>(null)

  // ── Delete mutation ──────────────────────────────────

  const deleteMutation = useMutation({
    mutationFn: async () => {
      if (!deleteTarget) return
      if (deleteTarget.kind === 'contact')
        return deleteProviderContact(id, deleteTarget.row.providerContactId)
      if (deleteTarget.kind === 'address')
        return deleteProviderAddress(id, deleteTarget.row.providerAddressId)
      if (deleteTarget.kind === 'bank')
        return deleteProviderBankAccount(id, deleteTarget.row.providerBankAccountId)
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['provider-detail', id] })
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success('Registro eliminado')
      setDeleteTarget(null)
    },
    onError: () => toast.error('Error al eliminar el registro'),
  })

  // ── Render ───────────────────────────────────────────

  if (isLoading) {
    return (
      <DashboardLayout>
        <div className='space-y-6'>
          <Skeleton className='h-9 w-64' />
          <Skeleton className='h-40 w-full' />
          <Skeleton className='h-96 w-full' />
        </div>
      </DashboardLayout>
    )
  }

  if (!data) {
    return (
      <DashboardLayout>
        <Card>
          <CardContent className='py-12 text-center'>
            <p className='text-muted-foreground'>Proveedor no encontrado.</p>
            <Button asChild variant='link' className='mt-2'>
              <Link to='/proveedores'>Volver al listado</Link>
            </Button>
          </CardContent>
        </Card>
      </DashboardLayout>
    )
  }

  return (
    <DashboardLayout>
      <div className='space-y-6'>
        {/* Header */}
        <div className='flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between'>
          <div className='flex items-start gap-3'>
            <Button asChild variant='ghost' size='icon' className='size-9'>
              <Link to='/proveedores'>
                <ArrowLeftIcon className='size-4' />
              </Link>
            </Button>
            <div>
              <div className='flex items-center gap-3'>
                <h1 className='text-2xl font-bold tracking-tight'>
                  {data.legalName}
                </h1>
                <StatusBadge
                  code={data.providerStatusCode}
                  name={data.providerStatusName}
                />
              </div>
              {data.tradeName && (
                <p className='text-muted-foreground text-sm'>
                  {data.tradeName}
                </p>
              )}
            </div>
          </div>
          <div className='flex gap-2'>
            <Button
              variant='outline'
              className='gap-2'
              onClick={() => setEditProviderOpen(true)}
            >
              <EditIcon className='size-4' />
              Editar
            </Button>
          </div>
        </div>

        {/* Summary card */}
        <Card>
          <CardHeader>
            <CardTitle className='text-base font-medium'>
              Información general
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className='grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4'>
              <Info label='RFC / Tax ID' value={data.taxId} />
              <Info
                label='Sitio web'
                value={
                  data.website ? (
                    <a
                      href={data.website}
                      target='_blank'
                      rel='noreferrer'
                      className='text-primary inline-flex items-center gap-1 hover:underline'
                    >
                      <GlobeIcon className='size-3' />
                      {data.website}
                    </a>
                  ) : null
                }
              />
              <Info label='Contactos' value={String(data.contactsCount)} />
              <Info label='Direcciones' value={String(data.addressesCount)} />
            </div>
            {data.notes && (
              <>
                <Separator className='my-4' />
                <div>
                  <p className='text-muted-foreground text-xs font-medium uppercase'>
                    Notas
                  </p>
                  <p className='text-sm whitespace-pre-line mt-1'>{data.notes}</p>
                </div>
              </>
            )}
          </CardContent>
        </Card>

        {/* Tabs */}
        <Tabs defaultValue='contacts' className='space-y-4'>
          <TabsList>
            <TabsTrigger value='contacts'>
              Contactos ({data.contacts.length})
            </TabsTrigger>
            <TabsTrigger value='addresses'>
              Direcciones ({data.addresses.length})
            </TabsTrigger>
            <TabsTrigger value='banks'>
              Cuentas bancarias ({data.bankAccounts.length})
            </TabsTrigger>
            <TabsTrigger value='payables' className='gap-1'>
              <ReceiptIcon className='size-3.5' />
              Cuentas por pagar
            </TabsTrigger>
          </TabsList>

          {/* Contacts */}
          <TabsContent value='contacts'>
            <SectionCard
              title='Contactos'
              emptyIcon={UserIcon}
              empty={data.contacts.length === 0}
              emptyMessage='No hay contactos registrados.'
              onAdd={() => {
                setEditingContact(null)
                setContactDialogOpen(true)
              }}
            >
              <div className='grid grid-cols-1 gap-3 sm:grid-cols-2'>
                {data.contacts.map((c) => (
                  <Card key={c.providerContactId} className='border-muted'>
                    <CardContent className='p-4'>
                      <div className='flex items-start justify-between gap-2'>
                        <div className='space-y-1 min-w-0'>
                          <div className='flex items-center gap-2 flex-wrap'>
                            <span className='font-medium truncate'>{c.name}</span>
                            {c.isPrimary && (
                              <Badge
                                variant='outline'
                                className='gap-1 border-amber-300 text-amber-600'
                              >
                                <StarIcon className='size-3 fill-amber-500 text-amber-500' />
                                Primario
                              </Badge>
                            )}
                          </div>
                          {c.role && (
                            <p className='text-muted-foreground text-xs'>
                              {c.role}
                            </p>
                          )}
                          <div className='space-y-1 pt-1 text-sm'>
                            {c.email && (
                              <p className='text-muted-foreground truncate'>
                                {c.email}
                              </p>
                            )}
                            {(c.phone || c.cellphone) && (
                              <p className='text-muted-foreground flex items-center gap-1'>
                                <PhoneIcon className='size-3' />
                                {c.phone || c.cellphone}
                                {c.whatsapp && (
                                  <Badge
                                    variant='outline'
                                    className='ml-1 border-green-300 text-green-700'
                                  >
                                    WhatsApp
                                  </Badge>
                                )}
                              </p>
                            )}
                          </div>
                          {c.notes && (
                            <p className='text-muted-foreground text-xs pt-1 line-clamp-2'>
                              {c.notes}
                            </p>
                          )}
                        </div>
                        <RowActions
                          onEdit={() => {
                            setEditingContact(c)
                            setContactDialogOpen(true)
                          }}
                          onDelete={() =>
                            setDeleteTarget({ kind: 'contact', row: c })
                          }
                        />
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </SectionCard>
          </TabsContent>

          {/* Addresses */}
          <TabsContent value='addresses'>
            <SectionCard
              title='Direcciones'
              emptyIcon={MapPinIcon}
              empty={data.addresses.length === 0}
              emptyMessage='No hay direcciones registradas.'
              onAdd={() => {
                setEditingAddress(null)
                setAddressDialogOpen(true)
              }}
            >
              <div className='grid grid-cols-1 gap-3 sm:grid-cols-2'>
                {data.addresses.map((a) => (
                  <Card key={a.providerAddressId} className='border-muted'>
                    <CardContent className='p-4'>
                      <div className='flex items-start justify-between gap-2'>
                        <div className='space-y-1 min-w-0'>
                          <div className='flex items-center gap-2 flex-wrap'>
                            <Badge variant='secondary'>{a.addressType}</Badge>
                            {a.isPrimary && (
                              <Badge
                                variant='outline'
                                className='gap-1 border-amber-300 text-amber-600'
                              >
                                <StarIcon className='size-3 fill-amber-500 text-amber-500' />
                                Primaria
                              </Badge>
                            )}
                          </div>
                          <p className='text-sm font-medium'>{a.addressLine}</p>
                          <p className='text-muted-foreground text-xs'>
                            {[a.city, a.state, a.postalCode, a.country]
                              .filter(Boolean)
                              .join(', ') || '—'}
                          </p>
                        </div>
                        <RowActions
                          onEdit={() => {
                            setEditingAddress(a)
                            setAddressDialogOpen(true)
                          }}
                          onDelete={() =>
                            setDeleteTarget({ kind: 'address', row: a })
                          }
                        />
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </SectionCard>
          </TabsContent>

          {/* Banks */}
          <TabsContent value='banks'>
            <SectionCard
              title='Cuentas bancarias'
              emptyIcon={CreditCardIcon}
              empty={data.bankAccounts.length === 0}
              emptyMessage='No hay cuentas bancarias registradas.'
              onAdd={() => {
                setEditingBank(null)
                setBankDialogOpen(true)
              }}
            >
              <div className='grid grid-cols-1 gap-3 sm:grid-cols-2'>
                {data.bankAccounts.map((b) => (
                  <Card key={b.providerBankAccountId} className='border-muted'>
                    <CardContent className='p-4'>
                      <div className='flex items-start justify-between gap-2'>
                        <div className='space-y-1 min-w-0'>
                          <div className='flex items-center gap-2 flex-wrap'>
                            <span className='font-medium inline-flex items-center gap-1'>
                              <BuildingIcon className='size-3.5' />
                              {b.bankName}
                            </span>
                            <Badge variant='outline'>{b.currencyCode}</Badge>
                            {b.isPrimary && (
                              <Badge
                                variant='outline'
                                className='gap-1 border-amber-300 text-amber-600'
                              >
                                <StarIcon className='size-3 fill-amber-500 text-amber-500' />
                                Primaria
                              </Badge>
                            )}
                          </div>
                          {b.accountHolder && (
                            <p className='text-muted-foreground text-xs'>
                              {b.accountHolder}
                            </p>
                          )}
                          <div className='text-sm space-y-0.5 pt-1'>
                            {b.accountNumber && (
                              <p>
                                <span className='text-muted-foreground'>No. cuenta:</span>{' '}
                                <span className='font-mono'>{b.accountNumber}</span>
                              </p>
                            )}
                            {b.clabe && (
                              <p>
                                <span className='text-muted-foreground'>CLABE:</span>{' '}
                                <span className='font-mono'>{b.clabe}</span>
                              </p>
                            )}
                          </div>
                          {b.notes && (
                            <p className='text-muted-foreground text-xs pt-1 line-clamp-2'>
                              {b.notes}
                            </p>
                          )}
                        </div>
                        <RowActions
                          onEdit={() => {
                            setEditingBank(b)
                            setBankDialogOpen(true)
                          }}
                          onDelete={() =>
                            setDeleteTarget({ kind: 'bank', row: b })
                          }
                        />
                      </div>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </SectionCard>
          </TabsContent>

          {/* Payables */}
          <TabsContent value='payables'>
            <ProviderPayablesTab providerId={id} />
          </TabsContent>
        </Tabs>
      </div>

      {/* Dialogs */}
      <ProviderFormDialog
        open={editProviderOpen}
        onOpenChange={setEditProviderOpen}
        provider={data}
      />
      <ProviderContactFormDialog
        open={contactDialogOpen}
        onOpenChange={setContactDialogOpen}
        providerId={id}
        contact={editingContact}
      />
      <ProviderAddressFormDialog
        open={addressDialogOpen}
        onOpenChange={setAddressDialogOpen}
        providerId={id}
        address={editingAddress}
      />
      <ProviderBankFormDialog
        open={bankDialogOpen}
        onOpenChange={setBankDialogOpen}
        providerId={id}
        bankAccount={editingBank}
      />

      <AlertDialog
        open={deleteTarget !== null}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>¿Eliminar registro?</AlertDialogTitle>
            <AlertDialogDescription>
              Esta acción no se puede deshacer.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={deleteMutation.isPending}>
              Cancelar
            </AlertDialogCancel>
            <AlertDialogAction
              onClick={(e) => {
                e.preventDefault()
                deleteMutation.mutate()
              }}
              disabled={deleteMutation.isPending}
              className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
            >
              {deleteMutation.isPending ? 'Eliminando...' : 'Eliminar'}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </DashboardLayout>
  )
}

// ── small components ─────────────────────────────────────

function Info({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div>
      <p className='text-muted-foreground text-xs font-medium uppercase'>
        {label}
      </p>
      <p className='text-sm mt-1'>{value || '—'}</p>
    </div>
  )
}

function RowActions({
  onEdit,
  onDelete,
}: {
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <div className='flex gap-1 shrink-0'>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button variant='ghost' size='icon' className='size-7' onClick={onEdit}>
            <EditIcon className='size-3.5' />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Editar</TooltipContent>
      </Tooltip>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button
            variant='ghost'
            size='icon'
            className='text-destructive hover:text-destructive size-7'
            onClick={onDelete}
          >
            <Trash2Icon className='size-3.5' />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Eliminar</TooltipContent>
      </Tooltip>
    </div>
  )
}

interface SectionCardProps {
  title: string
  empty: boolean
  emptyMessage: string
  emptyIcon: React.ComponentType<{ className?: string }>
  onAdd: () => void
  children: React.ReactNode
}

function SectionCard({
  title,
  empty,
  emptyMessage,
  emptyIcon: EmptyIcon,
  onAdd,
  children,
}: SectionCardProps) {
  return (
    <Card>
      <CardHeader className='flex flex-row items-center justify-between pb-3'>
        <CardTitle className='text-base font-medium'>{title}</CardTitle>
        <Button size='sm' onClick={onAdd} className='gap-2'>
          <PlusIcon className='size-4' />
          Agregar
        </Button>
      </CardHeader>
      <CardContent>
        {empty ? (
          <div className='flex flex-col items-center gap-2 py-10 text-center'>
            <EmptyIcon className='text-muted-foreground size-8' />
            <p className='text-muted-foreground text-sm'>{emptyMessage}</p>
            <Button variant='link' size='sm' onClick={onAdd}>
              Agregar el primero
            </Button>
          </div>
        ) : (
          children
        )}
      </CardContent>
    </Card>
  )
}
