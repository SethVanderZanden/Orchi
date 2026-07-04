import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'
import {
  Bot,
  ChevronDown,
  ChevronRight,
  Folder,
  FolderPlus,
  MessageSquarePlus,
  Settings,
  Shield,
  Trash2
} from 'lucide-react'

import { DeleteChatDialog } from '@/components/chat/delete-chat-dialog'
import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { SidebarHeader } from '@/components/layout/sidebar/sidebar-header'
import { SidebarNavItem } from '@/components/layout/sidebar/sidebar-nav-item'
import { SidebarSearch } from '@/components/layout/sidebar/sidebar-search'
import { SidebarSectionHeader } from '@/components/layout/sidebar/sidebar-section-header'
import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { RelativeTime } from '@/components/relative-time'
import { Button } from '@/components/ui/button'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { useDeleteChat } from '@/hooks/use-delete-chat'
import { useKeyboardShortcut } from '@/hooks/use-keyboard-shortcut'
import type { ChatThread } from '@/lib/chat/types'
import {
  filterProjectGroups,
  findProjectGroupForChat,
  groupChatsByProject,
  resolveWorkspaceIdForNewChat,
  type ProjectChatGroup,
  type WorkspaceChatSubGroup
} from '@/lib/projects/group-chats'
import { isReviewChildChat, type ChatTreeNode } from '@/lib/workspaces/chat-tree'
import { cn } from '@/lib/utils'
import { useChat } from '@/providers/chat-provider'
import { useProjects } from '@/providers/project-provider'
import { useWorkspaceLayout } from '@/providers/workspace-layout-provider'

function sidebarIconClass(isActive: boolean): string {
  return cn(
    'shrink-0 transition-colors duration-150 ease-out',
    isActive
      ? 'text-sidebar-accent-foreground'
      : 'text-sidebar-muted group-hover:text-sidebar-accent-foreground'
  )
}

export function WorkspaceNavigator(): React.JSX.Element {
  const navigate = useNavigate()
  const {
    chats,
    searchQuery,
    setSearchQuery,
    createChat,
    isPendingChats,
    isFetchingChats,
    chatsError,
    refetchChats,
    getChat,
    isChatSending,
    getChatSidebarStatus
  } = useChat()
  const { requestDelete, isDeleting, dialogProps } = useDeleteChat()
  const { projects, addProject, pickDirectory, isPendingProjects, projectsError, refetchProjects } =
    useProjects()
  const { isProjectExpanded, toggleProjectExpanded, ensureProjectExpanded } = useWorkspaceLayout()
  const [isCreating, setIsCreating] = useState(false)
  const [isAddingProject, setIsAddingProject] = useState(false)
  const [expandedWorkspaceIds, setExpandedWorkspaceIds] = useState<Set<string>>(() => new Set())
  const [expandedParentChatIds, setExpandedParentChatIds] = useState<Set<string>>(() => new Set())
  const priorChildCountsRef = useRef<Map<string, number>>(new Map())

  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params.chatId ?? null
  const activeChat = activeChatId ? getChat(activeChatId) : null

  const settingsMatch = useMatch({
    from: '/_app/settings',
    shouldThrow: false
  })

  const projectGroups = useMemo(() => {
    const groups = groupChatsByProject(projects, chats)
    return filterProjectGroups(groups, searchQuery)
  }, [projects, chats, searchQuery])

  const ensureWorkspaceExpanded = useCallback((workspaceId: string) => {
    setExpandedWorkspaceIds((current) => {
      if (current.has(workspaceId)) {
        return current
      }

      const next = new Set(current)
      next.add(workspaceId)
      return next
    })
  }, [])

  const toggleWorkspaceExpanded = useCallback((workspaceId: string) => {
    setExpandedWorkspaceIds((current) => {
      const next = new Set(current)
      if (next.has(workspaceId)) {
        next.delete(workspaceId)
      } else {
        next.add(workspaceId)
      }

      return next
    })
  }, [])

  const ensureParentExpanded = useCallback((parentChatId: string) => {
    setExpandedParentChatIds((current) => {
      if (current.has(parentChatId)) {
        return current
      }

      const next = new Set(current)
      next.add(parentChatId)
      return next
    })
  }, [])

  const toggleParentExpanded = useCallback((parentChatId: string) => {
    setExpandedParentChatIds((current) => {
      const next = new Set(current)
      if (next.has(parentChatId)) {
        next.delete(parentChatId)
      } else {
        next.add(parentChatId)
      }

      return next
    })
  }, [])

  useEffect(() => {
    if (!activeChat) {
      return
    }

    const matchingGroup = findProjectGroupForChat(projectGroups, activeChat)
    if (matchingGroup) {
      ensureProjectExpanded(matchingGroup.id)
    }

    if (activeChat.workspaceId) {
      ensureWorkspaceExpanded(activeChat.workspaceId)
    }

    if (activeChat.parentChatId) {
      ensureParentExpanded(activeChat.parentChatId)
    }
  }, [
    activeChat,
    ensureParentExpanded,
    ensureProjectExpanded,
    ensureWorkspaceExpanded,
    projectGroups
  ])

  useEffect(() => {
    for (const group of projectGroups) {
      const nodes = group.isFlat
        ? group.chatNodes
        : group.workspaceGroups.flatMap((workspaceGroup) => workspaceGroup.chatNodes)

      for (const node of nodes) {
        if (node.children.length === 0) {
          continue
        }

        const priorCount = priorChildCountsRef.current.get(node.chat.id) ?? 0
        if (node.children.length > priorCount) {
          ensureParentExpanded(node.chat.id)
        }
        priorChildCountsRef.current.set(node.chat.id, node.children.length)
      }
    }
  }, [ensureParentExpanded, projectGroups])

  useEffect(() => {
    if (projectGroups.length === 0) {
      return
    }

    const hasExpandedProject = projectGroups.some((group) => isProjectExpanded(group.id))
    if (!hasExpandedProject) {
      ensureProjectExpanded(projectGroups[0]!.id)
    }
  }, [ensureProjectExpanded, isProjectExpanded, projectGroups])

  const defaultProjectGroup = useMemo(() => {
    if (activeChat) {
      const activeGroup = findProjectGroupForChat(projectGroups, activeChat)
      if (activeGroup && !activeGroup.isOrphan) {
        return activeGroup
      }
    }

    return projectGroups.find((group) => !group.isOrphan) ?? null
  }, [activeChat, projectGroups])

  const createChatInGroup = useCallback(
    async (group: ProjectChatGroup, workspaceSubGroupId?: string) => {
      if (group.isOrphan || isCreating) {
        return
      }

      const workspaceId = resolveWorkspaceIdForNewChat(group, workspaceSubGroupId)
      if (!workspaceId) {
        return
      }

      setIsCreating(true)
      try {
        await createChat({ workspaceId, projectId: group.id })
      } finally {
        setIsCreating(false)
      }
    },
    [createChat, isCreating]
  )

  const createDefaultChat = useCallback(() => {
    if (!defaultProjectGroup) {
      return
    }

    void createChatInGroup(defaultProjectGroup)
  }, [createChatInGroup, defaultProjectGroup])

  useKeyboardShortcut('n', createDefaultChat, {
    enabled: Boolean(defaultProjectGroup) && !isCreating
  })

  const isInitialChatLoad = isPendingChats || (isFetchingChats && chats.length === 0)
  const isInitialProjectLoad = isPendingProjects && projects.length === 0

  async function handleAddProject(): Promise<void> {
    setIsAddingProject(true)
    try {
      const path = await pickDirectory()
      if (path) {
        const project = await addProject(path)
        if (project) {
          ensureProjectExpanded(project.id)
        }
      }
    } finally {
      setIsAddingProject(false)
    }
  }

  function handleRegisterOrphanPath(path: string): void {
    void addProject(path)
  }

  function renderChatRow(chat: ChatThread, isChild = false): React.JSX.Element {
    const isReview = isChild && isReviewChildChat(chat)
    const statusVariant = getChatSidebarStatus(chat)
    const isActive = chat.id === activeChatId

    return (
      <div key={chat.id} className="group relative min-w-0">
        <SidebarNavItem
          type="button"
          title={chat.title}
          size={isChild ? 'compact' : 'default'}
          isActive={isActive}
          leading={
            <>
              <ChatStatusDot variant={statusVariant} />
              {isChild ? (
                isReview ? (
                  <Shield className={cn('size-3', sidebarIconClass(isActive))} aria-hidden />
                ) : (
                  <Bot className={cn('size-3', sidebarIconClass(isActive))} aria-hidden />
                )
              ) : null}
            </>
          }
          trailing={
            <RelativeTime
              value={chat.updatedAt}
              compact
              className={cn(
                'shrink-0 tabular-nums text-sidebar-muted transition-colors duration-150 group-hover:text-sidebar-accent-foreground',
                isActive && 'text-sidebar-accent-foreground',
                isChild ? 'text-[10px]' : 'text-[11px]'
              )}
            />
          }
          onClick={() =>
            navigate({
              to: '/chat/$chatId',
              params: { chatId: chat.id }
            })
          }
        >
          {chat.title}
        </SidebarNavItem>
        <Button
          variant="ghost"
          size="icon"
          className="pointer-events-none absolute top-1/2 right-0.5 z-10 size-6 -translate-y-1/2 bg-sidebar/90 text-sidebar-muted opacity-0 backdrop-blur-sm transition-opacity duration-150 ease-out group-hover:pointer-events-auto group-hover:text-sidebar-accent-foreground group-hover:opacity-100 focus-visible:pointer-events-auto focus-visible:opacity-100"
          aria-label={`Delete ${chat.title}`}
          disabled={isChatSending(chat.id) || isDeleting}
          onClick={(event) => {
            event.stopPropagation()
            requestDelete(chat)
          }}
        >
          <Trash2 className={isChild ? 'size-3' : 'size-3.5'} />
        </Button>
      </div>
    )
  }

  function renderChatNode(node: ChatTreeNode): React.JSX.Element {
    const hasChildren = node.children.length > 0
    const isParentExpanded = expandedParentChatIds.has(node.chat.id)

    if (!hasChildren) {
      return renderChatRow(node.chat)
    }

    return (
      <div key={node.chat.id} className="min-w-0">
        <div className="flex min-w-0 items-center gap-0.5">
          <button
            type="button"
            className="flex size-6 shrink-0 items-center justify-center rounded-md text-sidebar-muted transition-colors duration-150 ease-out hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
            aria-label={isParentExpanded ? 'Collapse child agents' : 'Expand child agents'}
            onClick={() => toggleParentExpanded(node.chat.id)}
          >
            {isParentExpanded ? (
              <ChevronDown className="size-3" />
            ) : (
              <ChevronRight className="size-3" />
            )}
          </button>
          <div className="min-w-0 flex-1">{renderChatRow(node.chat)}</div>
        </div>

        {isParentExpanded ? (
          <div className="ml-3 space-y-0.5 pl-4">
            {node.children.map((child) => renderChatRow(child, true))}
          </div>
        ) : null}
      </div>
    )
  }

  function renderWorkspaceSubGroup(workspaceGroup: WorkspaceChatSubGroup): React.JSX.Element {
    const expanded = expandedWorkspaceIds.has(workspaceGroup.id)

    return (
      <div key={workspaceGroup.id} className="min-w-0">
        <SidebarNavItem
          type="button"
          size="compact"
          title={workspaceGroup.path}
          leading={
            <>
              {expanded ? (
                <ChevronDown className={cn('size-3', sidebarIconClass(false))} />
              ) : (
                <ChevronRight className={cn('size-3', sidebarIconClass(false))} />
              )}
              <Folder className={cn('size-3 fill-sidebar-muted/20', sidebarIconClass(false))} />
            </>
          }
          onClick={() => toggleWorkspaceExpanded(workspaceGroup.id)}
        >
          {workspaceGroup.name}
        </SidebarNavItem>

        {expanded ? (
          <div className="ml-3 space-y-0.5 pl-4">
            {workspaceGroup.chatNodes.length === 0 ? (
              <p className="px-2.5 py-1.5 text-xs text-sidebar-muted">No chats yet</p>
            ) : (
              workspaceGroup.chatNodes.map((node) => renderChatNode(node))
            )}
          </div>
        ) : null}
      </div>
    )
  }

  function renderProjectGroup(group: ProjectChatGroup): React.JSX.Element {
    const expanded = isProjectExpanded(group.id)
    const firstChat = group.isFlat
      ? group.chatNodes[0]?.chat
      : group.workspaceGroups.flatMap((workspaceGroup) => workspaceGroup.chatNodes)[0]?.chat

    return (
      <div key={group.id} className="min-w-0">
        <div className="sticky top-0 z-10 flex items-center gap-0.5 bg-sidebar">
          <SidebarNavItem
            type="button"
            size="compact"
            className="font-medium"
            title={group.name}
            leading={
              <>
                {expanded ? (
                  <ChevronDown className={cn('size-3.5', sidebarIconClass(false))} />
                ) : (
                  <ChevronRight className={cn('size-3.5', sidebarIconClass(false))} />
                )}
                <Folder className={cn('size-3.5 fill-sidebar-muted/30', sidebarIconClass(false))} />
              </>
            }
            onClick={() => toggleProjectExpanded(group.id)}
          >
            {group.name}
          </SidebarNavItem>

          {!group.isOrphan ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="ghost"
                  size="icon"
                  className="size-7 shrink-0 text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
                  aria-label={`New chat in ${group.name}`}
                  disabled={isCreating}
                  onClick={() => void createChatInGroup(group)}
                >
                  <MessageSquarePlus className="size-3.5" />
                </Button>
              </TooltipTrigger>
              <TooltipContent>New chat</TooltipContent>
            </Tooltip>
          ) : null}

          {group.isOrphan && group.chatNodes.length > 0 ? (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 shrink-0 px-2 text-xs text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
              onClick={() => {
                const path = firstChat?.workspacePath
                if (path) {
                  handleRegisterOrphanPath(path)
                }
              }}
            >
              Add as project
            </Button>
          ) : null}
        </div>

        {expanded ? (
          <div className="ml-3 space-y-0.5 pl-4">
            {group.isFlat ? (
              group.chatNodes.length === 0 ? (
                <p className="px-2.5 py-1.5 text-xs text-sidebar-muted">No chats yet</p>
              ) : (
                group.chatNodes.map((node) => renderChatNode(node))
              )
            ) : (
              group.workspaceGroups.map((workspaceGroup) => renderWorkspaceSubGroup(workspaceGroup))
            )}
          </div>
        ) : null}
      </div>
    )
  }

  return (
    <TooltipProvider delayDuration={300}>
      <>
        <aside className="flex h-full w-[260px] shrink-0 flex-col bg-sidebar text-sidebar-foreground">
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
                    onClick={() => void handleAddProject()}
                    disabled={isAddingProject}
                  >
                    <FolderPlus className="size-4" />
                  </Button>
                </TooltipTrigger>
                <TooltipContent>Add project</TooltipContent>
              </Tooltip>
            }
          />

          <SidebarSearch value={searchQuery} onChange={setSearchQuery} />

          {isInitialProjectLoad || isInitialChatLoad ? (
            <p className="px-3 py-6 text-sm text-sidebar-muted">Loading…</p>
          ) : projectsError ? (
            <div className="space-y-2 px-3 py-6">
              <p className="text-sm text-destructive">
                {projectsError.message || 'Failed to load projects.'}
              </p>
              <Button
                variant="ghost"
                size="sm"
                className="text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
                onClick={() => void refetchProjects()}
              >
                Retry
              </Button>
            </div>
          ) : chatsError ? (
            <div className="space-y-2 px-3 py-6">
              <p className="text-sm text-destructive">
                {chatsError.message || 'Failed to load chats.'}
              </p>
              <Button
                variant="ghost"
                size="sm"
                className="text-sidebar-muted hover:bg-sidebar-accent hover:text-sidebar-accent-foreground"
                onClick={() => void refetchChats()}
              >
                Retry
              </Button>
            </div>
          ) : projects.length === 0 ? (
            <div className="space-y-3 px-3 py-6">
              <p className="text-sm text-sidebar-muted">
                Add a project folder to start chatting with agents in that workspace.
              </p>
              <SidebarNavItem
                type="button"
                leading={<FolderPlus className={cn('size-4', sidebarIconClass(false))} />}
                onClick={() => void handleAddProject()}
                disabled={isAddingProject}
              >
                Add project
              </SidebarNavItem>
            </div>
          ) : (
            <>
              <SidebarSectionHeader isFirst>Projects</SidebarSectionHeader>
              <ScrollArea className="min-h-0 min-w-0 flex-1">
                <div className="min-w-0 space-y-1 px-3 pb-3">
                  {projectGroups.map((group) => renderProjectGroup(group))}
                </div>
              </ScrollArea>
            </>
          )}

          {projects.length > 0 ? (
            <div className="shrink-0 space-y-0.5 px-3 pb-3 pt-2">
              <SidebarNavItem
                type="button"
                isActive={Boolean(settingsMatch)}
                aria-current={settingsMatch ? 'page' : undefined}
                leading={
                  <Settings className={cn('size-4', sidebarIconClass(Boolean(settingsMatch)))} />
                }
                onClick={() => navigate({ to: '/settings' })}
              >
                Settings
              </SidebarNavItem>
              <SidebarNavItem
                type="button"
                leading={<FolderPlus className={cn('size-4', sidebarIconClass(false))} />}
                onClick={() => void handleAddProject()}
                disabled={isAddingProject}
              >
                Add project
              </SidebarNavItem>
            </div>
          ) : null}
        </aside>
        <DeleteChatDialog {...dialogProps} />
      </>
    </TooltipProvider>
  )
}
