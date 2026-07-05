import { Bot, ChevronDown, ChevronRight, Shield, Trash2 } from 'lucide-react'

import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import {
  sidebarIconClass,
  type SidebarChatActions
} from '@/components/layout/sidebar/sidebar-utils'
import { RelativeTime } from '@/components/relative-time'
import { Button } from '@/components/ui/button'
import type { ChatThread } from '@/lib/chat/types'
import { isReviewChildChat, type ChatTreeNode as ChatTreeNodeData } from '@/lib/projects/chat-tree'
import { cn } from '@/lib/utils'

type ChatTreeNodeProps = {
  node: ChatTreeNodeData
  actions: SidebarChatActions
  isExpanded: boolean
  onToggleExpand: () => void
}

function ChatRow({
  chat,
  isChild,
  actions
}: {
  chat: ChatThread
  isChild: boolean
  actions: SidebarChatActions
}): React.JSX.Element {
  const isReview = isChild && isReviewChildChat(chat)
  const statusVariant = actions.getChatSidebarStatus(chat)
  const isActive = chat.id === actions.activeChatId

  return (
    <div className="group relative min-w-0">
      <SidebarNavItem
        type="button"
        title={chat.title}
        size={isChild ? 'compact' : 'default'}
        isActive={isActive}
        leading={
          <>
            <ChatStatusDot variant={statusVariant} />
            {isChild ? (
              isReview ? (
                <Shield className={cn('size-3', sidebarIconClass(isActive))} aria-hidden />
              ) : (
                <Bot className={cn('size-3', sidebarIconClass(isActive))} aria-hidden />
              )
            ) : null}
          </>
        }
        trailing={
          <RelativeTime
            value={chat.updatedAt}
            compact
            className={cn(
              'shrink-0 tabular-nums text-sidebar-muted transition-colors duration-150 group-hover:text-sidebar-accent-foreground',
              isActive && 'text-sidebar-accent-foreground',
              isChild ? 'text-[10px]' : 'text-[11px]'
            )}
          />
        }
        onClick={() => actions.onSelectChat(chat.id)}
      >
        {chat.title}
      </SidebarNavItem>
      <Button
        variant="ghost"
        size="icon"
        className="pointer-events-none absolute top-1 right-0.5 z-10 size-6 bg-sidebar/90 text-sidebar-muted opacity-0 backdrop-blur-sm transition-opacity duration-150 ease-out group-hover:pointer-events-auto group-hover:text-sidebar-accent-foreground group-hover:opacity-100 focus-visible:pointer-events-auto focus-visible:opacity-100"
        aria-label={`Delete ${chat.title}`}
        disabled={actions.isChatSending(chat.id) || actions.isDeletingChat(chat.id)}
        onClick={(event) => {
          event.stopPropagation()
          actions.onRequestDelete(chat)
        }}
      >
        <Trash2 className={isChild ? 'size-3' : 'size-3.5'} />
      </Button>
    </div>
  )
}

export function ChatTreeNode({
  node,
  actions,
  isExpanded,
  onToggleExpand
}: ChatTreeNodeProps): React.JSX.Element {
  const hasChildren = node.children.length > 0

  if (!hasChildren) {
    return <ChatRow chat={node.chat} isChild={false} actions={actions} />
  }

  return (
    <div className="min-w-0">
      <div className="flex min-w-0 items-center gap-0.5">
        <button
          type="button"
          className="flex size-6 shrink-0 items-center justify-center rounded-md text-sidebar-muted transition-colors duration-150 ease-out hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
          aria-label={isExpanded ? 'Collapse child agents' : 'Expand child agents'}
          onClick={onToggleExpand}
        >
          {isExpanded ? <ChevronDown className="size-3" /> : <ChevronRight className="size-3" />}
        </button>
        <div className="min-w-0 flex-1">
          <ChatRow chat={node.chat} isChild={false} actions={actions} />
        </div>
      </div>

      {isExpanded ? (
        <div className="ml-2 space-y-0.5 pl-3">
          {node.children.map((child) => (
            <ChatRow key={child.id} chat={child} isChild actions={actions} />
          ))}
        </div>
      ) : null}
    </div>
  )
}
