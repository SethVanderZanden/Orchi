import type { ChatThread } from '@/lib/chat/types'
import { AppPageHeader } from '@/components/layout/app-page-header'
import { ChatComposer } from '@/components/chat/chat-composer'
import { ChatMessageList } from '@/components/chat/chat-message-list'

type ChatPanelProps = {
  chat: ChatThread
  isSending?: boolean
  onSendMessage: (content: string) => void
}

export function ChatPanel({
  chat,
  isSending = false,
  onSendMessage
}: ChatPanelProps): React.JSX.Element {
  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <AppPageHeader
        title={chat.title}
        description={
          chat.messages.length === 0
            ? 'No messages yet'
            : `${chat.messages.length} message${chat.messages.length === 1 ? '' : 's'}`
        }
      />

      <div className="flex min-h-0 flex-1 flex-col">
        <ChatMessageList messages={chat.messages} />
      </div>

      <div className="shrink-0 border-t bg-background/80 px-4 py-4 backdrop-blur-sm">
        <ChatComposer disabled={isSending} onSend={onSendMessage} />
      </div>
    </div>
  )
}
