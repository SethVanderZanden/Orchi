import { Columns2 } from 'lucide-react'

import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { Button } from '@/components/ui/button'
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger
} from '@/components/ui/context-menu'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import { cn } from '@/lib/utils'

type KanbanCardProps = {
  title: string
  projectName: string | null
  parentTitle: string | null
  statusVariant: ChatStatusVariant
  onOpen: () => void
  onOpenBeside: () => void
}

export function KanbanCard({
  title,
  projectName,
  parentTitle,
  statusVariant,
  onOpen,
  onOpenBeside
}: KanbanCardProps): React.JSX.Element {
  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>
        <div className="group/kanban-card relative min-w-0">
          <button
            type="button"
            onClick={onOpen}
            className={cn(
              'flex w-full flex-col gap-1 rounded-md bg-transparent px-3 py-2.5 text-left',
              'transition-colors hover:bg-accent/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'
            )}
          >
            <div className="flex min-w-0 items-start gap-2 pr-7">
              <ChatStatusDot variant={statusVariant} className="mt-1.5" />
              <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground">
                {title}
              </span>
            </div>
            <span className="truncate pl-3.5 text-xs text-muted-foreground">
              {projectName ?? 'No project'}
            </span>
            {parentTitle ? (
              <span
                className="truncate pl-3.5 text-xs text-muted-foreground"
                title={`Parent: ${parentTitle}`}
              >
                Parent: {parentTitle}
              </span>
            ) : null}
          </button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn(
              'pointer-events-none absolute top-1.5 right-1 z-10 size-6 text-muted-foreground opacity-0',
              'transition-opacity duration-150 ease-out',
              'group-hover/kanban-card:pointer-events-auto group-hover/kanban-card:opacity-100',
              'focus-visible:pointer-events-auto focus-visible:opacity-100'
            )}
            aria-label={`Open ${title} beside`}
            title="Open beside"
            onClick={(event) => {
              event.stopPropagation()
              onOpenBeside()
            }}
          >
            <Columns2 className="size-3.5" />
          </Button>
        </div>
      </ContextMenuTrigger>
      <ContextMenuContent className="min-w-44">
        <ContextMenuItem onSelect={onOpen}>Open</ContextMenuItem>
        <ContextMenuItem onSelect={onOpenBeside}>Open beside</ContextMenuItem>
      </ContextMenuContent>
    </ContextMenu>
  )
}
