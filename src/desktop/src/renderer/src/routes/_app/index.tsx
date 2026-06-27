import { createFileRoute, Navigate } from '@tanstack/react-router'

import { useChat } from '@/providers/chat-provider'

export const Route = createFileRoute('/_app/')({
  component: AppIndexPage
})

function AppIndexPage(): React.JSX.Element {
  const { chats } = useChat()
  const firstChatId = chats[0]?.id

  if (!firstChatId) {
    return (
      <div className="text-muted-foreground flex flex-1 items-center justify-center text-sm">
        No chats yet. Create one from the sidebar.
      </div>
    )
  }

  return <Navigate to="/chat/$chatId" params={{ chatId: firstChatId }} replace />
}
