import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type ChatLayoutProps = {
  children: ReactNode
  composer: ReactNode
  className?: string
}

export function ChatLayout({ children, composer, className }: ChatLayoutProps): React.JSX.Element {
  return (
    <div className={cn('flex min-h-0 flex-1 flex-col overflow-hidden', className)}>
      <div className="min-h-0 flex-1 overflow-y-auto">{children}</div>
      <div className="shrink-0 border-t bg-background p-4">{composer}</div>
    </div>
  )
}
