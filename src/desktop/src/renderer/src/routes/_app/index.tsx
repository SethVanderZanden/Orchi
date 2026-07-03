import { createFileRoute, Navigate } from '@tanstack/react-router'

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
    <div className="flex h-full min-w-0 items-center justify-center px-6">
      <p className="max-w-md text-center text-sm text-muted-foreground">
        Expand a project and start a chat, or create one with the chat button beside a project.
      </p>
    </div>
  )
}
