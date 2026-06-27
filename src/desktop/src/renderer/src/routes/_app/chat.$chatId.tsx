import { useEffect } from 'react'
import { createFileRoute, Navigate } from '@tanstack/react-router'

import { ChatPanel } from '@/components/chat/chat-panel'
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
      <div className="text-muted-foreground flex flex-1 items-center justify-center text-sm">
        Loading chat…
      </div>
    )
  }

  if (!chat) {
    return <Navigate to="/" replace />
  }

  return <ChatPanel chat={chat} />
}
