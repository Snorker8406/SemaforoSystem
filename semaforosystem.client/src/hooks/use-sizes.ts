import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type {
  SizeSystemQueryParams,
  CreateSizeSystemRequest,
  UpdateSizeSystemRequest,
  CreateSizeRequest,
  UpdateSizeRequest,
} from '@/services/size-service'
import {
  getSizeSystems,
  getSizeSystem,
  getSizeSystemsLookup,
  createSizeSystem,
  updateSizeSystem,
  deleteSizeSystem,
  getSizesBySystem,
  createSize,
  updateSize,
  deleteSize,
} from '@/services/size-service'
import { ApiError } from '@/lib/api-client'

// ── Query Keys ───────────────────────────────────────────

export const sizeKeys = {
  all: ['sizes'] as const,
  systems: () => [...sizeKeys.all, 'systems'] as const,
  systemList: (params: SizeSystemQueryParams) => [...sizeKeys.systems(), 'list', params] as const,
  systemDetail: (id: number) => [...sizeKeys.systems(), 'detail', id] as const,
  systemLookup: () => [...sizeKeys.systems(), 'lookup'] as const,
  bySystem: (systemId: number) => [...sizeKeys.all, 'bySystem', systemId] as const,
}

// ── Size System Queries ──────────────────────────────────

export function useSizeSystems(params: SizeSystemQueryParams = {}) {
  return useQuery({
    queryKey: sizeKeys.systemList(params),
    queryFn: () => getSizeSystems(params),
    placeholderData: (previousData) => previousData,
  })
}

export function useSizeSystem(id: number | null | undefined) {
  return useQuery({
    queryKey: sizeKeys.systemDetail(id!),
    queryFn: () => getSizeSystem(id!),
    enabled: id != null,
  })
}

export function useSizeSystemsLookup() {
  return useQuery({
    queryKey: sizeKeys.systemLookup(),
    queryFn: getSizeSystemsLookup,
    staleTime: 10 * 60 * 1000,
  })
}

// ── Size Queries ─────────────────────────────────────────

export function useSizesBySystem(systemId: number | null | undefined) {
  return useQuery({
    queryKey: sizeKeys.bySystem(systemId!),
    queryFn: () => getSizesBySystem(systemId!),
    enabled: systemId != null,
  })
}

// ── Size System Mutations ────────────────────────────────

export function useCreateSizeSystem(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateSizeSystemRequest) => createSizeSystem(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sizeKeys.all })
      toast.success('Sistema de tallas creado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Ya existe un sistema con ese nombre.')
      } else {
        toast.error('Error al crear el sistema de tallas')
      }
    },
  })
}

export function useUpdateSizeSystem(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateSizeSystemRequest }) =>
      updateSizeSystem(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sizeKeys.all })
      toast.success('Sistema de tallas actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Ya existe un sistema con ese nombre.')
      } else {
        toast.error('Error al actualizar el sistema de tallas')
      }
    },
  })
}

export function useDeleteSizeSystem(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteSizeSystem(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sizeKeys.all })
      toast.success('Sistema de tallas eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene tallas asociadas.',
        )
      } else {
        toast.error('Error al eliminar el sistema de tallas')
      }
    },
  })
}

// ── Size Mutations ───────────────────────────────────────

export function useCreateSize(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ systemId, data }: { systemId: number; data: CreateSizeRequest }) =>
      createSize(systemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sizeKeys.all })
      toast.success('Talla creada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al crear la talla')
      } else {
        toast.error('Error al crear la talla')
      }
    },
  })
}

export function useUpdateSize(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateSizeRequest }) =>
      updateSize(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sizeKeys.all })
      toast.success('Talla actualizada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar la talla')
      } else {
        toast.error('Error al actualizar la talla')
      }
    },
  })
}

export function useDeleteSize(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteSize(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: sizeKeys.all })
      toast.success('Talla eliminada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene registros relacionados.',
        )
      } else {
        toast.error('Error al eliminar la talla')
      }
    },
  })
}
