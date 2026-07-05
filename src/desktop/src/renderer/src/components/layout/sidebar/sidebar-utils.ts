import type { ChatSidebarStatusVariant } from '@/lib/chat/chat-sidebar-status'
import type { ChatThread } from '@/lib/chat/types'
import { cn } from '@/lib/utils'

export function sidebarIconClass(isActive: boolean): string {
  return cn(
    'shrink-0 transition-colors duration-150 ease-out',
    isActive
      ? 'text-sidebar-accent-foreground'
      : 'text-sidebar-muted group-hover:text-sidebar-accent-foreground'
  )
}

export type SidebarChatActions = {
  activeChatId?: string | null
  onSelectChat: (chatId: string) => void
  onRequestDelete: (chat: ChatThread) => void
  isDeletingChat: (chatId: string) => boolean
  getChatSidebarStatus: (chat: ChatThread) => ChatSidebarStatusVariant
  isChatSending: (chatId: string) => boolean
}
