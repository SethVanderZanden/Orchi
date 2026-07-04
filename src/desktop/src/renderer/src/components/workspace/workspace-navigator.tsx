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
import { cn } from '@/lib/utils'
import {
  filterWorkspaceGroups,
  groupChatsByWorkspace,
  type WorkspaceChatGroup
} from '@/lib/workspaces/group-chats'
import { isReviewChildChat, type ChatTreeNode } from '@/lib/workspaces/chat-tree'
import { useChat } from '@/providers/chat-provider'
import { useWorkspaces } from '@/providers/workspace-provider'
import { useWorkspaceLayout } from '@/providers/workspace-layout-provider'

function groupContainsChat(group: WorkspaceChatGroup, chatId: string): boolean {
  return group.chatNodes.some(
    (node) => node.chat.id === chatId || node.children.some((child) => child.id === chatId)
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
    getChat
  } = useChat()
  const { workspaces, addWorkspace, pickDirectory } = useWorkspaces()
  const { isProjectExpanded, toggleProjectExpanded, ensureProjectExpanded } = useWorkspaceLayout()
  const [isCreating, setIsCreating] = useState(false)
  const [isAddingProject, setIsAddingProject] = useState(false)
  const [expandedParentChatIds, setExpandedParentChatIds] = useState<Set<string>>(() => new Set())
  const priorChildCountsRef = useRef<Map<string, number>>(new Map())

  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params.chatId ?? null
  const activeChat = activeChatId ? getChat(activeChatId) : undefined

  const settingsMatch = useMatch({
    from: '/_app/settings',
    shouldThrow: false
  })

  const workspaceGroups = useMemo(() => {
    const groups = groupChatsByWorkspace(workspaces, chats)
    return filterWorkspaceGroups(groups, searchQuery)
  }, [workspaces, chats, searchQuery])

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

    const matchingGroup = workspaceGroups.find((group) => groupContainsChat(group, activeChat.id))
    if (matchingGroup) {
      ensureProjectExpanded(matchingGroup.id)
    }

    if (activeChat.parentChatId) {
      ensureParentExpanded(activeChat.parentChatId)
    }
  }, [activeChat, ensureParentExpanded, ensureProjectExpanded, workspaceGroups])

  useEffect(() => {
    for (const group of workspaceGroups) {
      for (const node of group.chatNodes) {
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
  }, [ensureParentExpanded, workspaceGroups])

  useEffect(() => {
    if (workspaceGroups.length === 0) {
      return
    }

    const hasExpandedProject = workspaceGroups.some((group) => isProjectExpanded(group.id))
    if (!hasExpandedProject) {
      ensureProjectExpanded(workspaceGroups[0].id)
    }
  }, [ensureProjectExpanded, isProjectExpanded, workspaceGroups])

  const defaultWorkspaceGroup = useMemo(() => {
    if (activeChat?.workspacePath) {
      const activeGroup = workspaceGroups.find(
        (group) => !group.isOrphan && group.path === activeChat.workspacePath
      )
      if (activeGroup) {
        return activeGroup
      }
    }

    return workspaceGroups.find((group) => !group.isOrphan) ?? null
  }, [activeChat?.workspacePath, workspaceGroups])

  const createChatInGroup = useCallback(
    async (group: WorkspaceChatGroup) => {
      if (group.isOrphan || isCreating) {
        return
      }

      setIsCreating(true)
      try {
        await createChat({ workspacePath: group.path })
      } finally {
        setIsCreating(false)
      }
    },
    [createChat, isCreating]
  )

  const createDefaultChat = useCallback(() => {
    if (!defaultWorkspaceGroup) {
      return
    }

    void createChatInGroup(defaultWorkspaceGroup)
  }, [createChatInGroup, defaultWorkspaceGroup])

  useKeyboardShortcut('n', createDefaultChat, { enabled: Boolean(defaultWorkspaceGroup) && !isCreating })

  const isInitialChatLoad = isPendingChats || (isFetchingChats && chats.length === 0)

  async function handleAddProject(): Promise<void> {
    setIsAddingProject(true)
    try {
      const path = await pickDirectory()
      if (path) {
        addWorkspace(path)
      }
    } finally {
      setIsAddingProject(false)
    }
  }

  function handleRegisterOrphanPath(path: string): void {
    addWorkspace(path)
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

          {workspaces.length > 0 ? (
            <Tooltip>
              <TooltipTrigger asChild>
                <Button
                  variant="secondary"
                  size="sm"
                  className="mx-2 mb-2 w-[calc(100%-1rem)]"
                  disabled={!defaultWorkspaceGroup || isCreating}
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

          {isInitialChatLoad ? (
            <p className="px-3 py-4 text-sm text-muted-foreground">Loading chats…</p>
          ) : chatsError ? (
            <div className="space-y-2 px-3 py-4">
              <p className="text-sm text-destructive">
                {chatsError.message || 'Failed to load chats.'}
              </p>
              <Button variant="secondary" size="sm" onClick={() => void refetchChats()}>
                Retry
              </Button>
            </div>
          ) : workspaces.length === 0 ? (
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
                {workspaceGroups.map((group) => {
                  const expanded = isProjectExpanded(group.id)
                  const firstChat = group.chatNodes[0]?.chat

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
                          {!group.isOrphan ? (
                            <button
                              type="button"
                              className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm text-muted-foreground hover:bg-accent"
                              disabled={isCreating}
                              onClick={() => void createChatInGroup(group)}
                            >
                              <MessageSquarePlus className="size-3.5 shrink-0" />
                              <span>New chat</span>
                            </button>
                          ) : null}
                          {group.chatNodes.length === 0 ? (
                            <p className="px-2 py-1 text-xs text-muted-foreground">No chats yet</p>
                          ) : (
                            group.chatNodes.map((node) => renderChatNode(node))
                          )}
                        </div>
                      ) : null}
                    </div>
                  )
                })}
              </div>
            </ScrollArea>
          )}

          {workspaces.length > 0 ? (
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
