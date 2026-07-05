import type { ReactNode } from 'react'

import { cn } from '@/lib/utils'

type ChatLayoutProps = {
  children: ReactNode
  composer: ReactNode
  projectContext?: ReactNode
  variant?: 'default' | 'centered'
  className?: string
}

export function ChatLayout({
  children,
  composer,
  projectContext,
  variant = 'default',
  className
}: ChatLayoutProps): React.JSX.Element {
  if (variant === 'centered') {
    return (
      <div className={cn('flex min-h-0 flex-1 flex-col overflow-hidden bg-background', className)}>
        <div className="flex min-h-0 flex-1 flex-col items-center justify-center px-4">
          <div className="w-full max-w-3xl space-y-2">
            {projectContext}
            {composer}
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className={cn('flex min-h-0 flex-1 flex-col overflow-hidden bg-background', className)}>
      <div className="flex min-h-0 flex-1 flex-col bg-background">{children}</div>
      <div className="shrink-0 bg-background px-4 py-5">
        <div className="mx-auto w-full max-w-3xl">{composer}</div>
      </div>
    </div>
  )
}
