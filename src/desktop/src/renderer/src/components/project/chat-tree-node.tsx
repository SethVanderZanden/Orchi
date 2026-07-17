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
  expandedParentChatIds: ReadonlySet<string>
  onToggleParentChat: (chatId: string) => void
  depth?: number
}

function ChatRow({
  chat,
  depth,
  actions
}: {
  chat: ChatThread
  depth: number
  actions: SidebarChatActions
}): React.JSX.Element {
  const isChild = depth > 0
  const isReview = isChild && isReviewChildChat(chat)
  const statusVariant = actions.getChatSidebarStatus(chat)
  const isActive = chat.id === actions.activeChatId

  return (
    <div className="group/chat-row relative min-w-0">
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
        className="pointer-events-none absolute top-1 right-0.5 z-10 size-6 bg-sidebar/90 text-sidebar-muted opacity-0 backdrop-blur-sm transition-opacity duration-150 ease-out group-hover/chat-row:pointer-events-auto group-hover/chat-row:text-sidebar-accent-foreground group-hover/chat-row:opacity-100 focus-visible:pointer-events-auto focus-visible:opacity-100 disabled:pointer-events-none disabled:opacity-0 disabled:group-hover/chat-row:pointer-events-none disabled:group-hover/chat-row:opacity-0"
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

function NestedChildren({
  node,
  actions,
  expandedParentChatIds,
  onToggleParentChat,
  depth
}: {
  node: ChatTreeNodeData
  actions: SidebarChatActions
  expandedParentChatIds: ReadonlySet<string>
  onToggleParentChat: (chatId: string) => void
  depth: number
}): React.JSX.Element {
  return (
    <div className="ml-2 space-y-0.5 pl-3">
      {node.children.map((child) => (
        <ChatTreeNode
          key={child.chat.id}
          node={child}
          actions={actions}
          expandedParentChatIds={expandedParentChatIds}
          onToggleParentChat={onToggleParentChat}
          depth={depth + 1}
        />
      ))}
    </div>
  )
}

export function ChatTreeNode({
  node,
  actions,
  expandedParentChatIds,
  onToggleParentChat,
  depth = 0
}: ChatTreeNodeProps): React.JSX.Element {
  const hasChildren = node.children.length > 0

  if (!hasChildren) {
    return <ChatRow chat={node.chat} depth={depth} actions={actions} />
  }

  // Only orchestration roots collapse. Nested reviews always stay open under their
  // implementation sibling so agent rows stay aligned (no per-agent chevron).
  if (depth > 0) {
    return (
      <div className="min-w-0">
        <ChatRow chat={node.chat} depth={depth} actions={actions} />
        <NestedChildren
          node={node}
          actions={actions}
          expandedParentChatIds={expandedParentChatIds}
          onToggleParentChat={onToggleParentChat}
          depth={depth}
        />
      </div>
    )
  }

  const isExpanded = expandedParentChatIds.has(node.chat.id)

  return (
    <div className="min-w-0">
      <div className="flex min-w-0 items-center gap-0.5">
        <button
          type="button"
          className="flex size-6 shrink-0 items-center justify-center rounded-md text-sidebar-muted transition-colors duration-150 ease-out hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
          aria-label={isExpanded ? 'Collapse child chats' : 'Expand child chats'}
          onClick={() => onToggleParentChat(node.chat.id)}
        >
          {isExpanded ? <ChevronDown className="size-3" /> : <ChevronRight className="size-3" />}
        </button>
        <div className="min-w-0 flex-1">
          <ChatRow chat={node.chat} depth={depth} actions={actions} />
        </div>
      </div>

      {isExpanded ? (
        <NestedChildren
          node={node}
          actions={actions}
          expandedParentChatIds={expandedParentChatIds}
          onToggleParentChat={onToggleParentChat}
          depth={depth}
        />
      ) : null}
    </div>
  )
}
