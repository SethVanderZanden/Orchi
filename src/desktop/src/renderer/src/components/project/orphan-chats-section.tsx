import { ChevronDown, ChevronRight, Folder } from 'lucide-react'

import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import {
  sidebarIconClass,
  type SidebarChatActions
} from '@/components/layout/sidebar/sidebar-utils'
import { ChatTreeNode } from '@/components/project/chat-tree-node'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { ProjectChatGroup } from '@/lib/projects/group-chats'

type OrphanChatsSectionProps = {
  group: ProjectChatGroup
  isExpanded: boolean
  onToggle: () => void
  onRegisterPath: (path: string) => void
  expandedParentChatIds: ReadonlySet<string>
  onToggleParentChat: (chatId: string) => void
  actions: SidebarChatActions
}

export function OrphanChatsSection({
  group,
  isExpanded,
  onToggle,
  onRegisterPath,
  expandedParentChatIds,
  onToggleParentChat,
  actions
}: OrphanChatsSectionProps): React.JSX.Element | null {
  if (!group.isOrphan || group.chatNodes.length === 0) {
    return null
  }

  const firstChat = group.chatNodes[0]?.chat

  return (
    <div className="min-w-0">
      <div className="sticky top-0 z-10 flex items-center gap-0.5 bg-sidebar">
        <div className="min-w-0 flex-1">
          <SidebarNavItem
            type="button"
            size="compact"
            className="font-medium"
            title={group.name}
            leading={
              <>
                {isExpanded ? (
                  <ChevronDown className={cn('size-3.5', sidebarIconClass(false))} />
                ) : (
                  <ChevronRight className={cn('size-3.5', sidebarIconClass(false))} />
                )}
                <Folder className={cn('size-3.5 fill-sidebar-muted/30', sidebarIconClass(false))} />
              </>
            }
            onClick={onToggle}
          >
            {group.name}
          </SidebarNavItem>
        </div>

        <Button
          variant="ghost"
          size="sm"
          className="h-7 shrink-0 px-2 text-xs text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
          onClick={() => {
            const path = firstChat?.workspacePath
            if (path) {
              onRegisterPath(path)
            }
          }}
        >
          Add as project
        </Button>
      </div>

      {isExpanded ? (
        <div className="ml-2 space-y-0.5 pl-3">
          {group.chatNodes.map((node) => (
            <ChatTreeNode
              key={node.chat.id}
              node={node}
              actions={actions}
              isExpanded={expandedParentChatIds.has(node.chat.id)}
              onToggleExpand={() => onToggleParentChat(node.chat.id)}
            />
          ))}
        </div>
      ) : null}
    </div>
  )
}
