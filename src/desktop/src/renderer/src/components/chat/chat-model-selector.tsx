import { useEffect, useState } from 'react'
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
import { listAgentModels } from '@/lib/chat/agent-models-api'
import type { AgentMode } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import { cn } from '@/lib/utils'

export const DEFAULT_MODEL_VALUE = '__default__'
const ONE_HOUR_MS = 60 * 60 * 1000

type ChatModelSelectorProps = {
  agentId: string
  modelId: string | null
  mode?: AgentMode
  disabled?: boolean
  error?: string | null
  onModelChange: (modelId: string | null) => void
  className?: string
  compact?: boolean
}

function toRadioValue(modelId: string | null): string {
  return modelId ?? DEFAULT_MODEL_VALUE
}

function fromRadioValue(value: string): string | null {
  return value === DEFAULT_MODEL_VALUE ? null : value
}

export function ChatModelSelector({
  agentId,
  modelId,
  mode,
  disabled = false,
  error = null,
  onModelChange,
  className,
  compact = false
}: ChatModelSelectorProps): React.JSX.Element {
  const [open, setOpen] = useState(false)
  const modelsQuery = useQuery({
    queryKey: agentKeys.models(agentId),
    queryFn: () => listAgentModels(agentId, false),
    staleTime: ONE_HOUR_MS
  })

  useEffect(() => {
    setOpen(false)
  }, [modelId, mode])

  const enabledModels = modelsQuery.data?.models ?? []
  const selectedModel = enabledModels.find((model) => model.id === modelId)
  const triggerLabel = selectedModel?.label ?? (compact ? 'Auto' : 'Default (CLI)')

  return (
    <div className={cn(compact ? 'inline-flex items-center' : 'space-y-1', className)}>
      <DropdownMenu open={open} onOpenChange={setOpen}>
        <DropdownMenuTrigger asChild>
          <Button
            variant={compact ? 'ghost' : 'outline'}
            size="sm"
            disabled={disabled || modelsQuery.isLoading}
            className={cn(
              'h-7 gap-1.5 text-xs font-normal',
              compact ? 'px-2 text-muted-foreground' : 'px-2.5'
            )}
            aria-label="Chat model"
          >
            <span className={cn('truncate', compact ? 'max-w-[6rem]' : 'max-w-[12rem]')}>
              {triggerLabel}
            </span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
          <DropdownMenuRadioGroup
            value={toRadioValue(modelId)}
            onValueChange={(value) => onModelChange(fromRadioValue(value))}
          >
            <DropdownMenuRadioItem value={DEFAULT_MODEL_VALUE}>
              {compact ? 'Auto (CLI default)' : 'Default (CLI)'}
            </DropdownMenuRadioItem>
            {enabledModels.map((model) => (
              <DropdownMenuRadioItem key={model.id} value={model.id}>
                {model.label}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>
      {!compact && error ? (
        <p className="text-[11px] text-destructive">{error}</p>
      ) : null}
      {!compact && disabled ? (
        <p className="text-[11px] text-muted-foreground">
          Model cannot be changed while the agent is running.
        </p>
      ) : null}
      {!compact && !disabled ? (
        <p className="text-[11px] text-muted-foreground">Model applies to the next message.</p>
      ) : null}
      {compact && error ? (
        <span className="sr-only">{error}</span>
      ) : null}
    </div>
  )
}
