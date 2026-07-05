import { MessageSquare } from 'lucide-react'

import { AgentModeAvatar } from '@/components/chat/agent-mode-avatar'
import { Bubble, BubbleContent } from '@/components/ui/bubble'
import { Marker, MarkerContent } from '@/components/ui/marker'
import {
  Message,
  MessageAvatar,
  MessageContent
} from '@/components/ui/message'
import { MessageScrollerItem } from '@/components/ui/message-scroller'
import { ChatToolCalls } from '@/components/chat/chat-tool-calls'
import { EmptyState } from '@/components/empty-state'
import { MarkdownContent } from '@/components/markdown-content'
import type { AgentMode, ChatMarker, ChatMessage as OrchiChatMessage } from '@/lib/chat/types'

type ChatMessageListProps = {
  messages: OrchiChatMessage[]
  markers: ChatMarker[]
  mode: AgentMode
  hideEmptyState?: boolean
}

export function OrchiChatMessageList({
  messages,
  markers,
  mode,
  hideEmptyState = false
}: ChatMessageListProps): React.JSX.Element {
  if (messages.length === 0 && markers.length === 0) {
    if (hideEmptyState) {
      return <div className="min-h-0" />
    }

    return (
      <EmptyState
        title="Start a conversation"
        description="Ask Orchi to help with coding tasks in your workspace."
        icon={<MessageSquare className="size-8" />}
      />
    )
  }

  const lastAssistantIndex = messages.findLastIndex((message) => message.role === 'assistant')
  const lastAssistant = lastAssistantIndex >= 0 ? messages[lastAssistantIndex] : null
  const isActiveTurn =
    lastAssistant?.status === 'processing' || lastAssistant?.status === 'streaming'
  const activeMarkers = isActiveTurn ? markers : []

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-6">
      {messages.map((message, index) => (
        <MessageScrollerItem
          key={message.id}
          scrollAnchor={message.role === 'user'}
        >
          <ChatMessageRow
            message={message}
            markers={index === lastAssistantIndex ? activeMarkers : []}
            mode={mode}
          />
        </MessageScrollerItem>
      ))}
    </div>
  )
}

type ChatMessageRowProps = {
  message: OrchiChatMessage
  markers: ChatMarker[]
  mode: AgentMode
}

function ChatMessageRow({ message, markers, mode }: ChatMessageRowProps): React.JSX.Element {
  const isUser = message.role === 'user'
  const showPlaceholder = !isUser && message.status === 'processing' && message.content.length === 0
  const showActivity = !isUser && markers.length > 0

  if (isUser) {
    return (
      <Message align="end">
        <MessageContent>
          <Bubble>
            <BubbleContent>{message.content}</BubbleContent>
          </Bubble>
        </MessageContent>
      </Message>
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

  const bubbleVariant = message.status === 'error' ? 'destructive' : 'muted'

  return (
    <Message>
      <MessageAvatar>
        <AgentModeAvatar mode={mode} />
      </MessageAvatar>
      <MessageContent>
        <Bubble variant={bubbleVariant}>
          <BubbleContent className={message.status === 'complete' ? 'max-w-none' : undefined}>
            {showPlaceholder ? (
              <span className="text-muted-foreground">…</span>
            ) : message.status === 'processing' || message.status === 'streaming' ? (
              <p className="whitespace-pre-wrap text-foreground">{message.content}</p>
            ) : (
              <MarkdownContent className="prose-base">{message.content}</MarkdownContent>
            )}
          </BubbleContent>
        </Bubble>
        {showActivity ? (
          toolCalls.length > 0 ? (
            <ChatToolCalls calls={toolCalls} />
          ) : (
            <Marker role="status">
              <MarkerContent>Working…</MarkerContent>
            </Marker>
          )
        ) : null}
      </MessageContent>
    </Message>
  )
}
