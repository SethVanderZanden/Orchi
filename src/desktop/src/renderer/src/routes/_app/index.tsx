import { createFileRoute, Navigate } from '@tanstack/react-router'
import { Text } from '@astryxdesign/core/Text'
import { VStack } from '@astryxdesign/core/Layout'

import { useChat } from '@/providers/chat-provider'

export const Route = createFileRoute('/_app/')({
  component: AppIndexPage
})

function AppIndexPage(): React.JSX.Element {
  const { chats } = useChat()
  const firstChatId = chats[0]?.id

  if (!firstChatId) {
    return (
      <VStack height="100%" hAlign="center" vAlign="center">
        <Text type="supporting" color="secondary">
          No chats yet. Create one from the navigator.
        </Text>
      </VStack>
    )
  }

  return <Navigate to="/chat/$chatId" params={{ chatId: firstChatId }} replace />
}
