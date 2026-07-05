import { X } from 'lucide-react'

import { getAgentModeDisplay } from '@/lib/chat/agent-mode-display'
import type { AgentMode } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type ChatModeBadgeProps = {
  mode: AgentMode
  disabled?: boolean
  onClear?: () => void
  className?: string
}

export function ChatModeBadge({
  mode,
  disabled = false,
  onClear,
  className
}: ChatModeBadgeProps): React.JSX.Element {
  const { Icon, label, badgeClassName } = getAgentModeDisplay(mode)
  const canClear = !disabled && onClear && mode !== 'default'

  return (
    <span
      title="Shift+Tab to cycle modes"
      className={cn(
        'inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs font-medium',
        badgeClassName,
        disabled && 'opacity-60',
        className
      )}
    >
      <Icon className="size-3 shrink-0" aria-hidden />
      <span>{label}</span>
      {canClear ? (
        <button
          type="button"
          onClick={onClear}
          className="ml-0.5 rounded-sm opacity-70 hover:opacity-100 focus:outline-none focus:ring-1 focus:ring-ring"
          aria-label={`Clear ${label} mode`}
        >
          <X className="size-3" />
        </button>
      ) : null}
    </span>
  )
}
