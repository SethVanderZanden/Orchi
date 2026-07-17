import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { listAgentContextSizes } from '@/lib/chat/agent-context-sizes-api'
import type { AgentMode } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import { cn } from '@/lib/utils'

export const DEFAULT_CONTEXT_SIZE_VALUE = '__default__'
const ONE_HOUR_MS = 60 * 60 * 1000

type ChatContextSizeSelectorProps = {
  agentId: string
  contextSizeId: string | null
  mode?: AgentMode
  disabled?: boolean
  error?: string | null
  onContextSizeChange: (contextSizeId: string | null) => void
  className?: string
  compact?: boolean
}

function toRadioValue(contextSizeId: string | null): string {
  return contextSizeId ?? DEFAULT_CONTEXT_SIZE_VALUE
}

function fromRadioValue(value: string): string | null {
  return value === DEFAULT_CONTEXT_SIZE_VALUE ? null : value
}

export function ChatContextSizeSelector({
  agentId,
  contextSizeId,
  mode,
  disabled = false,
  error = null,
  onContextSizeChange,
  className,
  compact = false
}: ChatContextSizeSelectorProps): React.JSX.Element | null {
  const [open, setOpen] = useState(false)
  const [prevSelection, setPrevSelection] = useState({ contextSizeId, mode })
  if (contextSizeId !== prevSelection.contextSizeId || mode !== prevSelection.mode) {
    setPrevSelection({ contextSizeId, mode })
    setOpen(false)
  }

  const sizesQuery = useQuery({
    queryKey: agentKeys.contextSizes(agentId),
    queryFn: () => listAgentContextSizes(agentId, false),
    staleTime: ONE_HOUR_MS
  })

  const enabledSizes = sizesQuery.data?.contextSizes ?? []
  if (!sizesQuery.isLoading && enabledSizes.length === 0) {
    return null
  }

  const selectedSize = enabledSizes.find((size) => size.id === contextSizeId)
  const triggerLabel = selectedSize?.label ?? (compact ? 'Ctx' : 'Default context')

  return (
    <div className={cn(compact ? 'inline-flex items-center' : 'space-y-1', className)}>
      <DropdownMenu open={open} onOpenChange={setOpen}>
        <DropdownMenuTrigger asChild>
          <Button
            variant={compact ? 'ghost' : 'outline'}
            size="sm"
            disabled={disabled || sizesQuery.isLoading}
            className={cn(
              'h-7 gap-1.5 text-xs font-normal',
              compact ? 'px-2 text-muted-foreground' : 'px-2.5'
            )}
            aria-label="Chat context size"
          >
            <span className={cn('truncate', compact ? 'max-w-[5rem]' : 'max-w-[12rem]')}>
              {triggerLabel}
            </span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
          <DropdownMenuRadioGroup
            value={toRadioValue(contextSizeId)}
            onValueChange={(value) => onContextSizeChange(fromRadioValue(value))}
          >
            <DropdownMenuRadioItem value={DEFAULT_CONTEXT_SIZE_VALUE}>
              {compact ? 'Auto' : 'Default (CLI)'}
            </DropdownMenuRadioItem>
            {enabledSizes.map((size) => (
              <DropdownMenuRadioItem key={size.id} value={size.id}>
                {size.label}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>
      {!compact && error ? <p className="text-[11px] text-destructive">{error}</p> : null}
      {compact && error ? <span className="sr-only">{error}</span> : null}
    </div>
  )
}
