import { ChevronDown, ChevronRight } from 'lucide-react'

import { AgentsSidebarSection } from '@/components/agents-sidebar/agents-sidebar-section'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger
} from '@/components/ui/collapsible'
import type { BoardProjectGroup } from '@/lib/agents-sidebar/group-board-chats'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { ChatThread } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type AgentsSidebarProjectGroupProps = {
  group: BoardProjectGroup
  isExpanded: boolean
  onToggle: () => void
  getStatusVariant: (chat: ChatThread) => ChatStatusVariant
  getParentTitle: (chat: ChatThread) => string | null
  isChatActive: (chatId: string) => boolean
  onOpenChat: (chatId: string) => void
  onOpenChatBeside: (chatId: string) => void
}

export function AgentsSidebarProjectGroup({
  group,
  isExpanded,
  onToggle,
  getStatusVariant,
  getParentTitle,
  isChatActive,
  onOpenChat,
  onOpenChatBeside
}: AgentsSidebarProjectGroupProps): React.JSX.Element {
  return (
    <Collapsible open={isExpanded} onOpenChange={onToggle}>
      <CollapsibleTrigger asChild>
        <button
          type="button"
          className={cn(
            'flex w-full items-center gap-1.5 rounded-md px-2.5 py-2 text-left text-sm',
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
                nested
              />
            ))}
        </div>
      </CollapsibleContent>
    </Collapsible>
  )
}
