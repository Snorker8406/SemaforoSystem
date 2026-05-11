import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

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

import type { ProviderResponse } from '@/services/provider-service'
import { deleteProvider } from '@/services/provider-service'
import { ApiError } from '@/lib/api-client'

interface ProviderDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  provider: ProviderResponse | null
}

export default function ProviderDeleteDialog({
  open,
  onOpenChange,
  provider,
}: ProviderDeleteDialogProps) {
  const qc = useQueryClient()

  const mutation = useMutation({
    mutationFn: () => deleteProvider(provider!.providerId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['providers'] })
      toast.success(`Proveedor "${provider!.legalName}" eliminado`)
      onOpenChange(false)
    },
    onError: (err: Error) => {
      if (err instanceof ApiError && err.status === 409) {
        const body = err.body as { message?: string }
        toast.error(
          body?.message ??
            'No se puede eliminar: el proveedor tiene registros relacionados. Considere cambiar el estado a INACTIVE.',
        )
      } else {
        toast.error('Error al eliminar el proveedor')
      }
    },
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar proveedor?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar el proveedor{' '}
            <strong>&quot;{provider?.legalName}&quot;</strong>. Esta acción no se
            puede deshacer. Si el proveedor tiene órdenes de compra, recibos,
            cuentas por pagar o relaciones con productos, no podrá eliminarse;
            en ese caso considere cambiar su estado a INACTIVE.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={mutation.isPending}>
            Cancelar
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              mutation.mutate()
            }}
            disabled={mutation.isPending}
            className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
          >
            {mutation.isPending ? 'Eliminando...' : 'Eliminar'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
