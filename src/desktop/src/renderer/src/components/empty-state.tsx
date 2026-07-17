import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type EmptyStateProps = {
  title: string
  description?: ReactNode
  icon?: ReactNode
  children?: ReactNode
  className?: string
}

export function EmptyState({
  title,
  description,
  icon,
  children,
  className
}: EmptyStateProps): React.JSX.Element {
  return (
    <div
      className={cn(
        'flex h-full flex-col items-center justify-center gap-3 px-6 py-12 text-center',
        className
      )}
    >
      {icon ? <div className="text-muted-foreground">{icon}</div> : null}
      <div className="space-y-1">
        <p className="text-sm font-medium">{title}</p>
        {description ? (
          <div className="max-w-sm text-sm text-muted-foreground">{description}</div>
        ) : null}
      </div>
      {children}
    </div>
  )
}
