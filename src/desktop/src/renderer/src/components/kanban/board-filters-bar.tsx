import { Calendar, ChevronDown, FolderKanban } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import {
  BOARD_DATE_RANGE_OPTIONS,
  getBoardDateRangeLabel,
  type BoardDateRange,
  type BoardProjectFilter
} from '@/lib/kanban/board-filters'
import type { Project } from '@/lib/projects/types'
import { cn } from '@/lib/utils'

type BoardFiltersBarProps = {
  projectFilter: BoardProjectFilter
  dateRange: BoardDateRange
  projects: Project[]
  onProjectFilterChange: (projectFilter: BoardProjectFilter) => void
  onDateRangeChange: (dateRange: BoardDateRange) => void
  className?: string
}

function getProjectFilterLabel(projectFilter: BoardProjectFilter, projects: Project[]): string {
  if (projectFilter === 'all') {
    return 'All projects'
  }

  return projects.find((project) => project.id === projectFilter)?.name ?? 'Project'
}

export function BoardFiltersBar({
  projectFilter,
  dateRange,
  projects,
  onProjectFilterChange,
  onDateRangeChange,
  className
}: BoardFiltersBarProps): React.JSX.Element {
  const projectLabel = getProjectFilterLabel(projectFilter, projects)
  const dateLabel = getBoardDateRangeLabel(dateRange)
  const projectFilterActive = projectFilter !== 'all'
  const dateFilterActive = dateRange !== 'all'

  return (
    <div className={cn('flex items-center gap-1.5', className)}>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className={cn(
              'h-8 gap-1.5 px-2.5 text-sm font-normal',
              projectFilterActive && 'border-primary/40 bg-primary/5'
            )}
            aria-label="Filter board by project"
          >
            <FolderKanban className="size-3.5" />
            <span className="max-w-[10rem] truncate">{projectLabel}</span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuLabel>Project</DropdownMenuLabel>
          <DropdownMenuRadioGroup
            value={projectFilter}
            onValueChange={(value) => onProjectFilterChange(value as BoardProjectFilter)}
          >
            <DropdownMenuRadioItem value="all">All projects</DropdownMenuRadioItem>
            {projects.length > 0 ? <DropdownMenuSeparator /> : null}
            {projects.map((project) => (
              <DropdownMenuRadioItem key={project.id} value={project.id}>
                {project.name}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className={cn(
              'h-8 gap-1.5 px-2.5 text-sm font-normal',
              dateFilterActive && 'border-primary/40 bg-primary/5'
            )}
            aria-label="Filter board by date"
          >
            <Calendar className="size-3.5" />
            <span>{dateLabel}</span>
            <ChevronDown className="size-3.5 opacity-60" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-48">
          <DropdownMenuLabel>Updated</DropdownMenuLabel>
          <DropdownMenuRadioGroup
            value={dateRange}
            onValueChange={(value) => onDateRangeChange(value as BoardDateRange)}
          >
            {BOARD_DATE_RANGE_OPTIONS.map((option) => (
              <DropdownMenuRadioItem key={option.value} value={option.value}>
                {option.label}
              </DropdownMenuRadioItem>
            ))}
          </DropdownMenuRadioGroup>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  )
}
