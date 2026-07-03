import { useEffect, useMemo, useState } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'
import {
  ChevronDown,
  ChevronRight,
  Folder,
  FolderPlus,
  MessageSquarePlus,
  Search,
  Settings
} from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { NewChatDialog, type NewChatOptions } from '@/components/chat/new-chat-dialog'
import { RelativeTime } from '@/components/relative-time'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { PageHeader } from '@/components/ui/page-header'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Separator } from '@/components/ui/separator'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { useChat } from '@/providers/chat-provider'
import { useWorkspaces } from '@/providers/workspace-provider'
import { useWorkspaceLayout } from '@/providers/workspace-layout-provider'
import {
  filterWorkspaceGroups,
  groupChatsByWorkspace,
  type WorkspaceChatGroup
} from '@/lib/workspaces/group-chats'

export function WorkspaceNavigator(): React.JSX.Element {
  const navigate = useNavigate()
  const { chats, searchQuery, setSearchQuery, createChat, isLoadingChats, getChat } = useChat()
  const { workspaces, addWorkspace, pickDirectory } = useWorkspaces()
  const { isProjectExpanded, toggleProjectExpanded, ensureProjectExpanded } = useWorkspaceLayout()
  const [newChatGroup, setNewChatGroup] = useState<WorkspaceChatGroup | null>(null)
  const [isCreating, setIsCreating] = useState(false)
  const [isAddingProject, setIsAddingProject] = useState(false)

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

  useEffect(() => {
    if (!activeChat) {
      return
    }

    const matchingGroup = workspaceGroups.find((group) =>
      group.chats.some((chat) => chat.id === activeChat.id)
    )
    if (matchingGroup) {
      ensureProjectExpanded(matchingGroup.id)
    }
  }, [activeChat, ensureProjectExpanded, workspaceGroups])

  useEffect(() => {
    if (workspaceGroups.length === 0) {
      return
    }

    const hasExpandedProject = workspaceGroups.some((group) => isProjectExpanded(group.id))
    if (!hasExpandedProject) {
      ensureProjectExpanded(workspaceGroups[0].id)
    }
  }, [ensureProjectExpanded, isProjectExpanded, workspaceGroups])

  async function handleCreateChat(options: NewChatOptions): Promise<void> {
    setIsCreating(true)
    try {
      await createChat(options)
    } finally {
      setIsCreating(false)
    }
  }

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

          <Separator />

          {isLoadingChats ? (
            <p className="px-3 py-4 text-sm text-muted-foreground">Loading chats…</p>
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
            <ScrollArea className="min-h-0 flex-1">
              <div className="space-y-0.5 p-1">
                {workspaceGroups.map((group) => {
                  const expanded = isProjectExpanded(group.id)

                  return (
                    <div key={group.id} className="min-w-0">
                      <div className="flex items-center gap-0.5">
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
                          <span className="truncate text-xs font-medium text-muted-foreground">
                            {group.name}
                          </span>
                        </button>

                        {group.isOrphan ? (
                          group.chats.length > 0 ? (
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-7 shrink-0 px-2 text-xs"
                              onClick={() => {
                                const path = group.chats[0]?.workspacePath
                                if (path) {
                                  handleRegisterOrphanPath(path)
                                }
                              }}
                            >
                              Add as project
                            </Button>
                          ) : null
                        ) : (
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="size-7 shrink-0"
                                aria-label={`New chat in ${group.name}`}
                                onClick={() => setNewChatGroup(group)}
                              >
                                <MessageSquarePlus className="size-3.5" />
                              </Button>
                            </TooltipTrigger>
                            <TooltipContent>New chat</TooltipContent>
                          </Tooltip>
                        )}
                      </div>

                      {expanded ? (
                        <div className="ml-5 space-y-0.5 border-l pl-1">
                          {group.chats.length === 0 ? (
                            <p className="px-2 py-1 text-xs text-muted-foreground">No chats yet</p>
                          ) : (
                            group.chats.map((chat) => (
                              <button
                                key={chat.id}
                                type="button"
                                className={cn(
                                  'flex w-full items-center justify-between gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-accent',
                                  chat.id === activeChatId && 'bg-accent font-medium'
                                )}
                                onClick={() =>
                                  navigate({
                                    to: '/chat/$chatId',
                                    params: { chatId: chat.id }
                                  })
                                }
                              >
                                <span className="truncate">{chat.title}</span>
                                <RelativeTime value={chat.updatedAt} className="shrink-0" />
                              </button>
                            ))
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

        {newChatGroup && !newChatGroup.isOrphan ? (
          <NewChatDialog
            open={Boolean(newChatGroup)}
            onOpenChange={(open) => {
              if (!open) {
                setNewChatGroup(null)
              }
            }}
            workspacePath={newChatGroup.path}
            workspaceName={newChatGroup.name}
            onCreateChat={handleCreateChat}
            isSubmitting={isCreating}
          />
        ) : null}
      </>
    </TooltipProvider>
  )
}
