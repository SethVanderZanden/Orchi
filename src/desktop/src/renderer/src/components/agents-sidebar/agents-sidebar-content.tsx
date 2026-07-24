import { useCallback, useMemo, useState } from 'react'

import { AgentsSidebarProjectGroup } from '@/components/agents-sidebar/agents-sidebar-project-group'
import { AgentsSidebarSection } from '@/components/agents-sidebar/agents-sidebar-section'
import { BoardFiltersBar } from '@/components/kanban/board-filters-bar'
import { ScrollArea } from '@/components/ui/scroll-area'
import { useBoardFilters } from '@/hooks/use-board-filters'
import { useBoardGrouping } from '@/hooks/use-board-grouping'
import { useKanbanBoardSync } from '@/hooks/use-kanban-board-sync'
import { groupBoardChats } from '@/lib/agents-sidebar/group-board-chats'
import { mapChatStatusToVariant } from '@/lib/chat/chat-status-variant'
import type { ChatStatus, ChatThread } from '@/lib/chat/types'
import { filterBoardChats } from '@/lib/kanban/board-filters'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

export function AgentsSidebarContent(): React.JSX.Element {
  const { chats, isLoadingChats, isChatSending, isParentKickingOffAny } = useChat()
  const { openChat, openChatInSplit, activeTabId, splitTabId } = useChatTabs()
  const { projects } = useProjects()
  const { filters, setProjectFilter, setDateRange } = useBoardFilters()
  const { grouping } = useBoardGrouping()
  // Track collapses so new projects default to expanded without an effect.
  const [collapsedProjectIds, setCollapsedProjectIds] = useState<Set<string>>(() => new Set())

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

  const grouped = useMemo(
    () =>
      groupBoardChats(filteredChats, {
        grouping,
        projects,
        resolveStatus: resolveBoardStatus
      }),
    [filteredChats, grouping, projects, resolveBoardStatus]
  )

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

  function isChatActive(chatId: string): boolean {
    return chatId === activeTabId || chatId === splitTabId
  }

  function isProjectExpanded(projectId: string): boolean {
    return !collapsedProjectIds.has(projectId)
  }

  function toggleProjectExpanded(projectId: string): void {
    setCollapsedProjectIds((current) => {
      const next = new Set(current)
      if (next.has(projectId)) {
        next.delete(projectId)
      } else {
        next.add(projectId)
      }
      return next
    })
  }

  return (
    <div className="flex min-h-0 min-w-0 flex-1 flex-col">
      <div className="shrink-0 space-y-2 border-b border-border/60 px-3 py-3">
        <div className="flex items-center justify-between gap-2">
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-sidebar-foreground">Agents</p>
            <p className="truncate text-xs text-sidebar-muted">Live status list</p>
          </div>
        </div>
        <BoardFiltersBar
          projectFilter={filters.projectFilter}
          dateRange={filters.dateRange}
          projects={projects}
          onProjectFilterChange={setProjectFilter}
          onDateRangeChange={setDateRange}
          className="flex-wrap"
        />
      </div>

      {isLoadingChats && chats.length === 0 ? (
        <p className="px-3 py-6 text-sm text-sidebar-muted">Loading chats…</p>
      ) : hasFilteredOutChats ? (
        <p className="px-3 py-6 text-sm text-sidebar-muted">
          No chats match the current filters. Try a wider date range or another project.
        </p>
      ) : grouped.mode === 'project' && grouped.projects.length === 0 ? (
        <p className="px-3 py-6 text-sm text-sidebar-muted">
          No chats to show. Open a chat or widen the filters.
        </p>
      ) : (
        <ScrollArea className="min-h-0 flex-1">
          <div className="min-w-0 space-y-1 px-1.5 pb-3">
            {grouped.mode === 'state'
              ? grouped.sections.map((section) => (
                  <AgentsSidebarSection
                    key={section.id}
                    title={section.title}
                    chats={section.chats}
                    showProjectName
                    getStatusVariant={(chat) => mapChatStatusToVariant(resolveBoardStatus(chat))}
                    getProjectName={(chat) => getProjectName(chat.projectId)}
                    getParentTitle={getParentTitle}
                    isChatActive={isChatActive}
                    onOpenChat={openChat}
                    onOpenChatBeside={openChatInSplit}
                  />
                ))
              : grouped.projects.map((project) => (
                  <AgentsSidebarProjectGroup
                    key={project.id}
                    group={project}
                    isExpanded={isProjectExpanded(project.id)}
                    onToggle={() => toggleProjectExpanded(project.id)}
                    getStatusVariant={(chat) => mapChatStatusToVariant(resolveBoardStatus(chat))}
                    getParentTitle={getParentTitle}
                    isChatActive={isChatActive}
                    onOpenChat={openChat}
                    onOpenChatBeside={openChatInSplit}
                  />
                ))}
          </div>
        </ScrollArea>
      )}
    </div>
  )
}
