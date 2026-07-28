import { MoreHorizontal, Trash2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'

type AgentsSidebarBulkActionsMenuProps = {
  deletableCount: number
  skippedSendingCount: number
  disabled?: boolean
  onDeleteVisible: () => void
}

export function AgentsSidebarBulkActionsMenu({
  deletableCount,
  skippedSendingCount,
  disabled = false,
  onDeleteVisible
}: AgentsSidebarBulkActionsMenuProps): React.JSX.Element | null {
  if (deletableCount === 0 && skippedSendingCount === 0) {
    return null
  }

  const deleteLabel =
    deletableCount === 1 ? 'Delete 1 visible chat' : `Delete ${deletableCount} visible chats`

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="size-7 shrink-0 text-sidebar-muted hover:text-sidebar-foreground"
          aria-label="Bulk chat actions"
          disabled={disabled || deletableCount === 0}
        >
          <MoreHorizontal className="size-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem
          className="font-medium text-destructive focus:bg-destructive/10 focus:text-destructive dark:focus:bg-destructive/20 [&_svg]:text-destructive!"
          disabled={deletableCount === 0}
          onClick={onDeleteVisible}
        >
          <Trash2 className="size-4" />
          {deleteLabel}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
