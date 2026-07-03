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

  if (firstChatId) {
    return <Navigate to="/chat/$chatId" params={{ chatId: firstChatId }} replace />
  }

  return (
    <VStack height="100%" hAlign="center" vAlign="center" className="min-w-0 px-6">
      <Text type="supporting" color="secondary">
        Expand a project and start a chat, or create one with the chat button beside a project.
      </Text>
    </VStack>
  )
}
