import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type PageHeaderProps = {
  title?: ReactNode
  description?: ReactNode
  startContent?: ReactNode
  endContent?: ReactNode
  className?: string
}

export function PageHeader({
  title,
  description,
  startContent,
  endContent,
  className
}: PageHeaderProps): React.JSX.Element {
  return (
    <header
      className={cn(
        'flex min-h-12 shrink-0 items-center justify-between gap-3 border-b px-4 py-2',
        className
      )}
    >
      <div className="flex min-w-0 flex-1 items-center gap-3">
        {startContent ?? (
          <div className="min-w-0">
            {title ? <div className="truncate text-sm font-semibold">{title}</div> : null}
            {description ? (
              <div className="truncate text-xs text-muted-foreground">{description}</div>
            ) : null}
          </div>
        )}
      </div>
      {endContent ? <div className="flex shrink-0 items-center gap-1">{endContent}</div> : null}
    </header>
  )
}
