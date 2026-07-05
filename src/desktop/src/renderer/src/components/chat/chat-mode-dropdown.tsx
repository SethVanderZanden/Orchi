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
import { listAgentModes } from '@/lib/chat/api'
import { resolveAgentModeOptions } from '@/lib/chat/agent-mode-utils'
import type { AgentMode } from '@/lib/chat/types'
import { agentKeys } from '@/lib/query-keys'
import { cn } from '@/lib/utils'

type ChatModeDropdownProps = {
  mode: AgentMode
  disabled?: boolean
  onModeChange: (mode: AgentMode) => void
  className?: string
}

export function ChatModeDropdown({
  mode,
  disabled = false,
  onModeChange,
  className
}: ChatModeDropdownProps): React.JSX.Element {
  const [open, setOpen] = useState(false)
  const modesQuery = useQuery({
    queryKey: agentKeys.modes(),
    queryFn: listAgentModes,
    staleTime: Infinity
  })

  useEffect(() => {
    setOpen(false)
  }, [mode])

  const modeOptions = resolveAgentModeOptions(modesQuery.data)
  const selectedMode =
    modeOptions.find((option) => option.id.toLowerCase() === mode.toLowerCase()) ?? modeOptions[0]

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          disabled={disabled || modesQuery.isLoading}
          className={cn('h-7 gap-1 px-2 text-xs font-normal text-muted-foreground', className)}
          aria-label="Chat mode"
          title="Shift+Tab to cycle modes"
        >
          <span className="max-w-[8rem] truncate">{selectedMode?.label ?? mode}</span>
          <ChevronDown className="size-3.5 opacity-60" />
        </Button>
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
  )
}
