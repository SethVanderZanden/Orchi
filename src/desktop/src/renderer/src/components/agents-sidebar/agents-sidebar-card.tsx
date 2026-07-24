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
import type { AgentMode } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type AgentsSidebarCardProps = {
  title: string
  projectName: string | null
  parentTitle: string | null
  statusVariant: ChatStatusVariant
  mode: AgentMode
  isActive: boolean
  showProjectName: boolean
  onOpen: () => void
  onOpenBeside: () => void
}

export function AgentsSidebarCard({
  title,
  projectName,
  parentTitle,
  statusVariant,
  mode,
  isActive,
  showProjectName,
  onOpen,
  onOpenBeside
}: AgentsSidebarCardProps): React.JSX.Element {
  return (
    <ContextMenu>
      <ContextMenuTrigger asChild>
        <div className="group/sidebar-card relative min-w-0">
          <button
            type="button"
            onClick={onOpen}
            aria-current={isActive ? 'page' : undefined}
            className={cn(
              'flex w-full flex-col gap-0.5 rounded-md px-2.5 py-2 text-left',
              'transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
              isActive
                ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                : 'text-sidebar-foreground hover:bg-sidebar-accent/70 hover:text-sidebar-accent-foreground'
            )}
          >
            <div className="flex min-w-0 items-start gap-2 pr-7">
              <ChatStatusDot variant={statusVariant} mode={mode} className="mt-1" />
              <span className="min-w-0 flex-1 truncate text-sm font-medium">{title}</span>
            </div>
            {showProjectName ? (
              <span
                className={cn(
                  'truncate pl-5.5 text-xs',
                  isActive ? 'text-sidebar-accent-foreground/80' : 'text-sidebar-muted'
                )}
              >
                {projectName ?? 'No project'}
              </span>
            ) : null}
            {parentTitle ? (
              <span
                className={cn(
                  'truncate pl-5.5 text-xs',
                  isActive ? 'text-sidebar-accent-foreground/80' : 'text-sidebar-muted'
                )}
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
              'pointer-events-none absolute top-1 right-1 z-10 size-6 text-sidebar-muted opacity-0',
              'transition-opacity duration-150 ease-out',
              'group-hover/sidebar-card:pointer-events-auto group-hover/sidebar-card:opacity-100',
              'focus-visible:pointer-events-auto focus-visible:opacity-100',
              'hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
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
