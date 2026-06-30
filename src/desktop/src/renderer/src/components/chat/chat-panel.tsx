import { useNavigate } from '@tanstack/react-router'
import { XIcon } from 'lucide-react'

import type { ChatThread } from '@/lib/chat/types'
import { formatChatMode } from '@/lib/chat/types'
import { useChat } from '@/providers/chat-provider'
import { AppPageHeader } from '@/components/layout/app-page-header'
import { ChatComposer } from '@/components/chat/chat-composer'
import { ChatMessageList } from '@/components/chat/chat-message-list'
import { ChatModeSelect } from '@/components/chat/chat-mode-select'
import { Button } from '@/components/ui/button'

type ChatPanelProps = {
  chat: ChatThread
}

export function ChatPanel({ chat }: ChatPanelProps): React.JSX.Element {
  const navigate = useNavigate()
  const { sendMessage, closeChat, updateChatMode, isSending, isUpdatingMode, getMarkers } = useChat()

  async function handleCloseChat(): Promise<void> {
    await closeChat(chat.id)
    navigate({ to: '/' })
  }

  return (
    <div className="flex h-full min-h-0 flex-1 flex-col">
      <AppPageHeader
        title={chat.title}
        description={`${chat.workspacePath} · ${formatChatMode(chat.mode)} · ${chat.messages.length} message${chat.messages.length === 1 ? '' : 's'}`}
      >
        <ChatModeSelect
          value={chat.mode}
          disabled={isSending || isUpdatingMode}
          onChange={(mode, attachedPlanId) => updateChatMode(chat.id, mode, attachedPlanId)}
          className="w-[140px]"
        />
        <Button size="icon-sm" variant="ghost" onClick={handleCloseChat} aria-label="Close chat">
          <XIcon />
        </Button>
      </AppPageHeader>

      <div className="flex min-h-0 flex-1 flex-col">
        <ChatMessageList messages={chat.messages} markers={getMarkers(chat.id)} />
      </div>

      <div className="shrink-0 border-t bg-background/80 px-4 py-4 backdrop-blur-sm">
        <ChatComposer
          disabled={isSending}
          onSend={(content) => sendMessage(chat.id, content)}
        />
      </div>
    </div>
  )
}
