import { memo } from 'react'
import { MessageSquare } from 'lucide-react'

import { AgentModeAvatar } from '@/components/chat/agent-mode-avatar'
import { MessageSelectionMenu } from '@/components/chat/message-selection-menu'
import { ChatToolCalls } from '@/components/chat/chat-tool-calls'
import { EmptyState } from '@/components/empty-state'
import { AssistantMessageContent } from '@/components/chat/assistant-message-content'
import { MarkdownContent } from '@/components/markdown-content'
import { Bubble, BubbleContent } from '@/components/ui/bubble'
import { Marker, MarkerContent } from '@/components/ui/marker'
import { Message, MessageAvatar, MessageContent } from '@/components/ui/message'
import { MessageScrollerItem } from '@/components/ui/message-scroller'
import { getChatMessageDisplayState } from '@/lib/chat/message-display'
import type { AgentMode, ChatMarker, ChatMessage as OrchiChatMessage } from '@/lib/chat/types'

const EMPTY_MARKERS: ChatMarker[] = []

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
  const activeMarkers = isActiveTurn && markers.length > 0 ? markers : EMPTY_MARKERS

  return (
    <div className="mx-auto flex w-full max-w-3xl flex-col gap-7 px-6 py-8">
      {messages.map((message, index) => {
        const rowMarkers = index === lastAssistantIndex ? activeMarkers : EMPTY_MARKERS
        const display = getChatMessageDisplayState({ message, mode, rowMarkers })

        if (!display.shouldRender) {
          return null
        }

        return (
          <MessageScrollerItem key={message.id} scrollAnchor={message.role === 'user'}>
            <ChatMessageRow
              message={message}
              displayContent={display.displayContent}
              showPlaceholder={display.showPlaceholder}
              showActivity={display.showActivity}
              showEmptyResponse={display.showEmptyResponse}
              markers={rowMarkers}
              mode={mode}
            />
          </MessageScrollerItem>
        )
      })}
    </div>
  )
}

type ChatMessageRowProps = {
  message: OrchiChatMessage
  displayContent: string
  showPlaceholder: boolean
  showActivity: boolean
  showEmptyResponse: boolean
  markers: ChatMarker[]
  mode: AgentMode
}

const ChatMessageRow = memo(function ChatMessageRow({
  message,
  displayContent,
  showPlaceholder,
  showActivity,
  showEmptyResponse,
  markers,
  mode
}: ChatMessageRowProps): React.JSX.Element {
  if (message.role === 'user') {
    return (
      <Message align="end">
        <MessageContent>
          <MessageSelectionMenu>
            <Bubble>
              <BubbleContent className="overflow-x-auto">
                <MarkdownContent>{message.content}</MarkdownContent>
              </BubbleContent>
            </Bubble>
          </MessageSelectionMenu>
        </MessageContent>
      </Message>
    )
  }

  const toolMarkers = markers.filter((marker) => marker.variant === 'tool')
  const isRunning = markers.some((item) => item.variant === 'status')
  const toolCalls = toolMarkers.map((marker, index) => ({
    key: marker.id,
    label: marker.content,
    status:
      isRunning && index === toolMarkers.length - 1 ? ('running' as const) : ('complete' as const)
  }))

  return (
    <Message>
      <MessageAvatar>
        <AgentModeAvatar mode={mode} />
      </MessageAvatar>
      <MessageContent>
        <MessageSelectionMenu>
          {showPlaceholder ? (
            <span className="text-muted-foreground">…</span>
          ) : showEmptyResponse ? (
            <span className="text-muted-foreground">No response from agent.</span>
          ) : displayContent.length > 0 ? (
            <AssistantMessageContent
              content={displayContent}
              status={message.status}
              className={message.status === 'error' ? 'prose-base text-destructive' : 'prose-base'}
            />
          ) : null}
        </MessageSelectionMenu>
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
})
