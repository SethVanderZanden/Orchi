import { PencilLine } from 'lucide-react'

import { getAgentModeDisplay } from '@/lib/chat/agent-mode-display'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import { getChatStatusVariantLabel } from '@/lib/chat/chat-status-variant'
import type { AgentMode } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type ChatStatusDotProps = {
  variant: ChatStatusVariant
  mode?: AgentMode
  className?: string
}

export function ChatStatusDot({
  variant,
  mode = 'default',
  className
}: ChatStatusDotProps): React.JSX.Element {
  const statusLabel = getChatStatusVariantLabel(variant)
  const { Icon, label: modeLabel } = getAgentModeDisplay(mode)
  const ariaLabel = statusLabel ? `${statusLabel}, ${modeLabel}` : modeLabel

  if (variant === 'draft') {
    return (
      <PencilLine
        className={cn('size-3.5 shrink-0 text-sky-500', className)}
        aria-hidden={false}
        role="status"
        aria-label={getChatStatusVariantLabel('draft')}
      />
    )
  }

  return (
    <Icon
      className={cn(
        'size-3.5 shrink-0',
        variant === 'standard' && 'text-muted-foreground/50',
        variant === 'active' && 'text-amber-500 [animation:status-pulse_1.4s_ease-in-out_infinite]',
        variant === 'attention' && 'text-primary',
        className
      )}
      aria-hidden={false}
      role="status"
      aria-label={ariaLabel}
    />
  )
}
