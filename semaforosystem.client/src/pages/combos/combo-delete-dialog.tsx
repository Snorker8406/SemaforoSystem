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

import type { ProductComboResponse } from '@/services/combo-service'
import { useDeleteCombo } from '@/hooks/use-combos'

interface ComboDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  combo: ProductComboResponse | null
}

export default function ComboDeleteDialog({
  open,
  onOpenChange,
  combo,
}: ComboDeleteDialogProps) {
  const deleteMutation = useDeleteCombo({
    onSuccess: () => onOpenChange(false),
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar combo?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar el combo{' '}
            <strong>&quot;{combo?.name}&quot;</strong>. Se eliminarán también
            todos sus detalles. Esta acción no se puede deshacer.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending}>
            Cancelar
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              deleteMutation.mutate(combo!.productComboId)
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
