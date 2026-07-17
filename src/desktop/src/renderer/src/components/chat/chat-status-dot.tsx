import { cn } from '@/lib/utils'
import type { ChatSidebarStatusVariant } from '@/lib/chat/chat-sidebar-status'
import { getChatSidebarStatusLabel } from '@/lib/chat/chat-sidebar-status'

type ChatStatusDotProps = {
  variant: ChatSidebarStatusVariant
  className?: string
}

export function ChatStatusDot({ variant, className }: ChatStatusDotProps): React.JSX.Element {
  const label = getChatSidebarStatusLabel(variant)

  return (
    <span
      className={cn(
        'size-1.5 shrink-0 rounded-full',
        variant === 'standard' && 'bg-muted-foreground/50',
        variant === 'active' && 'bg-amber-500 [animation:sidebar-pulse_1.4s_ease-in-out_infinite]',
        variant === 'attention' && 'bg-primary',
        className
      )}
      aria-hidden={variant === 'standard'}
      role={variant === 'standard' ? undefined : 'status'}
    >
      {label ? <span className="sr-only">{label}</span> : null}
    </span>
  )
}
