import { useCallback, useMemo } from 'react'

import { BoardFiltersBar } from '@/components/kanban/board-filters-bar'
import { KanbanColumn } from '@/components/kanban/kanban-column'
import { PageHeader } from '@/components/ui/page-header'
import { useBoardFilters } from '@/hooks/use-board-filters'
import { useKanbanBoardSync } from '@/hooks/use-kanban-board-sync'
import { mapChatStatusToVariant } from '@/lib/chat/chat-status-variant'
import type { ChatStatus, ChatThread } from '@/lib/chat/types'
import { filterBoardChats } from '@/lib/kanban/board-filters'
import { groupChatsByStatus } from '@/lib/kanban/group-chats-by-status'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

export function AgentKanbanBoard(): React.JSX.Element {
  const { chats, isLoadingChats, isChatSending, isParentKickingOffAny } = useChat()
  const { openChat, openChatInSplit } = useChatTabs()
  const { projects } = useProjects()
  const { filters, setProjectFilter, setDateRange } = useBoardFilters()

  useKanbanBoardSync()

  const filteredChats = useMemo(() => filterBoardChats(chats, filters), [chats, filters])

  const resolveBoardStatus = useCallback(
    (chat: ChatThread): ChatStatus => {
      if (isChatSending(chat.id) || isParentKickingOffAny(chat.id)) {
        return 'inProgress'
      }

      return chat.status
    },
    [isChatSending, isParentKickingOffAny]
  )

  const columns = groupChatsByStatus(filteredChats, { resolveStatus: resolveBoardStatus })
  const hasFilteredOutChats = !isLoadingChats && chats.length > 0 && filteredChats.length === 0

  function getProjectName(projectId: string | null): string | null {
    if (!projectId) {
      return null
    }
    return projects.find((project) => project.id === projectId)?.name ?? null
  }

  function getParentTitle(chat: ChatThread): string | null {
    if (!chat.parentChatId) {
      return null
    }

    return chats.find((candidate) => candidate.id === chat.parentChatId)?.title ?? 'Parent chat'
  }

  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title="Agents"
        description="Live board — cards move as status updates arrive"
        endContent={
          <BoardFiltersBar
            projectFilter={filters.projectFilter}
            dateRange={filters.dateRange}
            projects={projects}
            onProjectFilterChange={setProjectFilter}
            onDateRangeChange={setDateRange}
          />
        }
      />
      <div className="flex min-h-0 flex-1 gap-4 overflow-x-auto p-4">
        {isLoadingChats && chats.length === 0 ? (
          <p className="m-auto text-sm text-muted-foreground">Loading chats…</p>
        ) : hasFilteredOutChats ? (
          <p className="m-auto text-sm text-muted-foreground">
            No chats match the current filters. Try a wider date range or another project.
          </p>
        ) : (
          columns.map((column) => (
            <KanbanColumn
              key={column.id}
              title={column.title}
              chats={column.chats}
              getStatusVariant={(chat) => mapChatStatusToVariant(resolveBoardStatus(chat))}
              getProjectName={(chat) => getProjectName(chat.projectId)}
              getParentTitle={getParentTitle}
              onOpenChat={openChat}
              onOpenChatBeside={openChatInSplit}
              className="min-w-[220px] max-w-md basis-0"
            />
          ))
        )}
      </div>
    </div>
  )
}
