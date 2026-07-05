import { useCallback } from 'react'
import { useNavigate } from '@tanstack/react-router'
import { FolderPlus } from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { SidebarHeader } from '@/components/layout/sidebar/sidebar-header'
import { SidebarResizeHandle } from '@/components/layout/sidebar/sidebar-resize-handle'
import { SidebarSearch } from '@/components/layout/sidebar/sidebar-search'
import { ProjectNavigatorContent } from '@/components/project/project-navigator-content'
import { SidebarFooter } from '@/components/project/sidebar-footer'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { useProjectNavigatorState } from '@/hooks/use-project-navigator-state'
import { useProjectLayout } from '@/providers/project-layout-provider'

export function ProjectNavigator(): React.JSX.Element {
  const navigate = useNavigate()
  const { sidebarWidth, setSidebarWidth } = useProjectLayout()

  const navigateToChat = useCallback(
    (chatId: string) => navigate({ to: '/chat/$chatId', params: { chatId } }),
    [navigate]
  )
  const navigateToSettings = useCallback(() => navigate({ to: '/settings' }), [navigate])

  const state = useProjectNavigatorState(navigateToChat)

  return (
    <div className="flex h-full shrink-0" style={{ width: sidebarWidth }}>
      <aside className="flex h-full min-w-0 flex-1 flex-col bg-sidebar text-sidebar-foreground">
        <SidebarHeader
          title="Orchi"
          subtitle="AI orchestrator"
          icon={<OrchiAiIcon className="size-5" />}
          endContent={
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-7 text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
                  aria-label="Add project"
                  onClick={() => void state.handleAddProject()}
                  disabled={state.isAddingProject}
                >
                  <FolderPlus className="size-4" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>Add project</TooltipContent>
            </Tooltip>
          }
        />

        <SidebarSearch value={state.searchQuery} onChange={state.setSearchQuery} />

        <ProjectNavigatorContent
          isInitialProjectLoad={state.isInitialProjectLoad}
          isInitialChatLoad={state.isInitialChatLoad}
          projectsError={state.projectsError}
          chatsError={state.chatsError}
          onRetryProjects={() => void state.refetchProjects()}
          onRetryChats={() => void state.refetchChats()}
          projectsCount={state.projects.length}
          isAddingProject={state.isAddingProject}
          onAddProject={() => void state.handleAddProject()}
          regularGroups={state.regularGroups}
          orphanGroup={state.orphanGroup}
          isCreating={state.isCreating}
          isProjectExpanded={state.isProjectExpanded}
          onToggleProject={state.toggleProjectExpanded}
          onNewChatInGroup={(group) => void state.createChatInGroup(group)}
          expandedWorkspaceIds={state.expandedWorkspaceIds}
          onToggleWorkspace={state.toggleWorkspaceExpanded}
          expandedParentChatIds={state.expandedParentChatIds}
          onToggleParentChat={state.toggleParentExpanded}
          onRegisterOrphanPath={state.handleRegisterOrphanPath}
          chatActions={state.chatActions}
        />

        {state.projects.length > 0 ? (
          <SidebarFooter
            isSettingsActive={Boolean(state.settingsMatch)}
            onOpenSettings={navigateToSettings}
            onAddProject={() => void state.handleAddProject()}
            isAddingProject={state.isAddingProject}
          />
        ) : null}
      </aside>
      <SidebarResizeHandle width={sidebarWidth} onWidthChange={setSidebarWidth} />
    </div>
  )
}
