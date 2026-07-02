import { Avatar } from '@astryxdesign/core/Avatar'
import {
  ChatMessage,
  ChatMessageBubble,
  ChatMessageList,
  ChatToolCalls
} from '@astryxdesign/core/Chat'
import { Markdown } from '@astryxdesign/core/Markdown'
import { Text } from '@astryxdesign/core/Text'
import { EmptyState } from '@astryxdesign/core/EmptyState'
import { Icon } from '@astryxdesign/core/Icon'
import { ChatBubbleLeftEllipsisIcon } from '@heroicons/react/24/outline'

import type { ChatMarker, ChatMessage as OrchiChatMessage } from '@/lib/chat/types'

type ChatMessageListProps = {
  messages: OrchiChatMessage[]
  markers: ChatMarker[]
}

export function OrchiChatMessageList({
  messages,
  markers
}: ChatMessageListProps): React.JSX.Element {
  if (messages.length === 0 && markers.length === 0) {
    return (
      <EmptyState
        title="Start a conversation"
        description="Ask Orchi to help with coding tasks in your workspace."
        icon={<Icon icon={ChatBubbleLeftEllipsisIcon} size="md" />}
      />
    )
  }

  const lastAssistantIndex = messages.findLastIndex((message) => message.role === 'assistant')
  const lastAssistant = lastAssistantIndex >= 0 ? messages[lastAssistantIndex] : null
  const isActiveTurn =
    lastAssistant?.status === 'processing' || lastAssistant?.status === 'streaming'
  const activeMarkers = isActiveTurn ? markers : []

  return (
    <ChatMessageList>
      {messages.map((message, index) => (
        <ChatMessageRow
          key={message.id}
          message={message}
          markers={index === lastAssistantIndex ? activeMarkers : []}
        />
      ))}
    </ChatMessageList>
  )
}

type ChatMessageRowProps = {
  message: OrchiChatMessage
  markers: ChatMarker[]
}

function ChatMessageRow({ message, markers }: ChatMessageRowProps): React.JSX.Element {
  const isUser = message.role === 'user'
  const showPlaceholder =
    !isUser && message.status === 'processing' && message.content.length === 0
  const showActivity = !isUser && markers.length > 0

  if (isUser) {
    return (
      <ChatMessage sender="user">
        <ChatMessageBubble>
          <Text type="body">{message.content}</Text>
        </ChatMessageBubble>
      </ChatMessage>
    )
  }

  const toolCalls = markers
    .filter((marker) => marker.variant === 'tool')
    .map((marker) => ({
      key: marker.id,
      name: 'tool',
      target: marker.content,
      status: markers.some((item) => item.variant === 'status')
        ? ('running' as const)
        : ('complete' as const)
    }))

  return (
    <ChatMessage sender="assistant" avatar={<Avatar name="Orchi" size="small" />}>
      <ChatMessageBubble variant={message.status === 'error' ? 'filled' : 'ghost'}>
        {showPlaceholder ? (
          <Text type="body" color="secondary">
            …
          </Text>
        ) : message.status === 'processing' || message.status === 'streaming' ? (
          <Text type="body">{message.content}</Text>
        ) : (
          <Markdown>{message.content}</Markdown>
        )}
      </ChatMessageBubble>
      {showActivity ? (
        toolCalls.length > 0 ? (
          <ChatToolCalls calls={toolCalls} defaultIsExpanded />
        ) : (
          <Text type="supporting" color="secondary">
            Working…
          </Text>
        )
      ) : null}
    </ChatMessage>
  )
}
