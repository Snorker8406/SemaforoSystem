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

import type { SizeSystemResponse } from '@/services/size-service'
import { useDeleteSizeSystem } from '@/hooks/use-sizes'

interface SizeSystemDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  sizeSystem: SizeSystemResponse | null
}

export default function SizeSystemDeleteDialog({
  open,
  onOpenChange,
  sizeSystem,
}: SizeSystemDeleteDialogProps) {
  const deleteMutation = useDeleteSizeSystem({
    onSuccess: () => onOpenChange(false),
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar sistema de tallas?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar el sistema de tallas{' '}
            <strong>&quot;{sizeSystem?.name}&quot;</strong>.
            {sizeSystem && sizeSystem.sizeCount > 0 && (
              <>
                {' '}
                Este sistema tiene <strong>{sizeSystem.sizeCount} talla(s)</strong>{' '}
                asociada(s). Debe eliminar las tallas primero.
              </>
            )}
            {sizeSystem && sizeSystem.sizeCount === 0 && (
              <> Esta acción no se puede deshacer.</>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending}>
            Cancelar
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              if (sizeSystem) deleteMutation.mutate(sizeSystem.sizeSystemId)
            }}
            disabled={deleteMutation.isPending || (sizeSystem?.sizeCount ?? 0) > 0}
            className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
          >
            {deleteMutation.isPending ? 'Eliminando...' : 'Eliminar'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
