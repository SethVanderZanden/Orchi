import { ChevronDown, ChevronRight, Trash2 } from 'lucide-react'

import { AgentsSidebarSection } from '@/components/agents-sidebar/agents-sidebar-section'
import { Button } from '@/components/ui/button'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import type { BoardProjectGroup } from '@/lib/agents-sidebar/group-board-chats'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { ChatThread } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type AgentsSidebarProjectGroupProps = {
  group: BoardProjectGroup
  isExpanded: boolean
  onExpandedChange: (expanded: boolean) => void
  getStatusVariant: (chat: ChatThread) => ChatStatusVariant
  getParentTitle: (chat: ChatThread) => string | null
  isChatActive: (chatId: string) => boolean
  onOpenChat: (chatId: string) => void
  onOpenChatBeside: (chatId: string) => void
  onDeleteChat: (chat: ChatThread) => void
  onClearChats: (chats: ChatThread[], scopeLabel: string) => void
  isDeleteDisabled: (chatId: string) => boolean
  isClearDisabled: boolean
}

function collectGroupChats(group: BoardProjectGroup): ChatThread[] {
  return group.sections.flatMap((section) => section.chats)
}

export function AgentsSidebarProjectGroup({
  group,
  isExpanded,
  onExpandedChange,
  getStatusVariant,
  getParentTitle,
  isChatActive,
  onOpenChat,
  onOpenChatBeside,
  onDeleteChat,
  onClearChats,
  isDeleteDisabled,
  isClearDisabled
}: AgentsSidebarProjectGroupProps): React.JSX.Element {
  const chats = collectGroupChats(group)

  return (
    <Collapsible open={isExpanded} onOpenChange={onExpandedChange}>
      <div className="group/project-header relative flex items-center gap-1">
        <CollapsibleTrigger asChild>
          <button
            type="button"
            className={cn(
              'flex min-w-0 flex-1 items-center gap-1.5 rounded-md px-2.5 py-2 text-left text-sm',
              'text-sidebar-foreground transition-colors hover:bg-sidebar-accent/70 hover:text-sidebar-accent-foreground',
              'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring'
            )}
            aria-expanded={isExpanded}
          >
            {isExpanded ? (
              <ChevronDown className="size-3.5 shrink-0 text-sidebar-muted" aria-hidden />
            ) : (
              <ChevronRight className="size-3.5 shrink-0 text-sidebar-muted" aria-hidden />
            )}
            <span className="min-w-0 flex-1 truncate font-medium">{group.name}</span>
            <span className="font-mono text-[11px] text-sidebar-muted tabular-nums">
              {group.chatCount}
            </span>
          </button>
        </CollapsibleTrigger>
        {group.chatCount > 0 ? (
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn(
              'mr-1 size-6 shrink-0 text-sidebar-muted opacity-0',
              'transition-opacity duration-150 ease-out',
              'hover:bg-sidebar-accent hover:text-destructive',
              'focus-visible:opacity-100 focus-visible:text-destructive',
              'group-hover/project-header:opacity-100'
            )}
            aria-label={`Clear chats in ${group.name}`}
            title="Clear chats"
            disabled={isClearDisabled}
            onClick={(event) => {
              event.stopPropagation()
              onClearChats(chats, group.name)
            }}
          >
            <Trash2 className="size-3.5" />
          </Button>
        ) : null}
      </div>
      <CollapsibleContent>
        <div className="mb-2 ml-2 border-l border-border/60 pl-1">
          {group.sections
            .filter((section) => section.chats.length > 0)
            .map((section) => (
              <AgentsSidebarSection
                key={section.id}
                title={section.title}
                chats={section.chats}
                showProjectName={false}
                getStatusVariant={getStatusVariant}
                getProjectName={() => null}
                getParentTitle={getParentTitle}
                isChatActive={isChatActive}
                onOpenChat={onOpenChat}
                onOpenChatBeside={onOpenChatBeside}
                onDeleteChat={onDeleteChat}
                isDeleteDisabled={isDeleteDisabled}
                nested
              />
            ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}
