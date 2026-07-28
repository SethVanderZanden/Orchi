import { useCallback, useMemo, useState } from 'react'

import { AgentsSidebarProjectGroup } from '@/components/agents-sidebar/agents-sidebar-project-group'
import { AgentsSidebarSection } from '@/components/agents-sidebar/agents-sidebar-section'
import { BoardFiltersBar } from '@/components/kanban/board-filters-bar'
import { useBoardFilters } from '@/hooks/use-board-filters'
import { useBoardGrouping } from '@/hooks/use-board-grouping'
import { useDeleteChat } from '@/hooks/use-delete-chat'
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
  const { requestDelete, isDeletingChat } = useDeleteChat()
  const { projects } = useProjects()
  const { filters, setProjectFilter, setDateRange } = useBoardFilters()
  const { grouping } = useBoardGrouping()
  // Track collapses so new projects default to expanded without an effect.
  const [collapsedProjectIds, setCollapsedProjectIds] = useState<Set<string>>(() => new Set())

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

  const titleByChatId = useMemo(() => {
    const map = new Map<string, string>()
    for (const chat of chats) {
      map.set(chat.id, chat.title)
    }
    return map
  }, [chats])

  const projectNameById = useMemo(() => {
    const map = new Map<string, string>()
    for (const project of projects) {
      map.set(project.id, project.name)
    }
    return map
  }, [projects])

  const hasFilteredOutChats = !isLoadingChats && chats.length > 0 && filteredChats.length === 0

  function getProjectName(projectId: string | null): string | null {
    if (!projectId) {
      return null
    }
    return projectNameById.get(projectId) ?? null
  }

  function getParentTitle(chat: ChatThread): string | null {
    if (!chat.parentChatId) {
      return null
    }

    return titleByChatId.get(chat.parentChatId) ?? 'Parent chat'
  }

  function isChatActive(chatId: string): boolean {
    return chatId === activeTabId || chatId === splitTabId
  }

  function isDeleteDisabled(chatId: string): boolean {
    return isChatSending(chatId) || isDeletingChat(chatId)
  }

  function handleDeleteChat(chat: ChatThread): void {
    requestDelete(chat)
  }

  function isProjectExpanded(projectId: string): boolean {
    return !collapsedProjectIds.has(projectId)
  }

  function setProjectExpanded(projectId: string, expanded: boolean): void {
    setCollapsedProjectIds((current) => {
      const isCollapsed = current.has(projectId)
      if (expanded && !isCollapsed) {
        return current
      }
      if (!expanded && isCollapsed) {
        return current
      }

      const next = new Set(current)
      if (expanded) {
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
        <div className="min-h-0 flex-1 overflow-y-auto">
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
                    onDeleteChat={handleDeleteChat}
                    isDeleteDisabled={isDeleteDisabled}
                  />
                ))
              : grouped.projects.map((project) => (
                  <AgentsSidebarProjectGroup
                    key={project.id}
                    group={project}
                    isExpanded={isProjectExpanded(project.id)}
                    onExpandedChange={(expanded) => setProjectExpanded(project.id, expanded)}
                    getStatusVariant={(chat) => mapChatStatusToVariant(resolveBoardStatus(chat))}
                    getParentTitle={getParentTitle}
                    isChatActive={isChatActive}
                    onOpenChat={openChat}
                    onOpenChatBeside={openChatInSplit}
                    onDeleteChat={handleDeleteChat}
                    isDeleteDisabled={isDeleteDisabled}
                  />
                ))}
          </div>
        </div>
      )}
    </div>
  )
}
