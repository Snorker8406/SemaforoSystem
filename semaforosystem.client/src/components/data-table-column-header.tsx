import { useState, useRef, useEffect, useCallback } from 'react'
import { ArrowDownIcon, ArrowUpIcon, ArrowUpDownIcon, FilterIcon } from 'lucide-react'

import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

// ── Types ────────────────────────────────────────────────

export type ColumnFilterType =
  | { kind: 'text'; value: string; onChange: (value: string) => void; placeholder?: string; debounceMs?: number }
  | { kind: 'select'; value: string; onChange: (value: string) => void; options: { label: string; value: string }[]; placeholder?: string }
  | { kind: 'boolean'; value: string; onChange: (value: string) => void; trueLabel?: string; falseLabel?: string }

interface DataTableColumnHeaderProps {
  title: string
  /** Sort key that maps to the backend `sortBy` parameter. If undefined, column is not sortable. */
  sortKey?: string
  /** Current active sort key from query params */
  currentSortBy?: string
  /** Current sort direction */
  currentSortDesc?: boolean
  /** Called when user selects a sort option */
  onSort?: (sortBy: string | undefined, sortDesc: boolean) => void
  /** Optional column filter configuration */
  filter?: ColumnFilterType
  className?: string
}

// ── Debounced text input (internal) ──────────────────────

function DebouncedInput({
  value: externalValue,
  onChange,
  debounceMs = 500,
  ...props
}: {
  value: string
  onChange: (value: string) => void
  debounceMs?: number
} & Omit<React.ComponentProps<typeof Input>, 'onChange' | 'value'>) {
  const [localValue, setLocalValue] = useState(externalValue)
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null)
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange

  // Sync local value when external value changes (e.g. "clear all filters")
  useEffect(() => {
    setLocalValue(externalValue)
  }, [externalValue])

  // Cleanup timer on unmount
  useEffect(() => {
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current)
    }
  }, [])

  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const next = e.target.value
      setLocalValue(next)
      if (timerRef.current) clearTimeout(timerRef.current)
      timerRef.current = setTimeout(() => {
        onChangeRef.current(next)
      }, debounceMs)
    },
    [debounceMs],
  )

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent<HTMLInputElement>) => {
      e.stopPropagation()
      if (e.key === 'Enter') {
        // Flush immediately on Enter
        if (timerRef.current) clearTimeout(timerRef.current)
        onChangeRef.current(localValue)
      }
    },
    [localValue],
  )

  return (
    <Input
      {...props}
      value={localValue}
      onChange={handleChange}
      onClick={(e) => e.stopPropagation()}
      onKeyDown={handleKeyDown}
    />
  )
}

// ── Component ────────────────────────────────────────────

export function DataTableColumnHeader({
  title,
  sortKey,
  currentSortBy,
  currentSortDesc,
  onSort,
  filter,
  className,
}: DataTableColumnHeaderProps) {
  const isSorted = sortKey != null && currentSortBy === sortKey
  const isAsc = isSorted && !currentSortDesc
  const isDesc = isSorted && currentSortDesc

  const hasActiveFilter =
    filter &&
    ((filter.kind === 'text' && filter.value !== '') ||
      (filter.kind === 'select' && filter.value !== '' && filter.value !== 'all') ||
      (filter.kind === 'boolean' && filter.value !== '' && filter.value !== 'all'))

  if (!sortKey && !filter) {
    return <span className={className}>{title}</span>
  }

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          variant='ghost'
          size='sm'
          className={cn('-ml-3 h-8 gap-1 data-[state=open]:bg-accent', className)}
        >
          <span>{title}</span>
          {hasActiveFilter && (
            <FilterIcon className='size-3 text-primary' />
          )}
          {isDesc ? (
            <ArrowDownIcon className='size-3.5 text-primary' />
          ) : isAsc ? (
            <ArrowUpIcon className='size-3.5 text-primary' />
          ) : sortKey ? (
            <ArrowUpDownIcon className='text-muted-foreground/70 size-3.5' />
          ) : null}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align='start' className='w-48' onCloseAutoFocus={(e) => e.preventDefault()}>
        {/* Sort options */}
        {sortKey && (
          <>
            <DropdownMenuItem
              onClick={() => onSort?.(sortKey, false)}
              className={cn(isAsc && 'bg-accent')}
            >
              <ArrowUpIcon className='text-muted-foreground size-3.5' />
              Ascendente
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={() => onSort?.(sortKey, true)}
              className={cn(isDesc && 'bg-accent')}
            >
              <ArrowDownIcon className='text-muted-foreground size-3.5' />
              Descendente
            </DropdownMenuItem>
            {isSorted && (
              <DropdownMenuItem onClick={() => onSort?.(undefined, false)}>
                <ArrowUpDownIcon className='text-muted-foreground size-3.5' />
                Quitar orden
              </DropdownMenuItem>
            )}
          </>
        )}

        {/* Filter section */}
        {filter && (
          <>
            {sortKey && <DropdownMenuSeparator />}

            <div className='px-2 py-1.5'>
              <p className='text-muted-foreground mb-1.5 text-xs font-medium'>
                Filtrar
              </p>
              {filter.kind === 'text' && (
                <DebouncedInput
                  placeholder={filter.placeholder ?? 'Filtrar...'}
                  value={filter.value}
                  onChange={filter.onChange}
                  debounceMs={filter.debounceMs ?? 500}
                  className='h-8 text-xs'
                />
              )}
              {filter.kind === 'select' && (
                <Select value={filter.value || 'all'} onValueChange={filter.onChange}>
                  <SelectTrigger className='h-8 text-xs'>
                    <SelectValue placeholder={filter.placeholder ?? 'Todos'} />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value='all'>Todos</SelectItem>
                    {filter.options.map((opt) => (
                      <SelectItem key={opt.value} value={opt.value}>
                        {opt.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
              {filter.kind === 'boolean' && (
                <Select value={filter.value || 'all'} onValueChange={filter.onChange}>
                  <SelectTrigger className='h-8 text-xs'>
                    <SelectValue placeholder='Todos' />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value='all'>Todos</SelectItem>
                    <SelectItem value='true'>
                      {filter.trueLabel ?? 'Sí'}
                    </SelectItem>
                    <SelectItem value='false'>
                      {filter.falseLabel ?? 'No'}
                    </SelectItem>
                  </SelectContent>
                </Select>
              )}
            </div>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
