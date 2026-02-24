import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type {
  ProductComboQueryParams,
  CreateProductComboRequest,
  UpdateProductComboRequest,
  CreateProductComboDetailRequest,
  UpdateProductComboDetailRequest,
} from '@/services/combo-service'
import {
  getProductCombos,
  getProductCombo,
  getProductCombosLookup,
  createProductCombo,
  updateProductCombo,
  deleteProductCombo,
  getComboDetails,
  createComboDetail,
  updateComboDetail,
  deleteComboDetail,
} from '@/services/combo-service'
import { ApiError } from '@/lib/api-client'

// ── Query Keys ───────────────────────────────────────────

export const comboKeys = {
  all: ['combos'] as const,
  lists: () => [...comboKeys.all, 'list'] as const,
  list: (params: ProductComboQueryParams) => [...comboKeys.lists(), params] as const,
  details: () => [...comboKeys.all, 'detail'] as const,
  detail: (id: number) => [...comboKeys.details(), id] as const,
  lookup: () => [...comboKeys.all, 'lookup'] as const,
  comboDetails: (comboId: number) => [...comboKeys.all, 'comboDetails', comboId] as const,
}

// ── Combo Queries ────────────────────────────────────────

/** Paginated, filterable combo list */
export function useCombos(params: ProductComboQueryParams = {}) {
  return useQuery({
    queryKey: comboKeys.list(params),
    queryFn: () => getProductCombos(params),
    placeholderData: (previousData) => previousData,
  })
}

/** Single combo by ID */
export function useCombo(id: number | null | undefined) {
  return useQuery({
    queryKey: comboKeys.detail(id!),
    queryFn: () => getProductCombo(id!),
    enabled: id != null,
  })
}

/** Combo lookup (long staleTime) */
export function useCombosLookup() {
  return useQuery({
    queryKey: comboKeys.lookup(),
    queryFn: getProductCombosLookup,
    staleTime: 10 * 60 * 1000,
  })
}

/** Combo details for a specific combo */
export function useComboDetails(comboId: number | null | undefined) {
  return useQuery({
    queryKey: comboKeys.comboDetails(comboId!),
    queryFn: () => getComboDetails(comboId!),
    enabled: comboId != null,
  })
}

// ── Combo Mutations ──────────────────────────────────────

/** Create a new combo */
export function useCreateCombo(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateProductComboRequest) => createProductCombo(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: comboKeys.all })
      toast.success('Combo creado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Ya existe un combo con ese nombre.')
      } else {
        toast.error('Error al crear el combo')
      }
    },
  })
}

/** Update an existing combo */
export function useUpdateCombo(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateProductComboRequest }) =>
      updateProductCombo(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: comboKeys.all })
      toast.success('Combo actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Ya existe un combo con ese nombre.')
      } else {
        toast.error('Error al actualizar el combo')
      }
    },
  })
}

/** Delete a combo (handles 409 conflict) */
export function useDeleteCombo(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteProductCombo(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: comboKeys.all })
      toast.success('Combo eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene precios asociados.',
        )
      } else {
        toast.error('Error al eliminar el combo')
      }
    },
  })
}

// ── Combo Detail Mutations ───────────────────────────────

/** Create a combo detail line */
export function useCreateComboDetail(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      data,
    }: {
      comboId: number
      data: CreateProductComboDetailRequest
    }) => createComboDetail(comboId, data),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.comboDetails(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.all })
      toast.success('Detalle agregado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al agregar el detalle')
      } else {
        toast.error('Error al agregar el detalle')
      }
    },
  })
}

/** Update a combo detail line */
export function useUpdateComboDetail(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      detailId,
      data,
    }: {
      comboId: number
      detailId: number
      data: UpdateProductComboDetailRequest
    }) => updateComboDetail(comboId, detailId, data),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.comboDetails(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.all })
      toast.success('Detalle actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar el detalle')
      } else {
        toast.error('Error al actualizar el detalle')
      }
    },
  })
}

/** Delete a combo detail line */
export function useDeleteComboDetail(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      detailId,
    }: {
      comboId: number
      detailId: number
    }) => deleteComboDetail(comboId, detailId),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.comboDetails(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.all })
      toast.success('Detalle eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al eliminar el detalle')
      } else {
        toast.error('Error al eliminar el detalle')
      }
    },
  })
}
