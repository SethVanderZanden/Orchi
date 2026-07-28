import { memo } from 'react'
import { MessageSquare } from 'lucide-react'

import { AgentModeAvatar } from '@/components/chat/agent-mode-avatar'
import { MessageSelectionMenu } from '@/components/chat/message-selection-menu'
import { ChatToolCalls } from '@/components/chat/chat-tool-calls'
import { EmptyState } from '@/components/empty-state'
import { AssistantMessageContent } from '@/components/chat/assistant-message-content'
import { MarkdownContent } from '@/components/markdown-content'
import { MessageAttachments } from '@/components/chat/message-attachments'
import { Bubble, BubbleContent } from '@/components/ui/bubble'
import { Marker, MarkerContent } from '@/components/ui/marker'
import { Message, MessageAvatar, MessageContent } from '@/components/ui/message'
import { MessageScrollerItem } from '@/components/ui/message-scroller'
import { getChatMessageDisplayState } from '@/lib/chat/message-display'
import type { AgentMode, ChatMarker, ChatMessage as OrchiChatMessage } from '@/lib/chat/types'

const EMPTY_MARKERS: ChatMarker[] = []

type ChatMessageListProps = {
  chatId: string
  messages: OrchiChatMessage[]
  markers: ChatMarker[]
  mode: AgentMode
  hideEmptyState?: boolean
}

export function OrchiChatMessageList({
  chatId,
  messages,
  markers,
  mode,
  hideEmptyState = false
}: ChatMessageListProps): React.JSX.Element | null {
  if (messages.length === 0 && markers.length === 0) {
    if (hideEmptyState) {
      return null
    }

    return (
      <MessageScrollerItem messageId="empty-state">
        <EmptyState
          title="Start a conversation"
          description="Ask Orchi to help with coding tasks in your workspace."
          icon={<MessageSquare className="size-8" />}
        />
      </MessageScrollerItem>
    )
  }

  const lastAssistantIndex = messages.findLastIndex((message) => message.role === 'assistant')
  const lastAssistant = lastAssistantIndex >= 0 ? messages[lastAssistantIndex] : null
  const isActiveTurn =
    lastAssistant?.status === 'processing' || lastAssistant?.status === 'streaming'
  const activeMarkers = isActiveTurn && markers.length > 0 ? markers : EMPTY_MARKERS

  return (
    <>
      {messages.map((message, index) => {
        const rowMarkers = index === lastAssistantIndex ? activeMarkers : EMPTY_MARKERS
        const display = getChatMessageDisplayState({ message, mode, rowMarkers })

        if (!display.shouldRender) {
          return null
        }

        return (
          <MessageScrollerItem
            key={message.id}
            messageId={message.id}
            scrollAnchor={message.role === 'user'}
          >
            <ChatMessageRow
              chatId={chatId}
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
    </>
  )
}

type ChatMessageRowProps = {
  chatId: string
  message: OrchiChatMessage
  displayContent: string
  showPlaceholder: boolean
  showActivity: boolean
  showEmptyResponse: boolean
  markers: ChatMarker[]
  mode: AgentMode
}

const ChatMessageRow = memo(function ChatMessageRow({
  chatId,
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
                {message.content ? <MarkdownContent>{message.content}</MarkdownContent> : null}
                {message.attachments?.length ? (
                  <MessageAttachments chatId={chatId} attachments={message.attachments} />
                ) : null}
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
