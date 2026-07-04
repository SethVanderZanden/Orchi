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
import { cn } from '@/lib/utils'

export const DEFAULT_MODEL_VALUE = '__default__'
const ONE_HOUR_MS = 60 * 60 * 1000

type ChatModelSelectorProps = {
  agentId: string
  modelId: string | null
  disabled?: boolean
  error?: string | null
  onModelChange: (modelId: string | null) => void
  className?: string
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
  disabled = false,
  error = null,
  onModelChange,
  className
}: ChatModelSelectorProps): React.JSX.Element {
  const modelsQuery = useQuery({
    queryKey: ['agent-models', agentId],
    queryFn: () => listAgentModels(agentId, false),
    staleTime: ONE_HOUR_MS
  })

  const enabledModels = modelsQuery.data?.models ?? []
  const selectedModel = enabledModels.find((model) => model.id === modelId)
  const triggerLabel = selectedModel?.label ?? 'Default (CLI)'

  return (
    <div className={cn('space-y-1', className)}>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="outline"
            size="sm"
            disabled={disabled || modelsQuery.isLoading}
            className="h-7 gap-1.5 px-2.5 text-xs font-normal"
            aria-label="Chat model"
          >
            <span className="max-w-[12rem] truncate">{triggerLabel}</span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
          <DropdownMenuRadioGroup
            value={toRadioValue(modelId)}
            onValueChange={(value) => onModelChange(fromRadioValue(value))}
          >
            <DropdownMenuRadioItem value={DEFAULT_MODEL_VALUE}>Default (CLI)</DropdownMenuRadioItem>
            {enabledModels.map((model) => (
              <DropdownMenuRadioItem key={model.id} value={model.id}>
                {model.label}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>
      {error ? (
        <p className="text-[11px] text-destructive">{error}</p>
      ) : null}
      {disabled ? (
        <p className="text-[11px] text-muted-foreground">
          Model cannot be changed while the agent is running.
        </p>
      ) : (
        <p className="text-[11px] text-muted-foreground">Model applies to the next message.</p>
      )}
    </div>
  )
}
