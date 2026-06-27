import { createFileRoute, Navigate } from '@tanstack/react-router'

import { ChatPanel } from '@/components/chat/chat-panel'
import { useChat } from '@/providers/chat-provider'

export const Route = createFileRoute('/_app/chat/$chatId')({
  component: ChatPage
})

function ChatPage(): React.JSX.Element {
  const { chatId } = Route.useParams()
  const { getChat, sendMessage, isSending } = useChat()
  const chat = getChat(chatId)

  if (!chat) {
    return <Navigate to="/" replace />
  }

  return (
    <ChatPanel chat={chat} isSending={isSending} onSendMessage={(content) => sendMessage(chatId, content)} />
  )
}
