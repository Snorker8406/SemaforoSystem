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

import type { VariantSystemResponse } from '@/services/variant-service'
import { useDeleteVariantSystem } from '@/hooks/use-variants'

interface VariantSystemDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  variantSystem: VariantSystemResponse | null
}

export default function VariantSystemDeleteDialog({
  open,
  onOpenChange,
  variantSystem,
}: VariantSystemDeleteDialogProps) {
  const deleteMutation = useDeleteVariantSystem({
    onSuccess: () => onOpenChange(false),
  })

  const hasVariants = (variantSystem?.variantCount ?? 0) > 0
  const hasProducts = (variantSystem?.productCount ?? 0) > 0
  const canDelete = !hasVariants && !hasProducts

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar sistema de variantes?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar el sistema de variantes{' '}
            <strong>&quot;{variantSystem?.name}&quot;</strong>.
            {hasVariants && (
              <>
                {' '}
                Este sistema tiene <strong>{variantSystem!.variantCount} variante(s)</strong>{' '}
                asociada(s).
              </>
            )}
            {hasProducts && (
              <>
                {' '}
                Tiene <strong>{variantSystem!.productCount} producto(s)</strong>{' '}
                vinculado(s).
              </>
            )}
            {!canDelete && <> Debe eliminar las relaciones primero.</>}
            {canDelete && <> Esta acción no se puede deshacer.</>}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={deleteMutation.isPending}>
            Cancelar
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault()
              if (variantSystem) deleteMutation.mutate(variantSystem.productVariantId)
            }}
            disabled={deleteMutation.isPending || !canDelete}
            className='bg-destructive text-destructive-foreground hover:bg-destructive/90'
          >
            {deleteMutation.isPending ? 'Eliminando...' : 'Eliminar'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
