import { AgentsSidebarCard } from '@/components/agents-sidebar/agents-sidebar-card'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { ChatThread } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type AgentsSidebarSectionProps = {
  title: string
  chats: ChatThread[]
  showProjectName: boolean
  getStatusVariant: (chat: ChatThread) => ChatStatusVariant
  getProjectName: (chat: ChatThread) => string | null
  getParentTitle: (chat: ChatThread) => string | null
  isChatActive: (chatId: string) => boolean
  onOpenChat: (chatId: string) => void
  onOpenChatBeside: (chatId: string) => void
  nested?: boolean
  className?: string
}

export function AgentsSidebarSection({
  title,
  chats,
  showProjectName,
  getStatusVariant,
  getProjectName,
  getParentTitle,
  isChatActive,
  onOpenChat,
  onOpenChatBeside,
  nested = false,
  className
}: AgentsSidebarSectionProps): React.JSX.Element {
  return (
    <section
      className={cn('min-w-0', className)}
      aria-label={`${title}, ${chats.length} chats`}
    >
      <header
        className={cn(
          'flex items-center justify-between gap-2',
          nested ? 'px-2.5 pb-1 pt-2' : 'px-3 pb-1.5 pt-3'
        )}
      >
        <h2
          className={cn(
            'font-semibold tracking-wide text-sidebar-muted uppercase',
            nested ? 'text-[10px]' : 'text-[11px]'
          )}
        >
          {title}
        </h2>
        <span className="font-mono text-[11px] text-sidebar-muted tabular-nums">{chats.length}</span>
      </header>
      <div className={cn('flex flex-col gap-0.5', nested ? 'px-1' : 'px-1.5')}>
        {chats.length === 0 ? (
          <p className="px-2 py-2 text-center text-xs text-sidebar-muted">No chats</p>
        ) : (
          chats.map((chat) => (
            <AgentsSidebarCard
              key={chat.id}
              title={chat.title}
              projectName={getProjectName(chat)}
              parentTitle={getParentTitle(chat)}
              statusVariant={getStatusVariant(chat)}
              mode={chat.mode}
              isActive={isChatActive(chat.id)}
              showProjectName={showProjectName}
              onOpen={() => onOpenChat(chat.id)}
              onOpenBeside={() => onOpenChatBeside(chat.id)}
            />
          ))
        )}
      </div>
    </section>
  )
}
