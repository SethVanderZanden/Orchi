import { KanbanCard } from '@/components/kanban/kanban-card'
import { ScrollArea } from '@/components/ui/scroll-area'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { ChatThread } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

type KanbanColumnProps = {
  title: string
  chats: ChatThread[]
  getStatusVariant: (chat: ChatThread) => ChatStatusVariant
  getProjectName: (chat: ChatThread) => string | null
  getParentTitle: (chat: ChatThread) => string | null
  onOpenChat: (chatId: string) => void
  onOpenChatBeside: (chatId: string) => void
  className?: string
}

export function KanbanColumn({
  title,
  chats,
  getStatusVariant,
  getProjectName,
  getParentTitle,
  onOpenChat,
  onOpenChatBeside,
  className
}: KanbanColumnProps): React.JSX.Element {
  return (
    <section
      className={cn('flex min-h-0 min-w-0 flex-1 flex-col rounded-lg bg-muted/20', className)}
      aria-label={`${title}, ${chats.length} chats`}
    >
      <header className="flex shrink-0 items-center justify-between gap-2 px-3 py-2.5">
        <h2 className="text-sm font-semibold text-foreground">{title}</h2>
        <span className="font-mono text-xs text-muted-foreground tabular-nums">{chats.length}</span>
      </header>
      <ScrollArea className="min-h-0 flex-1">
        <div className="flex flex-col gap-1.5 px-1.5 pb-2">
          {chats.length === 0 ? (
            <p className="px-2 py-6 text-center text-xs text-muted-foreground">No chats</p>
          ) : (
            chats.map((chat) => (
              <KanbanCard
                key={chat.id}
                title={chat.title}
                projectName={getProjectName(chat)}
                parentTitle={getParentTitle(chat)}
                statusVariant={getStatusVariant(chat)}
                mode={chat.mode}
                onOpen={() => onOpenChat(chat.id)}
                onOpenBeside={() => onOpenChatBeside(chat.id)}
              />
            ))
          )}
        </div>
      </ScrollArea>
    </section>
  )
}
