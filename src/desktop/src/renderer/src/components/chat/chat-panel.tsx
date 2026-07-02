import { useNavigate } from '@tanstack/react-router'
import { VStack } from '@astryxdesign/core/Layout'
import { ChatLayout } from '@astryxdesign/core/Chat'
import { Button } from '@astryxdesign/core/Button'
import { Icon } from '@astryxdesign/core/Icon'
import { Toolbar } from '@astryxdesign/core/Toolbar'
import { Text } from '@astryxdesign/core/Text'
import { XMarkIcon } from '@heroicons/react/24/outline'
import type { CSSProperties } from 'react'

import { OrchiChatComposer } from '@/components/chat/chat-composer'
import { OrchiChatMessageList } from '@/components/chat/chat-message-list'
import type { ChatThread } from '@/lib/chat/types'
import { useChat } from '@/providers/chat-provider'

const chatShell: CSSProperties = {
  flex: 1,
  minHeight: 0,
  height: '100%',
  display: 'flex',
  flexDirection: 'column'
}

const chatLayout: CSSProperties = {
  flex: 1,
  minHeight: 0
}

type ChatPanelProps = {
  chat: ChatThread
}

export function ChatPanel({ chat }: ChatPanelProps): React.JSX.Element {
  const navigate = useNavigate()
  const { sendMessage, closeChat, isSending, getMarkers } = useChat()

  async function handleCloseChat(): Promise<void> {
    await closeChat(chat.id)
    navigate({ to: '/' })
  }

  return (
    <VStack style={chatShell}>
      <Toolbar
        label="Chat header"
        size="sm"
        dividers={['bottom']}
        startContent={
          <VStack gap={0}>
            <Text type="label" weight="semibold">
              {chat.title}
            </Text>
            <Text type="supporting" color="secondary">
              {chat.workspacePath} · {chat.messages.length} message
              {chat.messages.length === 1 ? '' : 's'}
            </Text>
          </VStack>
        }
        endContent={
          <Button
            label="Close chat"
            variant="ghost"
            size="sm"
            icon={<Icon icon={XMarkIcon} size="sm" />}
            isIconOnly
            onClick={() => void handleCloseChat()}
          />
        }
      />

      <ChatLayout
        density="spacious"
        style={chatLayout}
        composer={
          <OrchiChatComposer
            disabled={isSending}
            onSend={(content) => sendMessage(chat.id, content)}
          />
        }
      >
        <OrchiChatMessageList messages={chat.messages} markers={getMarkers(chat.id)} />
      </ChatLayout>
    </VStack>
  )
}
