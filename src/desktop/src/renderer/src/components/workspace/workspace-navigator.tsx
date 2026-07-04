import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'
import {
  Bot,
  ChevronDown,
  ChevronRight,
  Folder,
  FolderPlus,
  MessageSquarePlus,
  Search,
  Settings,
  Shield
} from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { RelativeTime } from '@/components/relative-time'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { PageHeader } from '@/components/ui/page-header'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
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
    getChat
  } = useChat()
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

    return (
      <button
        key={chat.id}
        type="button"
        title={chat.title}
        className={cn(
          'flex w-full min-w-0 items-center gap-2 overflow-hidden rounded-md px-2 py-1.5 text-left hover:bg-accent',
          isChild ? 'text-xs' : 'text-sm',
          chat.id === activeChatId && 'bg-accent font-medium'
        )}
        onClick={() =>
          navigate({
            to: '/chat/$chatId',
            params: { chatId: chat.id }
          })
        }
      >
        {isChild ? (
          isReview ? (
            <Shield className="size-3 shrink-0 text-muted-foreground" aria-hidden />
          ) : (
            <Bot className="size-3 shrink-0 text-muted-foreground" aria-hidden />
          )
        ) : null}
        <span className="min-w-0 flex-1 overflow-hidden">
          <span className="block truncate">{chat.title}</span>
        </span>
        <RelativeTime
          value={chat.updatedAt}
          className={cn('shrink-0 text-muted-foreground', isChild ? 'text-[10px]' : 'text-[11px]')}
        />
      </button>
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
            className="flex size-6 shrink-0 items-center justify-center rounded-md hover:bg-accent"
            aria-label={isParentExpanded ? 'Collapse child agents' : 'Expand child agents'}
            onClick={() => toggleParentExpanded(node.chat.id)}
          >
            {isParentExpanded ? (
              <ChevronDown className="size-3 text-muted-foreground" />
            ) : (
              <ChevronRight className="size-3 text-muted-foreground" />
            )}
          </button>
          <div className="min-w-0 flex-1">{renderChatRow(node.chat)}</div>
        </div>

        {isParentExpanded ? (
          <div className="ml-5 space-y-0.5 border-l pl-1">
            {node.children.map((child) => renderChatRow(child, true))}
          </div>
        ) : null}
      </div>
    )
  }

  function renderNewChatButton(
    group: ProjectChatGroup,
    workspaceSubGroupId?: string
  ): React.JSX.Element {
    return (
      <button
        type="button"
        className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm text-muted-foreground hover:bg-accent"
        disabled={isCreating}
        onClick={() => void createChatInGroup(group, workspaceSubGroupId)}
      >
        <MessageSquarePlus className="size-3.5 shrink-0" />
        <span>New chat</span>
      </button>
    )
  }

  function renderWorkspaceSubGroup(
    group: ProjectChatGroup,
    workspaceGroup: WorkspaceChatSubGroup
  ): React.JSX.Element {
    const expanded = expandedWorkspaceIds.has(workspaceGroup.id)

    return (
      <div key={workspaceGroup.id} className="min-w-0">
        <div className="flex items-center gap-0.5">
          <button
            type="button"
            className="flex min-w-0 flex-1 items-center gap-1.5 rounded-md px-2 py-1.5 text-left hover:bg-accent"
            onClick={() => toggleWorkspaceExpanded(workspaceGroup.id)}
          >
            {expanded ? (
              <ChevronDown className="size-3 shrink-0 text-muted-foreground" />
            ) : (
              <ChevronRight className="size-3 shrink-0 text-muted-foreground" />
            )}
            <Folder className="size-3 shrink-0 fill-muted-foreground/20 text-muted-foreground" />
            <span
              className="min-w-0 flex-1 truncate text-xs text-muted-foreground"
              title={workspaceGroup.path}
            >
              {workspaceGroup.name}
            </span>
          </button>
        </div>

        {expanded ? (
          <div className="ml-5 space-y-0.5 border-l pl-1">
            {renderNewChatButton(group, workspaceGroup.id)}
            {workspaceGroup.chatNodes.length === 0 ? (
              <p className="px-2 py-1 text-xs text-muted-foreground">No chats yet</p>
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
        <div className="sticky top-0 z-10 flex items-center gap-0.5 bg-background">
          <button
            type="button"
            className="flex min-w-0 flex-1 items-center gap-1.5 rounded-md px-2 py-1.5 text-left hover:bg-accent"
            onClick={() => toggleProjectExpanded(group.id)}
          >
            {expanded ? (
              <ChevronDown className="size-3.5 shrink-0 text-muted-foreground" />
            ) : (
              <ChevronRight className="size-3.5 shrink-0 text-muted-foreground" />
            )}
            <Folder className="size-3.5 shrink-0 fill-muted-foreground/30 text-muted-foreground" />
            <span
              className="min-w-0 flex-1 truncate text-xs font-medium text-muted-foreground"
              title={group.name}
            >
              {group.name}
            </span>
          </button>

          {group.isOrphan && group.chatNodes.length > 0 ? (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 shrink-0 px-2 text-xs"
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
          <div className="ml-5 space-y-0.5 border-l pl-1">
            {group.isFlat ? (
              <>
                {!group.isOrphan ? renderNewChatButton(group) : null}
                {group.chatNodes.length === 0 ? (
                  <p className="px-2 py-1 text-xs text-muted-foreground">No chats yet</p>
                ) : (
                  group.chatNodes.map((node) => renderChatNode(node))
                )}
              </>
            ) : (
              group.workspaceGroups.map((workspaceGroup) =>
                renderWorkspaceSubGroup(group, workspaceGroup)
              )
            )}
          </div>
        ) : null}
      </div>
    )
  }

  return (
    <TooltipProvider delayDuration={300}>
      <>
        <aside className="flex h-full w-60 shrink-0 flex-col border-r bg-background">
          <PageHeader
            startContent={
              <div className="flex items-center gap-2">
                <OrchiAiIcon className="size-5" />
                <div>
                  <p className="text-sm font-semibold">Orchi</p>
                  <p className="text-xs text-muted-foreground">AI orchestrator</p>
                </div>
              </div>
            }
            endContent={
              <>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className="size-8"
                      aria-label="Add project"
                      onClick={() => void handleAddProject()}
                      disabled={isAddingProject}
                    >
                      <FolderPlus className="size-4" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Add project</TooltipContent>
                </Tooltip>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <Button
                      variant="ghost"
                      size="icon"
                      className={cn('size-8', settingsMatch && 'bg-accent')}
                      aria-label="Settings"
                      aria-current={settingsMatch ? 'page' : undefined}
                      onClick={() => navigate({ to: '/settings' })}
                    >
                      <Settings className="size-4" />
                    </Button>
                  </TooltipTrigger>
                  <TooltipContent>Settings</TooltipContent>
                </Tooltip>
              </>
            }
          />

          <div className="px-2 py-2">
            <div className="relative">
              <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                placeholder="Search chats"
                aria-label="Search chats"
                className="h-8 pl-8"
              />
            </div>
          </div>

          {projects.length > 0 ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="secondary"
                  size="sm"
                  className="mx-2 mb-2 w-[calc(100%-1rem)]"
                  disabled={!defaultProjectGroup || isCreating}
                  onClick={createDefaultChat}
                >
                  <MessageSquarePlus className="size-4" />
                  New chat
                </Button>
              </TooltipTrigger>
              <TooltipContent>Ctrl+N</TooltipContent>
            </Tooltip>
          ) : null}

          <Separator />

          {isInitialProjectLoad || isInitialChatLoad ? (
            <p className="px-3 py-4 text-sm text-muted-foreground">Loading…</p>
          ) : projectsError ? (
            <div className="space-y-2 px-3 py-4">
              <p className="text-sm text-destructive">
                {projectsError.message || 'Failed to load projects.'}
              </p>
              <Button variant="secondary" size="sm" onClick={() => void refetchProjects()}>
                Retry
              </Button>
            </div>
          ) : chatsError ? (
            <div className="space-y-2 px-3 py-4">
              <p className="text-sm text-destructive">
                {chatsError.message || 'Failed to load chats.'}
              </p>
              <Button variant="secondary" size="sm" onClick={() => void refetchChats()}>
                Retry
              </Button>
            </div>
          ) : projects.length === 0 ? (
            <div className="space-y-3 px-3 py-4">
              <p className="text-sm text-muted-foreground">
                Add a project folder to start chatting with agents in that workspace.
              </p>
              <Button
                variant="secondary"
                size="sm"
                className="w-full"
                onClick={() => void handleAddProject()}
                disabled={isAddingProject}
              >
                <FolderPlus className="size-4" />
                Add project
              </Button>
            </div>
          ) : (
            <ScrollArea className="min-h-0 min-w-0 flex-1">
              <div className="min-w-0 space-y-0.5 p-1">
                {projectGroups.map((group) => renderProjectGroup(group))}
              </div>
            </ScrollArea>
          )}

          {projects.length > 0 ? (
            <>
              <Separator />
              <div className="p-2">
                <Button
                  variant="secondary"
                  size="sm"
                  className="w-full"
                  onClick={() => void handleAddProject()}
                  disabled={isAddingProject}
                >
                  <FolderPlus className="size-4" />
                  Add project
                </Button>
              </div>
            </>
          ) : null}
        </aside>
      </>
    </TooltipProvider>
  )
}
