import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type {
  VariantSystemQueryParams,
  CreateVariantSystemRequest,
  UpdateVariantSystemRequest,
  CreateVariantRequest,
  UpdateVariantRequest,
} from '@/services/variant-service'
import {
  getVariantSystems,
  getVariantSystem,
  getVariantSystemsLookup,
  createVariantSystem,
  updateVariantSystem,
  deleteVariantSystem,
  getVariantsBySystem,
  createVariant,
  updateVariant,
  deleteVariant,
} from '@/services/variant-service'
import { ApiError } from '@/lib/api-client'

// ── Query Keys ───────────────────────────────────────────

export const variantKeys = {
  all: ['variants'] as const,
  systems: () => [...variantKeys.all, 'systems'] as const,
  systemList: (params: VariantSystemQueryParams) => [...variantKeys.systems(), 'list', params] as const,
  systemDetail: (id: number) => [...variantKeys.systems(), 'detail', id] as const,
  systemLookup: () => [...variantKeys.systems(), 'lookup'] as const,
  bySystem: (systemId: number) => [...variantKeys.all, 'bySystem', systemId] as const,
}

// ── Variant System Queries ───────────────────────────────

export function useVariantSystems(params: VariantSystemQueryParams = {}) {
  return useQuery({
    queryKey: variantKeys.systemList(params),
    queryFn: () => getVariantSystems(params),
    placeholderData: (previousData) => previousData,
  })
}

export function useVariantSystem(id: number | null | undefined) {
  return useQuery({
    queryKey: variantKeys.systemDetail(id!),
    queryFn: () => getVariantSystem(id!),
    enabled: id != null,
  })
}

export function useVariantSystemsLookup() {
  return useQuery({
    queryKey: variantKeys.systemLookup(),
    queryFn: getVariantSystemsLookup,
    staleTime: 10 * 60 * 1000,
  })
}

// ── Variant Queries ──────────────────────────────────────

export function useVariantsBySystem(systemId: number | null | undefined) {
  return useQuery({
    queryKey: variantKeys.bySystem(systemId!),
    queryFn: () => getVariantsBySystem(systemId!),
    enabled: systemId != null,
  })
}

// ── Variant System Mutations ─────────────────────────────

export function useCreateVariantSystem(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateVariantSystemRequest) => createVariantSystem(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: variantKeys.all })
      toast.success('Sistema de variantes creado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Ya existe un sistema con ese nombre.')
      } else {
        toast.error('Error al crear el sistema de variantes')
      }
    },
  })
}

export function useUpdateVariantSystem(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateVariantSystemRequest }) =>
      updateVariantSystem(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: variantKeys.all })
      toast.success('Sistema de variantes actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Ya existe un sistema con ese nombre.')
      } else {
        toast.error('Error al actualizar el sistema de variantes')
      }
    },
  })
}

export function useDeleteVariantSystem(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteVariantSystem(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: variantKeys.all })
      toast.success('Sistema de variantes eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene variantes o productos asociados.',
        )
      } else {
        toast.error('Error al eliminar el sistema de variantes')
      }
    },
  })
}

// ── Variant Mutations ────────────────────────────────────

export function useCreateVariant(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ systemId, data }: { systemId: number; data: CreateVariantRequest }) =>
      createVariant(systemId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: variantKeys.all })
      toast.success('Variante creada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al crear la variante')
      } else {
        toast.error('Error al crear la variante')
      }
    },
  })
}

export function useUpdateVariant(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateVariantRequest }) =>
      updateVariant(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: variantKeys.all })
      toast.success('Variante actualizada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar la variante')
      } else {
        toast.error('Error al actualizar la variante')
      }
    },
  })
}

export function useDeleteVariant(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteVariant(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: variantKeys.all })
      toast.success('Variante eliminada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene registros relacionados.',
        )
      } else {
        toast.error('Error al eliminar la variante')
      }
    },
  })
}
