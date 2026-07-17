import { ChatWorkspacePanel } from '@/components/layout/chat-workspace-panel'
import { Button } from '@/components/ui/button'
import { useChatDetail } from '@/hooks/use-chat-detail'
import { isLocalChat } from '@/lib/chat/chat-persistence'
import { useChat } from '@/providers/chat-context'

type SplitChatPaneProps = {
  chatId: string
  onCloseSplit: () => void
}

export function SplitChatPane({ chatId, onCloseSplit }: SplitChatPaneProps): React.JSX.Element {
  const { isLoadingChats, getChat, isChatSending } = useChat()
  const chatQuery = useChatDetail(chatId)
  const cachedChat = getChat(chatId)
  const chat = cachedChat ?? chatQuery.data
  const isLocalDraft = isLocalChat(chatId)
  const isSending = isChatSending(chatId)

  if (chatQuery.isError && !chat) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3 border-l border-border px-4">
        <p className="text-sm text-muted-foreground">
          {chatQuery.error.message || 'Failed to load chat.'}
        </p>
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => void chatQuery.refetch()}
          >
            Retry
          </Button>
          <Button type="button" variant="ghost" size="sm" onClick={onCloseSplit}>
            Close split
          </Button>
        </div>
      </div>
    )
  }

  if (!chat && !isLocalDraft && (chatQuery.isPending || chatQuery.isFetching || isLoadingChats)) {
    return (
      <div className="flex h-full items-center justify-center border-l border-border">
        <p className="text-sm text-muted-foreground">Loading chat…</p>
      </div>
    )
  }

  if (!chat) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3 border-l border-border px-4">
        <p className="text-sm text-muted-foreground">Chat not found.</p>
        <Button type="button" variant="ghost" size="sm" onClick={onCloseSplit}>
          Close split
        </Button>
      </div>
    )
  }

  if (chatQuery.isFetching && chat.messages.length === 0 && !isLocalDraft && !isSending) {
    return (
      <div className="flex h-full items-center justify-center border-l border-border">
        <p className="text-sm text-muted-foreground">Loading chat…</p>
      </div>
    )
  }

  return <ChatWorkspacePanel key={chatId} chat={chat} />
}
