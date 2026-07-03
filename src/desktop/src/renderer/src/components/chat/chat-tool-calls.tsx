import { ChevronDown, ChevronRight } from 'lucide-react'

import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger
} from '@/components/ui/collapsible'
import { cn } from '@/lib/utils'

export type ToolCallItem = {
  key: string
  name: string
  target: string
  status: 'running' | 'complete'
}

type ChatToolCallsProps = {
  calls: ToolCallItem[]
  defaultOpen?: boolean
}

export function ChatToolCalls({
  calls,
  defaultOpen = false
}: ChatToolCallsProps): React.JSX.Element | null {
  if (calls.length === 0) {
    return null
  }

  return (
    <Collapsible defaultOpen={defaultOpen} className="mt-2 w-full max-w-2xl">
      <CollapsibleTrigger className="group flex items-center gap-1 rounded-md px-1 py-0.5 text-xs text-muted-foreground hover:text-foreground">
        <ChevronRight className="size-3 group-data-[state=open]:hidden" />
        <ChevronDown className="hidden size-3 group-data-[state=open]:block" />
        {calls.length} tool {calls.length === 1 ? 'call' : 'calls'}
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-1 space-y-1 rounded-md border bg-muted/40 p-2">
        {calls.map((call) => (
          <div key={call.key} className="flex items-start gap-2 text-xs">
            <span
              className={cn(
                'mt-1 size-1.5 shrink-0 rounded-full',
                call.status === 'running' ? 'animate-pulse bg-amber-500' : 'bg-emerald-500'
              )}
            />
            <div className="min-w-0">
              <span className="font-medium">{call.name}</span>
              <span className="text-muted-foreground"> · {call.target}</span>
            </div>
          </div>
        ))}
      </CollapsibleContent>
    </Collapsible>
  )
}
