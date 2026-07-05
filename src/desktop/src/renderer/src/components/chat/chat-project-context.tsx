import { ChevronDown } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import type { Project } from '@/lib/projects/types'
import { cn } from '@/lib/utils'

type ChatProjectContextProps = {
  projectId: string | null
  projectName: string | null
  projects: Project[]
  className?: string
}

export function ChatProjectContext({
  projectId,
  projectName,
  projects,
  className
}: ChatProjectContextProps): React.JSX.Element {
  const label = projectName ?? 'No project'

  return (
    <div className={cn('flex items-center gap-1 text-sm text-muted-foreground', className)}>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="h-7 gap-1 px-2 font-normal text-muted-foreground hover:text-foreground"
            aria-label="Project"
            title="Project is set when the chat is created"
          >
            <span className="max-w-[12rem] truncate">{label}</span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start">
          {projects.length === 0 ? (
            <DropdownMenuItem disabled>No projects</DropdownMenuItem>
          ) : (
            projects.map((project) => (
              <DropdownMenuItem
                key={project.id}
                disabled={project.id !== projectId}
                className={cn(project.id === projectId && 'font-medium')}
              >
                {project.name}
                {project.id === projectId ? ' (current)' : ''}
              </DropdownMenuItem>
            ))
          )}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
