import { createFileRoute, Navigate } from '@tanstack/react-router'

import { useChat } from '@/providers/chat-context'

export const Route = createFileRoute('/_app/')({
  component: AppIndexPage
})

function AppIndexPage(): React.JSX.Element {
  const { chats, isPendingChats } = useChat()
  const firstChatId = chats[0]?.id

  if (isPendingChats) {
    return (
      <div className="flex h-full min-w-0 items-center justify-center px-6">
        <p className="text-sm text-muted-foreground">Loading chats…</p>
      </div>
    )
  }

  if (firstChatId) {
    return <Navigate to="/chat/$chatId" params={{ chatId: firstChatId }} replace />
  }

  return (
    <div className="flex h-full min-w-0 items-center justify-center px-6">
      <p className="max-w-md text-center text-sm text-muted-foreground">
        Expand a project and use the new chat button on its row, or press Ctrl+N.
      </p>
    </div>
  )
}
