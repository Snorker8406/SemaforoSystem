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

import type { SchoolLevelResponse } from '@/services/school-level-service'
import { deleteSchoolLevel } from '@/services/school-level-service'
import { ApiError } from '@/lib/api-client'

interface SchoolLevelDeleteDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  level: SchoolLevelResponse | null
}

export default function SchoolLevelDeleteDialog({
  open,
  onOpenChange,
  level,
}: SchoolLevelDeleteDialogProps) {
  const queryClient = useQueryClient()

  const deleteMutation = useMutation({
    mutationFn: () => deleteSchoolLevel(level!.schoolLevelId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['schoolLevels'] })
      queryClient.invalidateQueries({ queryKey: ['school-levels'] })
      toast.success(`Nivel "${level!.name}" eliminado exitosamente`)
      onOpenChange(false)
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string; schoolCount?: number }
        toast.error(
          body?.message ??
            'No se puede eliminar: tiene escuelas asignadas.',
        )
      } else {
        toast.error('Error al eliminar el nivel escolar')
      }
    },
  })

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>¿Eliminar nivel escolar?</AlertDialogTitle>
          <AlertDialogDescription>
            Está a punto de eliminar el nivel escolar{' '}
            <strong>&quot;{level?.name}&quot;</strong>. Esta acción no se puede
            deshacer.
            {level && level.schoolCount > 0 && (
              <>
                {' '}
                Este nivel tiene{' '}
                <strong>
                  {level.schoolCount}{' '}
                  {level.schoolCount === 1
                    ? 'escuela asignada'
                    : 'escuelas asignadas'}
                </strong>
                .
              </>
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
