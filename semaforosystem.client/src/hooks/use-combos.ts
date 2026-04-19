import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type {
  ProductComboQueryParams,
  CreateProductComboRequest,
  UpdateProductComboRequest,
  CreateVisualDefinitionRequest,
  UpdateVisualDefinitionRequest,
  CreateComponentRequest,
  UpdateComponentRequest,
} from '@/services/combo-service'
import {
  getProductCombos,
  getProductCombo,
  getProductCombosLookup,
  getComboVisualDefinitions,
  createProductCombo,
  updateProductCombo,
  deleteProductCombo,
  createComboVisualDefinition,
  updateComboVisualDefinition,
  deleteComboVisualDefinition,
  createComboComponent,
  updateComboComponent,
  deleteComboComponent,
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
  visualDefinitions: (comboId: number) => [...comboKeys.all, 'visualDefinitions', comboId] as const,
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

/** Visual definitions for a specific combo */
export function useComboVisualDefinitions(comboId: number | null | undefined) {
  return useQuery({
    queryKey: comboKeys.visualDefinitions(comboId!),
    queryFn: () => getComboVisualDefinitions(comboId!),
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

// ── Visual Definition Mutations ──────────────────────────

/** Create a visual definition under a combo */
export function useCreateVisualDefinition(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      data,
    }: {
      comboId: number
      data: CreateVisualDefinitionRequest
    }) => createComboVisualDefinition(comboId, data),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.visualDefinitions(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.lists() })
      toast.success('Visual definition creada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al crear la visual definition')
      } else {
        toast.error('Error al crear la visual definition')
      }
    },
  })
}

/** Update a visual definition */
export function useUpdateVisualDefinition(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      vdId,
      data,
    }: {
      comboId: number
      vdId: number
      data: UpdateVisualDefinitionRequest
    }) => updateComboVisualDefinition(comboId, vdId, data),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.visualDefinitions(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.lists() })
      toast.success('Visual definition actualizada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar la visual definition')
      } else {
        toast.error('Error al actualizar la visual definition')
      }
    },
  })
}

/** Delete a visual definition */
export function useDeleteVisualDefinition(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      vdId,
    }: {
      comboId: number
      vdId: number
    }) => deleteComboVisualDefinition(comboId, vdId),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.visualDefinitions(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.lists() })
      toast.success('Visual definition eliminada exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al eliminar la visual definition')
      } else {
        toast.error('Error al eliminar la visual definition')
      }
    },
  })
}

// ── Component Mutations ──────────────────────────────────

/** Add a component to a visual definition */
export function useCreateComponent(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      vdId,
      data,
    }: {
      comboId: number
      vdId: number
      data: CreateComponentRequest
    }) => createComboComponent(comboId, vdId, data),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.visualDefinitions(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.lists() })
      toast.success('Componente agregado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al agregar el componente')
      } else {
        toast.error('Error al agregar el componente')
      }
    },
  })
}

/** Update a component */
export function useUpdateComponent(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      vdId,
      compId,
      data,
    }: {
      comboId: number
      vdId: number
      compId: number
      data: UpdateComponentRequest
    }) => updateComboComponent(comboId, vdId, compId, data),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.visualDefinitions(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.lists() })
      toast.success('Componente actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar el componente')
      } else {
        toast.error('Error al actualizar el componente')
      }
    },
  })
}

/** Delete a component */
export function useDeleteComponent(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({
      comboId,
      vdId,
      compId,
    }: {
      comboId: number
      vdId: number
      compId: number
    }) => deleteComboComponent(comboId, vdId, compId),
    onSuccess: (_resp, vars) => {
      queryClient.invalidateQueries({ queryKey: comboKeys.visualDefinitions(vars.comboId) })
      queryClient.invalidateQueries({ queryKey: comboKeys.lists() })
      toast.success('Componente eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al eliminar el componente')
      } else {
        toast.error('Error al eliminar el componente')
      }
    },
  })
}
