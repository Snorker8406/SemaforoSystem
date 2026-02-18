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

import type { SchoolResponse } from '@/services/school-service'
import { deleteSchool } from '@/services/school-service'
import { ApiError } from '@/lib/api-client'

interface SchoolDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  school: SchoolResponse | null
}

export default function SchoolDeleteDialog({
  open,
  onOpenChange,
  school,
}: SchoolDeleteDialogProps) {
  const queryClient = useQueryClient()

  const deleteMutation = useMutation({
    mutationFn: () => deleteSchool(school!.schoolId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schools'] })
      toast.success(`Escuela "${school!.name}" eliminada exitosamente`)
      onOpenChange(false)
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'No se puede eliminar: tiene registros relacionados.')
      } else {
        toast.error('Error al eliminar la escuela')
      }
    },
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar escuela?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar la escuela{' '}
            <strong>&quot;{school?.name}&quot;</strong>. Esta acción no se puede
            deshacer.
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
  )
}
