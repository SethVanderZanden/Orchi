import { createFileRoute, Navigate } from '@tanstack/react-router'

import { ChatWorkspacePanel } from '@/components/layout/chat-workspace-panel'
import { Button } from '@/components/ui/button'
import { useChatDetail } from '@/hooks/use-chat-detail'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-context'

export const Route = createFileRoute('/_app/chat/$chatId')({
  component: ChatPage
})

function ChatPage(): React.JSX.Element {
  const { chatId } = Route.useParams()
  const { isLoadingChats, getChat, isChatSending } = useChat()
  const chatQuery = useChatDetail(chatId)
  const cachedChat = getChat(chatId)
  const chat = cachedChat ?? chatQuery.data
  const isLocalDraft = isLocalChat(chatId)
  const isSending = isChatSending(chatId)

  if (chatQuery.isError && !chat) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3">
        <p className="text-sm text-muted-foreground">
          {chatQuery.error.message || 'Failed to load chat.'}
        </p>
        <Button type="button" variant="outline" size="sm" onClick={() => void chatQuery.refetch()}>
          Retry
        </Button>
      </div>
    )
  }

  if (!chat && !isLocalDraft && (chatQuery.isPending || chatQuery.isFetching || isLoadingChats)) {
    return (
      <div className="flex h-full items-center justify-center">
        <p className="text-sm text-muted-foreground">Loading chat…</p>
      </div>
    )
  }

  if (!chat) {
    return <Navigate to="/" replace />
  }

  if (chatQuery.isFetching && chat.messages.length === 0 && !isLocalDraft && !isSending) {
    return (
      <div className="flex h-full items-center justify-center">
        <p className="text-sm text-muted-foreground">Loading chat…</p>
      </div>
    )
  }

  return <ChatWorkspacePanel key={chatId} chat={chat} />
}
