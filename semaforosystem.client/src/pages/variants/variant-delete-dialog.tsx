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

import type { VariantResponse } from '@/services/variant-service'
import { useDeleteVariant } from '@/hooks/use-variants'

interface VariantDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  variant: VariantResponse | null
}

export default function VariantDeleteDialog({
  open,
  onOpenChange,
  variant,
}: VariantDeleteDialogProps) {
  const deleteMutation = useDeleteVariant({
    onSuccess: () => onOpenChange(false),
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar variante?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar la variante{' '}
            <strong>&quot;{variant?.variantValue}&quot;</strong> del sistema{' '}
            <strong>&quot;{variant?.productVariantSystemName}&quot;</strong>. Esta acción no se
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
              if (variant) deleteMutation.mutate(variant.productVariantId)
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
