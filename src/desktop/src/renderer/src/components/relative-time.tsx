import { formatDistanceToNow } from 'date-fns'

import { cn } from '@/lib/utils'

type RelativeTimeProps = {
  value: string | Date
  className?: string
}

export function RelativeTime({ value, className }: RelativeTimeProps): React.JSX.Element {
  const date = typeof value === 'string' ? new Date(value) : value
  const label = formatDistanceToNow(date, { addSuffix: true })

  return (
    <time dateTime={date.toISOString()} className={cn('text-xs text-muted-foreground', className)}>
      {label}
    </time>
  )
}
