import { LoaderCircleIcon, UserIcon, WrenchIcon } from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'

import { AssistantMessageContent } from '@/components/chat/assistant-message-content'
import type { ChatMarker, ChatMessage } from '@/lib/chat/types'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Bubble, BubbleContent } from '@/components/ui/bubble'
import { Marker, MarkerContent, MarkerIcon } from '@/components/ui/marker'
import { Message, MessageAvatar, MessageContent } from '@/components/ui/message'
import {
  MessageScroller,
  MessageScrollerButton,
  MessageScrollerContent,
  MessageScrollerItem,
  MessageScrollerProvider,
  MessageScrollerViewport
} from '@/components/ui/message-scroller'
import {
  Empty,
  EmptyDescription,
  EmptyHeader,
  EmptyMedia,
  EmptyTitle
} from '@/components/ui/empty'

type ChatMessageListProps = {
  messages: ChatMessage[]
  markers: ChatMarker[]
}

type RenderItem =
  | { kind: 'message'; message: ChatMessage }
  | { kind: 'marker'; marker: ChatMarker }

export function ChatMessageList({ messages, markers }: ChatMessageListProps): React.JSX.Element {
  if (messages.length === 0 && markers.length === 0) {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <Empty className="border-none">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <OrchiAiIcon className="size-5" />
            </EmptyMedia>
            <EmptyTitle>Start a conversation</EmptyTitle>
            <EmptyDescription>
              Ask Orchi to plan agent workflows, review code, or coordinate work across worktrees.
            </EmptyDescription>
          </EmptyHeader>
        </Empty>
      </div>
    )
  }

  const toolMarkers = markers.filter((marker) => marker.variant === 'tool')
  const statusMarker = markers.find((marker) => marker.variant === 'status')

  const items: RenderItem[] = [
    ...messages.map((message) => ({ kind: 'message' as const, message })),
    ...toolMarkers.map((marker) => ({ kind: 'marker' as const, marker })),
    ...(statusMarker ? [{ kind: 'marker' as const, marker: statusMarker }] : [])
  ]

  return (
    <MessageScrollerProvider>
      <MessageScroller className="flex-1">
        <MessageScrollerViewport>
          <MessageScrollerContent className="mx-auto w-full max-w-3xl px-4 py-6 md:px-8">
            {items.map((item, index) => (
              <MessageScrollerItem
                key={item.kind === 'message' ? item.message.id : item.marker.id}
                scrollAnchor={index === items.length - 1}
              >
                {item.kind === 'message' ? (
                  <ChatMessageRow message={item.message} />
                ) : (
                  <ChatMarkerRow marker={item.marker} />
                )}
              </MessageScrollerItem>
            ))}
          </MessageScrollerContent>
        </MessageScrollerViewport>
        <MessageScrollerButton />
      </MessageScroller>
    </MessageScrollerProvider>
  )
}

function ChatMarkerRow({ marker }: { marker: ChatMarker }): React.JSX.Element {
  const isTool = marker.variant === 'tool'

  return (
    <Marker variant={isTool ? 'separator' : 'default'}>
      <MarkerIcon>
        {isTool ? (
          <WrenchIcon className="size-3.5" />
        ) : (
          <LoaderCircleIcon className="size-3.5 animate-spin" />
        )}
      </MarkerIcon>
      <MarkerContent>{marker.content}</MarkerContent>
    </Marker>
  )
}

function ChatMessageRow({ message }: { message: ChatMessage }): React.JSX.Element {
  const isUser = message.role === 'user'
  const showPlaceholder = !isUser && message.status === 'processing' && message.content.length === 0

  return (
    <Message align={isUser ? 'end' : 'start'} className="gap-3">
      <MessageAvatar>
        <Avatar size="default">
          <AvatarFallback className={isUser ? undefined : 'bg-muted/40'}>
            {isUser ? (
              <UserIcon className="size-5" />
            ) : (
              <OrchiAiIcon className="size-5" />
            )}
          </AvatarFallback>
        </Avatar>
      </MessageAvatar>

      <MessageContent className={isUser ? 'w-auto max-w-[85%]' : 'max-w-[85%]'}>
        {isUser ? (
          <p className="whitespace-pre-wrap px-1 text-sm leading-relaxed text-foreground">
            {message.content}
          </p>
        ) : (
          <Bubble
            variant={message.status === 'error' ? 'destructive' : 'muted'}
            align="start"
          >
            <BubbleContent>
              <AssistantMessageContent
                content={message.content}
                status={message.status}
                showPlaceholder={showPlaceholder}
              />
            </BubbleContent>
          </Bubble>
        )}
      </MessageContent>
    </Message>
  )
}
