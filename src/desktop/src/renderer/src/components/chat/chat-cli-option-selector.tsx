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
import { listAgentCliOptions } from '@/lib/chat/agent-cli-options-api'
import type { AgentCliOptionKind, AgentMode } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import { cn } from '@/lib/utils'

export const DEFAULT_CLI_OPTION_VALUE = '__default__'
const ONE_HOUR_MS = 60 * 60 * 1000

const KIND_LABELS: Record<AgentCliOptionKind, { default: string; compact: string; aria: string }> =
  {
    model_reasoning_effort: {
      default: 'Default reasoning',
      compact: 'Reason',
      aria: 'Chat reasoning effort'
    },
    approval_policy: {
      default: 'Default approval',
      compact: 'Approve',
      aria: 'Chat approval policy'
    }
  }

type ChatCliOptionSelectorProps = {
  agentId: string
  kind: AgentCliOptionKind
  optionId: string | null
  mode?: AgentMode
  disabled?: boolean
  error?: string | null
  onOptionChange: (optionId: string | null) => void
  className?: string
  compact?: boolean
}

function toRadioValue(optionId: string | null): string {
  return optionId ?? DEFAULT_CLI_OPTION_VALUE
}

function fromRadioValue(value: string): string | null {
  return value === DEFAULT_CLI_OPTION_VALUE ? null : value
}

export function ChatCliOptionSelector({
  agentId,
  kind,
  optionId,
  mode,
  disabled = false,
  error = null,
  onOptionChange,
  className,
  compact = false
}: ChatCliOptionSelectorProps): React.JSX.Element | null {
  const [open, setOpen] = useState(false)
  const [prevSelection, setPrevSelection] = useState({ optionId, mode, kind })
  if (
    optionId !== prevSelection.optionId ||
    mode !== prevSelection.mode ||
    kind !== prevSelection.kind
  ) {
    setPrevSelection({ optionId, mode, kind })
    setOpen(false)
  }

  const optionsQuery = useQuery({
    queryKey: agentKeys.cliOptions(agentId, kind),
    queryFn: () => listAgentCliOptions(agentId, kind, false),
    staleTime: ONE_HOUR_MS
  })

  const enabledOptions = optionsQuery.data?.options ?? []
  if (!optionsQuery.isLoading && enabledOptions.length === 0) {
    return null
  }

  const labels = KIND_LABELS[kind]
  const selected = enabledOptions.find((option) => option.id === optionId)
  const triggerLabel = selected?.label ?? (compact ? labels.compact : labels.default)

  return (
    <div className={cn(compact ? 'inline-flex items-center' : 'space-y-1', className)}>
      <DropdownMenu open={open} onOpenChange={setOpen}>
        <DropdownMenuTrigger asChild>
          <Button
            variant={compact ? 'ghost' : 'outline'}
            size="sm"
            disabled={disabled || optionsQuery.isLoading}
            className={cn(
              'h-7 gap-1.5 text-xs font-normal',
              compact ? 'px-2 text-muted-foreground' : 'px-2.5'
            )}
            aria-label={labels.aria}
          >
            <span className={cn('truncate', compact ? 'max-w-[5rem]' : 'max-w-[12rem]')}>
              {triggerLabel}
            </span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
          <DropdownMenuRadioGroup
            value={toRadioValue(optionId)}
            onValueChange={(value) => onOptionChange(fromRadioValue(value))}
          >
            <DropdownMenuRadioItem value={DEFAULT_CLI_OPTION_VALUE}>
              {compact ? 'Auto' : 'Default (CLI)'}
            </DropdownMenuRadioItem>
            {enabledOptions.map((option) => (
              <DropdownMenuRadioItem key={option.id} value={option.id}>
                {option.label}
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
