import { createFileRoute, Navigate } from '@tanstack/react-router'

import { ChatWorkspacePanel } from '@/components/layout/chat-workspace-panel'
import { useChatDetail } from '@/hooks/use-chat-detail'
import { useChat } from '@/providers/chat-provider'

export const Route = createFileRoute('/_app/chat/$chatId')({
  component: ChatPage
})

function ChatPage(): React.JSX.Element {
  const { chatId } = Route.useParams()
  const { isLoadingChats } = useChat()
  const chatQuery = useChatDetail(chatId)
  const chat = chatQuery.data

  if (!chat && (chatQuery.isPending || isLoadingChats)) {
    return (
      <div className="flex h-full items-center justify-center">
        <p className="text-sm text-muted-foreground">Loading chat…</p>
      </div>
    )
  }

  if (!chat) {
    return <Navigate to="/" replace />
  }

  return <ChatWorkspacePanel key={chatId} chat={chat} />
}
