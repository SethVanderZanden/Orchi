import { useEffect } from 'react'
import { createFileRoute, Navigate } from '@tanstack/react-router'
import { Text } from '@astryxdesign/core/Text'
import { VStack } from '@astryxdesign/core/Layout'

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
      <VStack height="100%" hAlign="center" vAlign="center">
        <Text type="supporting" color="secondary">
          Loading chat…
        </Text>
      </VStack>
    )
  }

  if (!chat) {
    return <Navigate to="/" replace />
  }

  return <ChatPanel chat={chat} />
}
