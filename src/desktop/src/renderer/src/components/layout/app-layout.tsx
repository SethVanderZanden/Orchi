import { Outlet, useMatch } from '@tanstack/react-router'

import { ChatSidebar } from '@/components/chat/chat-sidebar'
import { useChat } from '@/providers/chat-provider'
import { SidebarInset, SidebarProvider } from '@/components/ui/sidebar'
import { TooltipProvider } from '@/components/ui/tooltip'

export function AppLayout(): React.JSX.Element {
  const { chats, searchQuery, setSearchQuery, createChat } = useChat()

  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })

  const activeChatId = chatMatch?.params.chatId ?? null

  return (
    <TooltipProvider>
      <SidebarProvider defaultOpen className="h-svh min-h-0">
        <ChatSidebar
          chats={chats}
          activeChatId={activeChatId}
          searchQuery={searchQuery}
          onSearchQueryChange={setSearchQuery}
          onNewChat={createChat}
        />
        <SidebarInset className="min-h-0 overflow-hidden">
          <Outlet />
        </SidebarInset>
      </SidebarProvider>
    </TooltipProvider>
  )
}
