import { MessageSquare } from 'lucide-react'

import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { ChatToolCalls } from '@/components/chat/chat-tool-calls'
import { EmptyState } from '@/components/empty-state'
import { MarkdownContent } from '@/components/markdown-content'
import { cn } from '@/lib/utils'
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
        <ChatMessageRow
          key={message.id}
          message={message}
          markers={index === lastAssistantIndex ? activeMarkers : []}
        />
      ))}
    </div>
  )
}

type ChatMessageRowProps = {
  message: OrchiChatMessage
  markers: ChatMarker[]
}

function ChatMessageRow({ message, markers }: ChatMessageRowProps): React.JSX.Element {
  const isUser = message.role === 'user'
  const showPlaceholder = !isUser && message.status === 'processing' && message.content.length === 0
  const showActivity = !isUser && markers.length > 0

  if (isUser) {
    return (
      <div className="flex justify-end">
        <div className="max-w-[85%] rounded-2xl rounded-br-md bg-primary px-4 py-2.5 text-sm text-primary-foreground">
          {message.content}
        </div>
      </div>
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
    <div className="flex gap-3">
      <Avatar className="size-7">
        <AvatarFallback className="text-[10px]">Or</AvatarFallback>
      </Avatar>
      <div className="min-w-0 flex-1 space-y-2">
        <div
          className={cn(
            'max-w-none text-sm',
            message.status === 'error' && 'rounded-lg border border-destructive/40 bg-destructive/10 px-3 py-2'
          )}
        >
          {showPlaceholder ? (
            <span className="text-muted-foreground">…</span>
          ) : message.status === 'processing' || message.status === 'streaming' ? (
            <p className="whitespace-pre-wrap text-foreground">{message.content}</p>
          ) : (
            <MarkdownContent>{message.content}</MarkdownContent>
          )}
        </div>
        {showActivity ? (
          toolCalls.length > 0 ? (
            <ChatToolCalls calls={toolCalls} />
          ) : (
            <p className="text-[11px] text-muted-foreground">Working…</p>
          )
        ) : null}
      </div>
    </div>
  )
}
