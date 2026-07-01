import { LoaderCircleIcon, PanelRightIcon } from 'lucide-react'

import { extractPlanTitle } from '@/lib/chat/plan-preview'
import type { ChatMessageStatus } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type PlanMessageTeaserProps = {
  content: string
  status: ChatMessageStatus
  onOpenPlan: () => void
  className?: string
}

export function PlanMessageTeaser({
  content,
  status,
  onOpenPlan,
  className
}: PlanMessageTeaserProps): React.JSX.Element {
  const isActive = status === 'processing' || status === 'streaming'
  const title = isActive ? 'Generating plan…' : extractPlanTitle(content)

  return (
    <button
      type="button"
      onClick={onOpenPlan}
      className={cn(
        'hover:bg-muted/60 flex w-full items-center gap-3 rounded-lg border border-border/60 bg-muted/30 px-3 py-2.5 text-left transition-colors',
        className
      )}
    >
      <PanelRightIcon className="text-muted-foreground size-4 shrink-0" />
      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-medium">{title}</p>
        <p className="text-muted-foreground text-xs">
          {isActive ? 'Streaming in plan panel' : 'View full plan in panel'}
        </p>
      </div>
      {isActive ? <LoaderCircleIcon className="text-muted-foreground size-4 shrink-0 animate-spin" /> : null}
    </button>
  )
}
