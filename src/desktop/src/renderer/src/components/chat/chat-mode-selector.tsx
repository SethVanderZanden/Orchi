import { useQuery } from '@tanstack/react-query'

import { Badge } from '@/components/ui/badge'
import { listAgentModes } from '@/lib/chat/api'
import type { AgentMode, AgentModeOption } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

const FALLBACK_MODE_OPTIONS: AgentModeOption[] = [
  { id: 'default', label: 'Default', description: null },
  {
    id: 'orchestration',
    label: 'Orchestration',
    description: 'Splits work into plans that can be kicked off to implementation agents.'
  },
  {
    id: 'review',
    label: 'Review',
    description: 'Produces review plans after implementation to verify work against the original plan.'
  }
]

export function getNextAgentMode(current: AgentMode, options: AgentModeOption[]): AgentMode {
  if (options.length === 0) {
    return current
  }

  const currentIndex = options.findIndex((option) => option.id === current)
  const nextIndex = currentIndex === -1 ? 0 : (currentIndex + 1) % options.length
  return options[nextIndex]!.id
}

export const CHAT_MODE_FALLBACK_OPTIONS = FALLBACK_MODE_OPTIONS

type ChatModeSelectorProps = {
  mode: AgentMode
  disabled?: boolean
  error?: string | null
  onModeChange: (mode: AgentMode) => void
  className?: string
}

export function ChatModeSelector({
  mode,
  disabled = false,
  error = null,
  onModeChange,
  className
}: ChatModeSelectorProps): React.JSX.Element {
  const modesQuery = useQuery({
    queryKey: ['agent-modes'],
    queryFn: listAgentModes,
    staleTime: Infinity
  })

  const modeOptions = modesQuery.data ?? FALLBACK_MODE_OPTIONS
  const selectedMode = modeOptions.find((option) => option.id === mode) ?? modeOptions[0]

  return (
    <div className={cn('space-y-1', className)}>
      <div
        role="radiogroup"
        aria-label="Chat mode"
        className="flex flex-wrap items-center gap-2"
      >
        {modeOptions.map((option) => {
          const isActive = option.id === mode

          return (
            <button
              key={option.id}
              type="button"
              role="radio"
              aria-checked={isActive}
              aria-disabled={disabled}
              disabled={disabled}
              onClick={() => onModeChange(option.id)}
              className={cn(
                'rounded-md focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
                disabled && 'pointer-events-none opacity-60'
              )}
            >
              <Badge variant={isActive ? 'default' : 'outline'} className="cursor-pointer">
                {option.label}
              </Badge>
            </button>
          )
        })}
      </div>
      {error ? (
        <p className="text-[11px] text-destructive">{error}</p>
      ) : null}
      {disabled ? (
        <p className="text-[11px] text-muted-foreground">
          Mode cannot be changed while the agent is running.
        </p>
      ) : (
        <>
          {selectedMode?.description ? (
            <p className="text-[11px] text-muted-foreground">{selectedMode.description}</p>
          ) : null}
          <p className="text-[11px] text-muted-foreground">Mode applies to the next message.</p>
          <p className="sr-only">Shift+Tab to cycle modes</p>
        </>
      )}
    </div>
  )
}

export type CreateChatOptions = {
  workspacePath: string
}
