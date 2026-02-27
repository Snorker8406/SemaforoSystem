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

import type { EmbroideryResponse } from '@/services/embroidery-service'
import { useDeleteEmbroidery } from '@/hooks/use-embroideries'

interface EmbroideryDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  embroidery: EmbroideryResponse | null
}

export default function EmbroideryDeleteDialog({
  open,
  onOpenChange,
  embroidery,
}: EmbroideryDeleteDialogProps) {
  const deleteMutation = useDeleteEmbroidery({
    onSuccess: () => onOpenChange(false),
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar ponchado?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar el ponchado{' '}
            <strong>&quot;{embroidery?.name ?? 'Sin nombre'}&quot;</strong>. Esta
            acción no se puede deshacer.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending}>
            Cancelar
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              deleteMutation.mutate(embroidery!.embroideryId)
            }}
            disabled={deleteMutation.isPending}
            className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
          >
            {deleteMutation.isPending ? 'Eliminando...' : 'Eliminar'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
