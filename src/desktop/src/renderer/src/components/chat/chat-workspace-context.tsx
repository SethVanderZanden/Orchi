import { memo, useCallback, useMemo } from 'react'
import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { findProjectForWorkspace, findWorkspaceInProjects } from '@/lib/projects/find-workspace'
import type { Project } from '@/lib/projects/types'
import { cn } from '@/lib/utils'

type ChatWorkspaceContextProps = {
  workspaceId: string | null
  workspaceName: string | null
  projectName: string | null
  projects: Project[]
  canChangeWorkspace?: boolean
  onWorkspaceChange?: (workspaceId: string) => void
  className?: string
}

export const ChatWorkspaceContext = memo(function ChatWorkspaceContext({
  workspaceId,
  workspaceName,
  projectName,
  projects,
  canChangeWorkspace = false,
  onWorkspaceChange,
  className
}: ChatWorkspaceContextProps): React.JSX.Element {
  const workspace = useMemo(
    () => findWorkspaceInProjects(projects, workspaceId),
    [projects, workspaceId]
  )

  const label = workspaceName ?? workspace?.name ?? 'No workspace'
  const subtitle = useMemo(() => {
    if (projectName) {
      return projectName
    }

    return findProjectForWorkspace(projects, workspaceId)?.name
  }, [projectName, projects, workspaceId])

  const handleWorkspaceSelect = useCallback(
    (selectedWorkspaceId: string) => {
      if (!canChangeWorkspace || selectedWorkspaceId === workspaceId) {
        return
      }

      onWorkspaceChange?.(selectedWorkspaceId)
    },
    [canChangeWorkspace, onWorkspaceChange, workspaceId]
  )

  return (
    <div className={cn('flex items-center gap-1 text-sm text-muted-foreground', className)}>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 max-w-[16rem] gap-1 px-2 font-normal text-muted-foreground hover:text-foreground"
            aria-label="Workspace"
            title={
              canChangeWorkspace
                ? 'Switch workspace'
                : 'Workspace is set when the chat is created'
            }
          >
            <span className="flex min-w-0 flex-col items-start leading-tight">
              <span className="w-full truncate">{label}</span>
              {subtitle ? (
                <span className="w-full truncate text-xs text-muted-foreground/80">{subtitle}</span>
              ) : null}
            </span>
            <ChevronDown className="size-3.5 shrink-0 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start" className="max-h-80 w-72 overflow-y-auto">
          {projects.length === 0 ? (
            <DropdownMenuItem disabled>No projects</DropdownMenuItem>
          ) : (
            projects.map((project, index) => (
              <DropdownMenuGroup key={project.id}>
                {index > 0 ? <DropdownMenuSeparator /> : null}
                <DropdownMenuLabel className="text-xs font-medium text-muted-foreground">
                  {project.name}
                </DropdownMenuLabel>
                {project.workspaces.map((entry) => (
                  <DropdownMenuItem
                    key={entry.id}
                    disabled={!canChangeWorkspace && entry.id !== workspaceId}
                    className={cn(entry.id === workspaceId && 'font-medium')}
                    onSelect={() => handleWorkspaceSelect(entry.id)}
                  >
                    <span className="flex min-w-0 flex-col">
                      <span className="truncate">{entry.name}</span>
                      <span className="truncate text-xs text-muted-foreground">{entry.path}</span>
                    </span>
                    {entry.id === workspaceId ? (
                      <span className="ml-auto shrink-0 text-xs text-muted-foreground">current</span>
                    ) : null}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuGroup>
            ))
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
})
