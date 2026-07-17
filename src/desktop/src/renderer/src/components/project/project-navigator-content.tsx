import { FolderPlus } from 'lucide-react'

import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import { SidebarSectionHeader } from '@/components/layout/sidebar/sidebar-section-header'
import {
  sidebarIconClass,
  type SidebarChatActions
} from '@/components/layout/sidebar/sidebar-utils'
import { OrphanChatsSection } from '@/components/project/orphan-chats-section'
import { ProjectGroupRow } from '@/components/project/project-group-row'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import type { ProjectChatGroup } from '@/lib/projects/group-chats'

type ProjectNavigatorContentProps = {
  isInitialProjectLoad: boolean
  isInitialChatLoad: boolean
  projectsError: Error | null
  chatsError: Error | null
  onRetryProjects: () => void
  onRetryChats: () => void
  projectsCount: number
  isAddingProject: boolean
  onAddProject: () => void
  regularGroups: ProjectChatGroup[]
  orphanGroup: ProjectChatGroup | null
  isCreating: boolean
  isProjectExpanded: (projectId: string) => boolean
  onToggleProject: (projectId: string) => void
  onNewChatInGroup: (group: ProjectChatGroup) => void
  expandedWorkspaceIds: ReadonlySet<string>
  onToggleWorkspace: (workspaceId: string) => void
  expandedParentChatIds: ReadonlySet<string>
  onToggleParentChat: (parentChatId: string) => void
  onRegisterOrphanPath: (path: string) => void
  chatActions: SidebarChatActions
}

export function ProjectNavigatorContent({
  isInitialProjectLoad,
  isInitialChatLoad,
  projectsError,
  chatsError,
  onRetryProjects,
  onRetryChats,
  projectsCount,
  isAddingProject,
  onAddProject,
  regularGroups,
  orphanGroup,
  isCreating,
  isProjectExpanded,
  onToggleProject,
  onNewChatInGroup,
  expandedWorkspaceIds,
  onToggleWorkspace,
  expandedParentChatIds,
  onToggleParentChat,
  onRegisterOrphanPath,
  chatActions
}: ProjectNavigatorContentProps): React.JSX.Element {
  if (isInitialProjectLoad || isInitialChatLoad) {
    return <p className="px-3 py-6 text-sm text-sidebar-muted">Loading…</p>
  }

  if (projectsError) {
    return (
      <div className="space-y-2 px-3 py-6">
        <p className="text-sm text-destructive">
          {projectsError.message || 'Failed to load projects.'}
        </p>
        <Button
          variant="ghost"
          size="sm"
          className="text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
          onClick={onRetryProjects}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (chatsError) {
    return (
      <div className="space-y-2 px-3 py-6">
        <p className="text-sm text-destructive">{chatsError.message || 'Failed to load chats.'}</p>
        <Button
          variant="ghost"
          size="sm"
          className="text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
          onClick={onRetryChats}
        >
          Retry
        </Button>
      </div>
    )
  }

  if (projectsCount === 0) {
    return (
      <div className="space-y-3 px-3 py-6">
        <p className="text-sm text-sidebar-muted">
          Add a project folder to start chatting with agents in that workspace.
        </p>
        <SidebarNavItem
          type="button"
          leading={<FolderPlus className={sidebarIconClass(false)} />}
          onClick={onAddProject}
          disabled={isAddingProject}
        >
          Add project
        </SidebarNavItem>
      </div>
    )
  }

  return (
    <>
      <SidebarSectionHeader isFirst>Projects</SidebarSectionHeader>
      <ScrollArea className="min-h-0 min-w-0 flex-1">
        <div className="min-w-0 space-y-1 px-3 pb-3">
          {regularGroups.map((group) => (
            <ProjectGroupRow
              key={group.id}
              group={group}
              isExpanded={isProjectExpanded(group.id)}
              onToggle={() => onToggleProject(group.id)}
              onNewChat={() => onNewChatInGroup(group)}
              isCreating={isCreating}
              expandedWorkspaceIds={expandedWorkspaceIds}
              onToggleWorkspace={onToggleWorkspace}
              expandedParentChatIds={expandedParentChatIds}
              onToggleParentChat={onToggleParentChat}
              actions={chatActions}
            />
          ))}
          {orphanGroup ? (
            <OrphanChatsSection
              group={orphanGroup}
              isExpanded={isProjectExpanded(orphanGroup.id)}
              onToggle={() => onToggleProject(orphanGroup.id)}
              onRegisterPath={onRegisterOrphanPath}
              expandedParentChatIds={expandedParentChatIds}
              onToggleParentChat={onToggleParentChat}
              actions={chatActions}
            />
          ) : null}
        </div>
      </ScrollArea>
    </>
  )
}
