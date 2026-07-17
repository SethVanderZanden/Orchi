import { PencilLine } from 'lucide-react'

import { cn } from '@/lib/utils'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import { getChatStatusVariantLabel } from '@/lib/chat/chat-status-variant'

type ChatStatusDotProps = {
  variant: ChatStatusVariant
  className?: string
}

export function ChatStatusDot({ variant, className }: ChatStatusDotProps): React.JSX.Element {
  const label = getChatStatusVariantLabel(variant)

  if (variant === 'draft') {
    return (
      <PencilLine
        className={cn('size-3 shrink-0 text-sky-500', className)}
        aria-hidden={false}
        role="status"
        aria-label={label}
      />
    )
  }

  return (
    <span
      className={cn(
        'size-1.5 shrink-0 rounded-full',
        variant === 'standard' && 'bg-muted-foreground/50',
        variant === 'active' && 'bg-amber-500 [animation:status-pulse_1.4s_ease-in-out_infinite]',
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
