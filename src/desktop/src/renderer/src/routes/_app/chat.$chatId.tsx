import { useEffect } from 'react'
import { createFileRoute, Navigate } from '@tanstack/react-router'

import { ChatWorkspacePanel } from '@/components/layout/chat-workspace-panel'
import { useChat } from '@/providers/chat-provider'

export const Route = createFileRoute('/_app/chat/$chatId')({
  component: ChatPage
})

function ChatPage(): React.JSX.Element {
  const { chatId } = Route.useParams()
  const { getChat, loadChat, isLoadingChats } = useChat()
  const chat = getChat(chatId)

  useEffect(() => {
    void loadChat(chatId)
  }, [chatId, loadChat])

  if (!chat && isLoadingChats) {
    return (
      <div className="flex h-full items-center justify-center">
        <p className="text-sm text-muted-foreground">Loading chat…</p>
      </div>
    )
  }

  if (!chat) {
    return <Navigate to="/" replace />
  }

  return <ChatWorkspacePanel chat={chat} />
}
