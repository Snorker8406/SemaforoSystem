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

import type { SizeResponse } from '@/services/size-service'
import { useDeleteSize } from '@/hooks/use-sizes'

interface SizeDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  size: SizeResponse | null
}

export default function SizeDeleteDialog({
  open,
  onOpenChange,
  size,
}: SizeDeleteDialogProps) {
  const deleteMutation = useDeleteSize({
    onSuccess: () => onOpenChange(false),
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar talla?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar la talla{' '}
            <strong>&quot;{size?.sizeValue}&quot;</strong> del sistema{' '}
            <strong>&quot;{size?.sizeSystemName}&quot;</strong>. Esta acción no se
            puede deshacer.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending}>
            Cancelar
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              if (size) deleteMutation.mutate(size.sizeId)
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
