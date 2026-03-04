import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'

import type { CreateEntryRequest } from '@/services/inventory-service'
import { getSites, createEntry } from '@/services/inventory-service'
import { ApiError } from '@/lib/api-client'

// ── Query Keys ───────────────────────────────────────────

export const inventoryKeys = {
  all: ['inventory'] as const,
  sites: () => [...inventoryKeys.all, 'sites'] as const,
  transactions: () => [...inventoryKeys.all, 'transactions'] as const,
}

// ── Sites ────────────────────────────────────────────────

export function useSites() {
  return useQuery({
    queryKey: inventoryKeys.sites(),
    queryFn: getSites,
    staleTime: 10 * 60 * 1000,
  })
}

// ── Entry mutation ───────────────────────────────────────

export function useCreateEntry() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateEntryRequest) => createEntry(request),
    onSuccess: () => {
      toast.success('Existencias agregadas exitosamente')
      queryClient.invalidateQueries({ queryKey: inventoryKeys.all })
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        const body = error.body as { error?: string; message?: string } | undefined
        toast.error(body?.error ?? body?.message ?? 'Error al agregar existencias')
      } else {
        toast.error('Error al agregar existencias')
      }
    },
  })
}
