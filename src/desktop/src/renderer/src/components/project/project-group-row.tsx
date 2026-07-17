import { ChevronDown, ChevronRight, Folder, MessageSquarePlus } from 'lucide-react'

import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import {
  sidebarIconClass,
  type SidebarChatActions
} from '@/components/layout/sidebar/sidebar-utils'
import { ChatTreeNode } from '@/components/project/chat-tree-node'
import { WorkspaceSubGroup } from '@/components/project/workspace-subgroup'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import type { ProjectChatGroup } from '@/lib/projects/group-chats'

type ProjectGroupRowProps = {
  group: ProjectChatGroup
  isExpanded: boolean
  onToggle: () => void
  onNewChat: () => void
  isCreating: boolean
  expandedWorkspaceIds: ReadonlySet<string>
  onToggleWorkspace: (workspaceId: string) => void
  expandedParentChatIds: ReadonlySet<string>
  onToggleParentChat: (chatId: string) => void
  actions: SidebarChatActions
}

export function ProjectGroupRow({
  group,
  isExpanded,
  onToggle,
  onNewChat,
  isCreating,
  expandedWorkspaceIds,
  onToggleWorkspace,
  expandedParentChatIds,
  onToggleParentChat,
  actions
}: ProjectGroupRowProps): React.JSX.Element {
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

        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="size-7 shrink-0 text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
              aria-label={`New chat in ${group.name}`}
              disabled={isCreating}
              onClick={onNewChat}
            >
              <MessageSquarePlus className="size-3.5" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>New chat</TooltipContent>
        </Tooltip>
      </div>

      {isExpanded ? (
        <div className="ml-2 space-y-0.5 pl-3">
          {group.isFlat ? (
            group.chatNodes.length === 0 ? (
              <p className="px-2.5 py-1.5 text-xs text-sidebar-muted">No chats yet</p>
            ) : (
              group.chatNodes.map((node) => (
                <ChatTreeNode
                  key={node.chat.id}
                  node={node}
                  actions={actions}
                  expandedParentChatIds={expandedParentChatIds}
                  onToggleParentChat={onToggleParentChat}
                />
              ))
            )
          ) : (
            group.workspaceGroups.map((workspaceGroup) => (
              <WorkspaceSubGroup
                key={workspaceGroup.id}
                workspaceGroup={workspaceGroup}
                isExpanded={expandedWorkspaceIds.has(workspaceGroup.id)}
                onToggle={() => onToggleWorkspace(workspaceGroup.id)}
                expandedParentChatIds={expandedParentChatIds}
                onToggleParentChat={onToggleParentChat}
                actions={actions}
              />
            ))
          )}
        </div>
      ) : null}
    </div>
  )
}
