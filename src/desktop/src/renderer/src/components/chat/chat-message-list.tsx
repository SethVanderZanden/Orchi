import { ChevronRightIcon, LoaderCircleIcon, UserIcon, WrenchIcon } from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'

import { AssistantMessageContent } from '@/components/chat/assistant-message-content'
import type { ChatMarker, ChatMessage } from '@/lib/chat/types'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Bubble, BubbleContent } from '@/components/ui/bubble'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
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
import { cn } from '@/lib/utils'

type ChatMessageListProps = {
  messages: ChatMessage[]
  markers: ChatMarker[]
}

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

  const lastAssistantIndex = messages.findLastIndex((message) => message.role === 'assistant')
  const lastAssistant = lastAssistantIndex >= 0 ? messages[lastAssistantIndex] : null
  const isActiveTurn =
    lastAssistant?.status === 'processing' || lastAssistant?.status === 'streaming'
  const activeMarkers = isActiveTurn ? markers : []

  return (
    <MessageScrollerProvider>
      <MessageScroller className="flex-1">
        <MessageScrollerViewport>
          <MessageScrollerContent className="mx-auto w-full max-w-3xl px-4 py-6 md:px-8">
            {messages.map((message, index) => (
              <MessageScrollerItem
                key={message.id}
                scrollAnchor={index === messages.length - 1}
              >
                <ChatMessageRow
                  message={message}
                  markers={index === lastAssistantIndex ? activeMarkers : []}
                />
              </MessageScrollerItem>
            ))}
          </MessageScrollerContent>
        </MessageScrollerViewport>
        <MessageScrollerButton />
      </MessageScroller>
    </MessageScrollerProvider>
  )
}

type AssistantActivityProps = {
  markers: ChatMarker[]
}

function AssistantActivity({ markers }: AssistantActivityProps): React.JSX.Element | null {
  const toolMarkers = markers.filter((marker) => marker.variant === 'tool')
  const hasStatus = markers.some((marker) => marker.variant === 'status')

  if (toolMarkers.length === 0 && !hasStatus) {
    return null
  }

  if (toolMarkers.length === 0) {
    return (
      <div className="text-muted-foreground flex items-center gap-1.5 px-1 pt-2 text-xs">
        <LoaderCircleIcon className="size-3 animate-spin" />
        <span>Working…</span>
      </div>
    )
  }

  const stepLabel =
    toolMarkers.length === 1 ? '1 step' : `${toolMarkers.length} steps`

  return (
    <Collapsible className="pt-2">
      <CollapsibleTrigger className="text-muted-foreground hover:text-foreground flex w-full items-center gap-1.5 px-1 text-xs transition-colors [&[data-state=open]>svg:first-child]:rotate-90">
        <ChevronRightIcon className="size-3 shrink-0 transition-transform" />
        <WrenchIcon className="size-3 shrink-0" />
        <span>{stepLabel}</span>
        {hasStatus ? <LoaderCircleIcon className="ml-auto size-3 animate-spin" /> : null}
      </CollapsibleTrigger>
      <CollapsibleContent>
        <ul className="text-muted-foreground mt-1.5 space-y-1 border-l pl-3 text-xs">
          {toolMarkers.map((marker) => (
            <li key={marker.id} className="leading-relaxed">
              {marker.content}
            </li>
          ))}
        </ul>
      </CollapsibleContent>
    </Collapsible>
  )
}

type ChatMessageRowProps = {
  message: ChatMessage
  markers: ChatMarker[]
}

function ChatMessageRow({ message, markers }: ChatMessageRowProps): React.JSX.Element {
  const isUser = message.role === 'user'
  const showPlaceholder = !isUser && message.status === 'processing' && message.content.length === 0
  const showActivity = !isUser && markers.length > 0

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

      <MessageContent className={cn(isUser ? 'w-auto max-w-[85%]' : 'max-w-[85%]')}>
        {isUser ? (
          <p className="whitespace-pre-wrap px-1 text-sm leading-relaxed text-foreground">
            {message.content}
          </p>
        ) : (
          <>
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
            {showActivity ? <AssistantActivity markers={markers} /> : null}
          </>
        )}
      </MessageContent>
    </Message>
  )
}
