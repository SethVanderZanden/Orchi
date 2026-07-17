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
        'flex min-h-14 shrink-0 items-center justify-between gap-3 border-b px-5 py-3',
        className
      )}
    >
      <div className="flex min-w-0 flex-1 items-center gap-3">
        {startContent ?? (
          <div className="min-w-0 space-y-0.5">
            {title ? (
              <div className="truncate text-base font-medium tracking-tight">{title}</div>
            ) : null}
            {description ? (
              <div className="truncate text-sm text-muted-foreground">{description}</div>
            ) : null}
          </div>
        )}
      </div>
      {endContent ? <div className="flex shrink-0 items-center gap-1.5">{endContent}</div> : null}
    </header>
  )
}
