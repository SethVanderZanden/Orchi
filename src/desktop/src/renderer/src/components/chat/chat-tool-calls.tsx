import { ChevronDown, ChevronRight } from 'lucide-react'

import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import { cn } from '@/lib/utils'

export type ToolCallItem = {
  key: string
  name: string
  target: string
  status: 'running' | 'complete'
}

type ChatToolCallsProps = {
  calls: ToolCallItem[]
}

export function ChatToolCalls({ calls }: ChatToolCallsProps): React.JSX.Element | null {
  if (calls.length === 0) {
    return null
  }

  const hasRunning = calls.some((call) => call.status === 'running')

  return (
    <Collapsible className="mt-1 w-full max-w-2xl">
      <CollapsibleTrigger className="group inline-flex items-center gap-1 rounded-full border border-transparent px-2 py-0.5 text-[11px] text-muted-foreground transition-colors hover:border-border hover:bg-muted/50 hover:text-foreground">
        {hasRunning ? (
          <span className="size-1.5 shrink-0 animate-pulse rounded-full bg-amber-500" />
        ) : (
          <ChevronRight className="size-3 group-data-[state=open]:hidden" />
        )}
        <ChevronDown className="hidden size-3 group-data-[state=open]:block" />
        {calls.length} tool {calls.length === 1 ? 'call' : 'calls'}
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-1 space-y-0.5 pl-2">
        {calls.map((call) => (
          <div
            key={call.key}
            className="flex items-start gap-1.5 text-[11px] text-muted-foreground"
          >
            <span
              className={cn(
                'mt-1 size-1 shrink-0 rounded-full',
                call.status === 'running' ? 'animate-pulse bg-amber-500' : 'bg-emerald-500'
              )}
            />
            <div className="min-w-0 truncate">
              <span className="text-foreground/80">{call.name}</span>
              <span> · {call.target}</span>
            </div>
          </div>
        ))}
      </CollapsibleContent>
    </Collapsible>
  )
}
