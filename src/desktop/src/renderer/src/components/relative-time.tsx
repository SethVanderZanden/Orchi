import { formatDistanceToNow } from 'date-fns'

import { cn } from '@/lib/utils'

type RelativeTimeProps = {
  value: string | Date
  className?: string
  compact?: boolean
}

function formatCompactDistance(date: Date): string {
  const seconds = Math.max(0, Math.floor((Date.now() - date.getTime()) / 1000))

  if (seconds < 45) {
    return 'now'
  }

  const minutes = Math.floor(seconds / 60)
  if (minutes < 60) {
    return `${minutes}m`
  }

  const hours = Math.floor(minutes / 60)
  if (hours < 24) {
    return `${hours}h`
  }

  const days = Math.floor(hours / 24)
  if (days < 7) {
    return `${days}d`
  }

  const weeks = Math.floor(days / 7)
  if (weeks < 5) {
    return `${weeks}w`
  }

  const months = Math.floor(days / 30)
  if (months < 12) {
    return `${months}mo`
  }

  return `${Math.floor(days / 365)}y`
}

export function RelativeTime({
  value,
  className,
  compact = false
}: RelativeTimeProps): React.JSX.Element {
  const date = typeof value === 'string' ? new Date(value) : value
  const label = compact
    ? formatCompactDistance(date)
    : formatDistanceToNow(date, { addSuffix: true })

  return (
    <time
      dateTime={date.toISOString()}
      title={formatDistanceToNow(date, { addSuffix: true })}
      className={cn('text-xs text-muted-foreground', className)}
    >
      {label}
    </time>
  )
}
