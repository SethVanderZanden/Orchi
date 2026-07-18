import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown, X } from 'lucide-react'

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { listAgentModes } from '@/lib/chat/api'
import { getAgentModeDisplay } from '@/lib/chat/agent-mode-display'
import { resolveAgentModeOptions } from '@/lib/chat/agent-mode-utils'
import type { AgentMode } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import { cn } from '@/lib/utils'

type ChatModeDropdownProps = {
  mode: AgentMode
  disabled?: boolean
  onModeChange: (mode: AgentMode) => void
  onClear?: () => void
  className?: string
}

export function ChatModeDropdown({
  mode,
  disabled = false,
  onModeChange,
  onClear,
  className
}: ChatModeDropdownProps): React.JSX.Element {
  const [open, setOpen] = useState(false)
  const [prevMode, setPrevMode] = useState(mode)
  if (mode !== prevMode) {
    setPrevMode(mode)
    setOpen(false)
  }

  const modesQuery = useQuery({
    queryKey: agentKeys.modes(),
    queryFn: listAgentModes,
    staleTime: Infinity
  })

  const modeOptions = resolveAgentModeOptions(modesQuery.data)
  const { Icon, label, badgeClassName } = getAgentModeDisplay(mode)
  const canClear = !disabled && onClear && mode !== 'default'
  const isDisabled = disabled || modesQuery.isLoading

  return (
    <div
      className={cn(
        'inline-flex items-center rounded-full border text-xs font-medium',
        badgeClassName,
        isDisabled && 'opacity-60',
        className
      )}
    >
      <DropdownMenu open={open} onOpenChange={setOpen}>
        <DropdownMenuTrigger asChild>
          <button
            type="button"
            disabled={isDisabled}
            className={cn(
              'inline-flex items-center gap-1 rounded-full py-0.5 pl-2',
              canClear ? 'pr-0.5' : 'pr-2',
              'transition-opacity hover:opacity-90 focus:outline-none focus-visible:ring-1 focus-visible:ring-ring',
              'disabled:pointer-events-none'
            )}
            aria-label="Chat mode"
            title="Shift+Tab to cycle modes"
          >
            <Icon className="size-3 shrink-0" aria-hidden />
            <span>{label}</span>
            <ChevronDown className="size-3 shrink-0 opacity-60" aria-hidden />
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="max-h-64 overflow-y-auto">
          <DropdownMenuRadioGroup value={mode} onValueChange={onModeChange}>
            {modeOptions.map((option) => (
              <DropdownMenuRadioItem key={option.id} value={option.id}>
                {option.label}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>
      {canClear ? (
        <button
          type="button"
          onClick={onClear}
          disabled={isDisabled}
          className="mr-1.5 rounded-sm opacity-70 hover:opacity-100 focus:outline-none focus:ring-1 focus:ring-ring disabled:pointer-events-none"
          aria-label={`Clear ${label} mode`}
        >
          <X className="size-3" />
        </button>
      ) : null}
    </div>
  )
}
