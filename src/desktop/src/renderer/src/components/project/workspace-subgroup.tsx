import { ChevronDown, ChevronRight, Folder } from 'lucide-react'

import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import {
  sidebarIconClass,
  type SidebarChatActions
} from '@/components/layout/sidebar/sidebar-utils'
import { ChatTreeNode } from '@/components/project/chat-tree-node'
import { cn } from '@/lib/utils'
import type { WorkspaceChatSubGroup } from '@/lib/projects/group-chats'

type WorkspaceSubGroupProps = {
  workspaceGroup: WorkspaceChatSubGroup
  isExpanded: boolean
  onToggle: () => void
  expandedParentChatIds: ReadonlySet<string>
  onToggleParentChat: (chatId: string) => void
  actions: SidebarChatActions
}

export function WorkspaceSubGroup({
  workspaceGroup,
  isExpanded,
  onToggle,
  expandedParentChatIds,
  onToggleParentChat,
  actions
}: WorkspaceSubGroupProps): React.JSX.Element {
  return (
    <div className="min-w-0">
      <SidebarNavItem
        type="button"
        size="compact"
        title={workspaceGroup.path}
        leading={
          <>
            {isExpanded ? (
              <ChevronDown className={cn('size-3', sidebarIconClass(false))} />
            ) : (
              <ChevronRight className={cn('size-3', sidebarIconClass(false))} />
            )}
            <Folder className={cn('size-3 fill-sidebar-muted/20', sidebarIconClass(false))} />
          </>
        }
        onClick={onToggle}
      >
        {workspaceGroup.name}
      </SidebarNavItem>

      {isExpanded ? (
        <div className="ml-2 space-y-0.5 pl-3">
          {workspaceGroup.chatNodes.length === 0 ? (
            <p className="px-2.5 py-1.5 text-xs text-sidebar-muted">No chats yet</p>
          ) : (
            workspaceGroup.chatNodes.map((node) => (
              <ChatTreeNode
                key={node.chat.id}
                node={node}
                actions={actions}
                expandedParentChatIds={expandedParentChatIds}
                onToggleParentChat={onToggleParentChat}
              />
            ))
          )}
        </div>
      ) : null}
    </div>
  )
}
