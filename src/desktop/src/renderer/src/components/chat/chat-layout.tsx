import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type ChatLayoutProps = {
  children: ReactNode
  composer: ReactNode
  footer?: ReactNode
  className?: string
}

export function ChatLayout({
  children,
  composer,
  footer,
  className
}: ChatLayoutProps): React.JSX.Element {
  return (
    <div className={cn('flex min-h-0 flex-1 flex-col overflow-hidden bg-background', className)}>
      <div className="min-h-0 flex-1 overflow-y-auto bg-background">{children}</div>
      <div className="shrink-0 border-t bg-background px-4 py-5">
        {composer}
        {footer ? <div className="pt-2">{footer}</div> : null}
      </div>
    </div>
  )
}
