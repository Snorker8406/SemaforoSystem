import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type {
  ProductQueryParams,
  CreateProductRequest,
  UpdateProductRequest,
} from '@/services/product-service'
import {
  getProducts,
  getProduct,
  createProduct,
  updateProduct,
  deleteProduct,
  getBrands,
  getCategories,
  getProductSchools,
  getProductItemDefinitions,
  getProductVisualDefinitions,
} from '@/services/product-service'
import { ApiError } from '@/lib/api-client'

// ── Query Keys ───────────────────────────────────────────

export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (params: ProductQueryParams) => [...productKeys.lists(), params] as const,
  details: () => [...productKeys.all, 'detail'] as const,
  detail: (id: number) => [...productKeys.details(), id] as const,
  brands: () => ['product-brands'] as const,
  categories: () => ['product-categories'] as const,
  schools: (productId: number) => [...productKeys.all, 'schools', productId] as const,
  itemDefinitions: (productId: number) => [...productKeys.all, 'item-definitions', productId] as const,
  visualDefinitions: (productId: number) => [...productKeys.all, 'visual-definitions', productId] as const,
}

// ── Queries ──────────────────────────────────────────────

/** Paginated, filterable product list */
export function useProducts(params: ProductQueryParams = {}) {
  return useQuery({
    queryKey: productKeys.list(params),
    queryFn: () => getProducts(params),
    placeholderData: (previousData) => previousData,
  })
}

/** Single product by ID */
export function useProduct(id: number | null | undefined) {
  return useQuery({
    queryKey: productKeys.detail(id!),
    queryFn: () => getProduct(id!),
    enabled: id != null,
  })
}

/** Brand lookup (long staleTime — rarely changes) */
export function useBrands() {
  return useQuery({
    queryKey: productKeys.brands(),
    queryFn: getBrands,
    staleTime: 10 * 60 * 1000, // 10 min
  })
}

/** Category lookup (long staleTime — rarely changes) */
export function useCategories() {
  return useQuery({
    queryKey: productKeys.categories(),
    queryFn: getCategories,
    staleTime: 10 * 60 * 1000, // 10 min
  })
}

/** Schools associated with a product */
export function useProductSchools(productId: number | null | undefined) {
  return useQuery({
    queryKey: productKeys.schools(productId!),
    queryFn: () => getProductSchools(productId!),
    enabled: productId != null,
    staleTime: 5 * 60 * 1000, // 5 min
  })
}

/** Item definitions (variant-derived items) for a product */
export function useProductItemDefinitions(productId: number | null | undefined) {
  return useQuery({
    queryKey: productKeys.itemDefinitions(productId!),
    queryFn: () => getProductItemDefinitions(productId!),
    enabled: productId != null,
    staleTime: 5 * 60 * 1000, // 5 min
  })
}

/** Visual definitions grouped with their item definitions (sizes) */
export function useProductVisualDefinitions(productId: number | null | undefined) {
  return useQuery({
    queryKey: productKeys.visualDefinitions(productId!),
    queryFn: () => getProductVisualDefinitions(productId!),
    enabled: productId != null,
    staleTime: 5 * 60 * 1000, // 5 min
  })
}

// ── Mutations ────────────────────────────────────────────

/** Create a new product */
export function useCreateProduct(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateProductRequest) => createProduct(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      toast.success('Producto creado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al crear el producto')
      } else {
        toast.error('Error al crear el producto')
      }
    },
  })
}

/** Update an existing product */
export function useUpdateProduct(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateProductRequest }) =>
      updateProduct(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      toast.success('Producto actualizado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError) {
        const body = error.body as { message?: string }
        toast.error(body?.message ?? 'Error al actualizar el producto')
      } else {
        toast.error('Error al actualizar el producto')
      }
    },
  })
}

/** Delete a product (handles 409 conflict) */
export function useDeleteProduct(options?: { onSuccess?: () => void }) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: number) => deleteProduct(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: productKeys.all })
      toast.success('Producto eliminado exitosamente')
      options?.onSuccess?.()
    },
    onError: (error: Error) => {
      if (error instanceof ApiError && error.status === 409) {
        const body = error.body as { message?: string }
        toast.error(
          body?.message ?? 'No se puede eliminar: tiene registros relacionados.',
        )
      } else {
        toast.error('Error al eliminar el producto')
      }
    },
  })
}
