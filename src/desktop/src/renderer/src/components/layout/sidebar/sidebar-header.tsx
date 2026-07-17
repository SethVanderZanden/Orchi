import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type SidebarHeaderProps = {
  title: ReactNode
  subtitle?: ReactNode
  icon?: ReactNode
  endContent?: ReactNode
  className?: string
}

export function SidebarHeader({
  title,
  subtitle,
  icon,
  endContent,
  className
}: SidebarHeaderProps): React.JSX.Element {
  return (
    <header
      className={cn('flex shrink-0 items-center justify-between gap-3 px-4 pb-4 pt-5', className)}
    >
      <div className="flex min-w-0 flex-1 items-center gap-2.5">
        {icon}
        <div className="min-w-0">
          <p className="truncate text-[15px] font-semibold tracking-tight text-sidebar-foreground">
            {title}
          </p>
          {subtitle ? <p className="truncate text-xs text-sidebar-muted">{subtitle}</p> : null}
        </div>
      </div>
      {endContent ? <div className="flex shrink-0 items-center gap-0.5">{endContent}</div> : null}
    </header>
  )
}
