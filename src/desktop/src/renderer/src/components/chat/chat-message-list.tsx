import { SparklesIcon, UserIcon } from 'lucide-react'

import type { ChatMessage } from '@/lib/chat/types'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Bubble, BubbleContent } from '@/components/ui/bubble'
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
}

export function ChatMessageList({ messages }: ChatMessageListProps): React.JSX.Element {
  if (messages.length === 0) {
    return (
      <div className="flex flex-1 items-center justify-center p-8">
        <Empty className="border-none">
          <EmptyHeader>
            <EmptyMedia variant="icon">
              <SparklesIcon />
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
                <ChatMessageRow message={message} />
              </MessageScrollerItem>
            ))}
          </MessageScrollerContent>
        </MessageScrollerViewport>
        <MessageScrollerButton />
      </MessageScroller>
    </MessageScrollerProvider>
  )
}

function ChatMessageRow({ message }: { message: ChatMessage }): React.JSX.Element {
  const isUser = message.role === 'user'

  return (
    <Message align={isUser ? 'end' : 'start'} className="gap-3">
      {!isUser && (
        <MessageAvatar>
          <Avatar size="sm">
            <AvatarFallback className="bg-primary text-primary-foreground">
              <SparklesIcon className="size-3.5" />
            </AvatarFallback>
          </Avatar>
        </MessageAvatar>
      )}

      <MessageContent className="max-w-[85%]">
        <Bubble variant={isUser ? 'secondary' : 'muted'} align={isUser ? 'end' : 'start'}>
          <BubbleContent className="whitespace-pre-wrap">{message.content}</BubbleContent>
        </Bubble>
      </MessageContent>

      {isUser && (
        <MessageAvatar>
          <Avatar size="sm">
            <AvatarFallback>
              <UserIcon className="size-3.5" />
            </AvatarFallback>
          </Avatar>
        </MessageAvatar>
      )}
    </Message>
  )
}
