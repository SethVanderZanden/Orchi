import { useEffect, useRef } from 'react'
import type { LucideIcon } from 'lucide-react'
import { FilePenLine, FileText, Globe, List, Search, Terminal, Wrench } from 'lucide-react'

import { Marker, MarkerContent, MarkerIcon } from '@/components/ui/marker'
import { ScrollArea } from '@/components/ui/scroll-area'
import { cn } from '@/lib/utils'

export type ToolCallItem = {
  key: string
  label: string
  status: 'running' | 'complete'
}

type ChatToolCallsProps = {
  calls: ToolCallItem[]
}

const MAX_HEIGHT_PX = 96
const ROW_HEIGHT_PX = 22

const TOOL_ICON_RULES: Array<{ match: RegExp; icon: LucideIcon }> = [
  { match: /^reading\b/i, icon: FileText },
  { match: /^(writing|applying)\b/i, icon: FilePenLine },
  { match: /^(searching|grep)\b/i, icon: Search },
  { match: /^listing\b/i, icon: List },
  { match: /^running\b/i, icon: Terminal },
  { match: /^(mcp|web)\b/i, icon: Globe }
]

function iconForLabel(label: string): LucideIcon {
  for (const rule of TOOL_ICON_RULES) {
    if (rule.match.test(label)) {
      return rule.icon
    }
  }

  return Wrench
}

export function ChatToolCalls({ calls }: ChatToolCallsProps): React.JSX.Element | null {
  const rootRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const viewport = rootRef.current?.querySelector<HTMLElement>(
      '[data-radix-scroll-area-viewport]'
    )
    if (!viewport) {
      return
    }

    viewport.scrollTop = viewport.scrollHeight
  }, [calls])

  if (calls.length === 0) {
    return null
  }

  const height = Math.min(calls.length * ROW_HEIGHT_PX, MAX_HEIGHT_PX)

  return (
    <ScrollArea ref={rootRef} className="mt-1.5 w-full max-w-2xl" style={{ height }}>
      <div className="flex flex-col gap-0.5 pr-2" role="status" aria-live="polite">
        {calls.map((call) => {
          const Icon = iconForLabel(call.label)

          return (
            <Marker key={call.key} className="min-h-0 gap-1.5 py-0.5 text-xs">
              <MarkerIcon
                className={cn(
                  'size-3.5 text-muted-foreground/70 [&_svg:not([class*="size-"])]:size-3.5',
                  call.status === 'running' && 'animate-pulse text-amber-500/80'
                )}
              >
                <Icon />
              </MarkerIcon>
              <MarkerContent className="truncate text-muted-foreground/80">
                {call.label}
              </MarkerContent>
            </Marker>
          )
        })}
      </div>
    </ScrollArea>
  )
}
