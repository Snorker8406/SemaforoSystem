import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type {
  EmbroideryQueryParams,
  CreateEmbroideryRequest,
  UpdateEmbroideryRequest,
} from '@/services/embroidery-service'
import {
  getEmbroideries,
  getEmbroidery,
  getEmbroideriesLookup,
  createEmbroidery,
  updateEmbroidery,
  deleteEmbroidery,
  uploadEmbroideryImage,
} from '@/services/embroidery-service'
import { ApiError } from '@/lib/api-client'

// ── Query Keys ───────────────────────────────────────────

export const embroideryKeys = {
  all: ['embroideries'] as const,
  lists: () => [...embroideryKeys.all, 'list'] as const,
  list: (params: EmbroideryQueryParams) => [...embroideryKeys.lists(), params] as const,
  details: () => [...embroideryKeys.all, 'detail'] as const,
  detail: (id: number) => [...embroideryKeys.details(), id] as const,
  lookup: (schoolId?: number) => [...embroideryKeys.all, 'lookup', schoolId] as const,
}

// ── Queries ──────────────────────────────────────────────

/** Paginated, filterable embroidery list */
export function useEmbroideries(params: EmbroideryQueryParams = {}) {
  return useQuery({
    queryKey: embroideryKeys.list(params),
    queryFn: () => getEmbroideries(params),
    placeholderData: (previousData) => previousData,
  })
}

/** Single embroidery by ID */
export function useEmbroidery(id: number | null | undefined) {
  return useQuery({
    queryKey: embroideryKeys.detail(id!),
    queryFn: () => getEmbroidery(id!),
    enabled: id != null,
  })
}

/** Lightweight lookup list */
export function useEmbroideriesLookup(schoolId?: number) {
  return useQuery({
    queryKey: embroideryKeys.lookup(schoolId),
    queryFn: () => getEmbroideriesLookup(schoolId),
    staleTime: 10 * 60 * 1000,
  })
}

// ── Mutations ────────────────────────────────────────────

/** Create a new embroidery */
export function useCreateEmbroidery(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateEmbroideryRequest) => createEmbroidery(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: embroideryKeys.all })
      toast.success('Ponchado creado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al crear el ponchado')
      } else {
        toast.error('Error al crear el ponchado')
      }
    },
  })
}

/** Update an existing embroidery */
export function useUpdateEmbroidery(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateEmbroideryRequest }) =>
      updateEmbroidery(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: embroideryKeys.all })
      toast.success('Ponchado actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar el ponchado')
      } else {
        toast.error('Error al actualizar el ponchado')
      }
    },
  })
}

/** Delete an embroidery */
export function useDeleteEmbroidery(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteEmbroidery(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: embroideryKeys.all })
      toast.success('Ponchado eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene registros relacionados.',
        )
      } else {
        toast.error('Error al eliminar el ponchado')
      }
    },
  })
}

/** Upload embroidery image */
export function useUploadEmbroideryImage(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, file }: { id: number; file: File }) =>
      uploadEmbroideryImage(id, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: embroideryKeys.all })
      toast.success('Imagen subida exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al subir la imagen')
      } else {
        toast.error('Error al subir la imagen')
      }
    },
  })
}
