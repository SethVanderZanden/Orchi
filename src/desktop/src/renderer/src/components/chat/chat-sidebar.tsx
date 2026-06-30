import { useMemo, useState } from 'react'
import { Link, useMatch } from '@tanstack/react-router'
import { formatDistanceToNow } from 'date-fns'
import { FolderPlusIcon, MessageSquarePlusIcon, SearchIcon, SettingsIcon } from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { NewChatDialog, type NewChatOptions } from '@/components/chat/new-chat-dialog'
import { useChat } from '@/providers/chat-provider'
import { useWorkspaces } from '@/providers/workspace-provider'
import { filterWorkspaceGroups, groupChatsByWorkspace } from '@/lib/workspaces/group-chats'
import { Button } from '@/components/ui/button'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupAction,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInput,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  SidebarSeparator
} from '@/components/ui/sidebar'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@/components/ui/tooltip'

type PendingNewChat = {
  workspacePath: string
  workspaceName: string
}

export function ChatSidebar(): React.JSX.Element {
  const { chats, searchQuery, setSearchQuery, createChat, isLoadingChats } = useChat()
  const { workspaces, addWorkspace, pickDirectory } = useWorkspaces()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)
  const [pendingNewChat, setPendingNewChat] = useState<PendingNewChat | null>(null)
  const [isAddingProject, setIsAddingProject] = useState(false)

  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params.chatId ?? null

  const settingsMatch = useMatch({
    from: '/_app/settings',
    shouldThrow: false
  })

  const workspaceGroups = useMemo(() => {
    const groups = groupChatsByWorkspace(workspaces, chats)
    return filterWorkspaceGroups(groups, searchQuery)
  }, [workspaces, chats, searchQuery])

  async function handleCreateChat(options: NewChatOptions): Promise<void> {
    setIsCreating(true)
    try {
      await createChat(options)
    } finally {
      setIsCreating(false)
    }
  }

  function openNewChatDialog(workspacePath: string, workspaceName: string): void {
    setPendingNewChat({ workspacePath, workspaceName })
    setDialogOpen(true)
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

  async function handleRegisterOrphanPath(path: string): Promise<void> {
    addWorkspace(path)
  }

  return (
    <TooltipProvider delayDuration={300}>
      <Sidebar collapsible="icon" className="border-r">
        <SidebarHeader className="gap-3 border-b">
          <div className="flex min-w-0 items-center gap-2 group-data-[collapsible=icon]:justify-center">
            <Avatar size="default" className="group-data-[collapsible=icon]:mx-auto">
              <AvatarFallback className="bg-muted/40">
                <OrchiAiIcon className="size-5" />
              </AvatarFallback>
            </Avatar>
            <div className="min-w-0 group-data-[collapsible=icon]:hidden">
              <p className="truncate text-sm font-semibold">Orchi</p>
              <p className="text-muted-foreground truncate text-xs">AI orchestrator</p>
            </div>
          </div>

          <div className="relative group-data-[collapsible=icon]:hidden">
            <SearchIcon className="text-muted-foreground pointer-events-none absolute top-1/2 left-2.5 size-4 -translate-y-1/2" />
            <SidebarInput
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search chats"
              className="pl-8"
            />
          </div>
        </SidebarHeader>

        <SidebarContent>
          {isLoadingChats ? (
            <p className="text-muted-foreground px-2 py-3 text-xs">Loading chats…</p>
          ) : workspaces.length === 0 ? (
            <SidebarGroup className="group-data-[collapsible=icon]:hidden">
              <SidebarGroupContent>
                <div className="flex flex-col gap-3 px-2 py-2">
                  <p className="text-muted-foreground text-xs leading-relaxed">
                    Add a project folder to start chatting with agents in that workspace.
                  </p>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleAddProject}
                    disabled={isAddingProject}
                  >
                    <FolderPlusIcon />
                    Add project
                  </Button>
                </div>
              </SidebarGroupContent>
            </SidebarGroup>
          ) : (
            workspaceGroups.map((group) => (
              <SidebarGroup key={group.id}>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <SidebarGroupLabel className="min-w-0 truncate group-data-[collapsible=icon]:hidden">
                      {group.name}
                    </SidebarGroupLabel>
                  </TooltipTrigger>
                  {group.path ? (
                    <TooltipContent side="right" className="max-w-xs">
                      {group.path}
                    </TooltipContent>
                  ) : null}
                </Tooltip>

                {group.isOrphan ? (
                  group.chats.length > 0 ? (
                    <SidebarGroupAction
                      aria-label="Add as project"
                      onClick={() => {
                        const path = group.chats[0]?.workspacePath
                        if (path) {
                          void handleRegisterOrphanPath(path)
                        }
                      }}
                    >
                      <FolderPlusIcon />
                    </SidebarGroupAction>
                  ) : null
                ) : (
                  <SidebarGroupAction
                    aria-label={`New chat in ${group.name}`}
                    onClick={() => openNewChatDialog(group.path, group.name)}
                  >
                    <MessageSquarePlusIcon />
                  </SidebarGroupAction>
                )}

                <SidebarGroupContent className="group-data-[collapsible=icon]:hidden">
                  <SidebarMenu>
                    {group.chats.length === 0 ? (
                      <p className="text-muted-foreground px-2 py-2 text-xs">No chats yet</p>
                    ) : (
                      group.chats.map((chat) => (
                        <SidebarMenuItem key={chat.id}>
                          <SidebarMenuButton
                            asChild
                            isActive={chat.id === activeChatId}
                            className="h-auto items-start py-2.5"
                          >
                            <Link to="/chat/$chatId" params={{ chatId: chat.id }}>
                              <div className="flex min-w-0 flex-1 flex-col gap-1 text-left">
                                <div className="flex items-center justify-between gap-2">
                                  <span className="truncate font-medium">{chat.title}</span>
                                  <span className="text-muted-foreground shrink-0 text-[10px]">
                                    {formatDistanceToNow(new Date(chat.updatedAt), {
                                      addSuffix: true
                                    })}
                                  </span>
                                </div>
                                <span className="text-muted-foreground line-clamp-2 text-xs leading-relaxed">
                                  {chat.preview}
                                </span>
                              </div>
                            </Link>
                          </SidebarMenuButton>
                        </SidebarMenuItem>
                      ))
                    )}
                  </SidebarMenu>
                </SidebarGroupContent>
              </SidebarGroup>
            ))
          )}

          {workspaces.length > 0 ? (
            <SidebarGroup className="group-data-[collapsible=icon]:hidden">
              <SidebarGroupContent>
                <Button
                  variant="outline"
                  size="sm"
                  className="w-full"
                  onClick={handleAddProject}
                  disabled={isAddingProject}
                >
                  <FolderPlusIcon />
                  Add project
                </Button>
              </SidebarGroupContent>
            </SidebarGroup>
          ) : null}

          <SidebarSeparator className="group-data-[collapsible=icon]:hidden" />

          <SidebarGroup className="group-data-[collapsible=icon]:hidden">
            <SidebarGroupLabel>App</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild isActive={Boolean(settingsMatch)} tooltip="Settings">
                    <Link to="/settings">
                      <SettingsIcon />
                      <span>Settings</span>
                    </Link>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        </SidebarContent>

        <SidebarFooter className="border-t">
          <div className="flex items-center gap-2 rounded-lg group-data-[collapsible=icon]:justify-center">
            <Avatar size="sm">
              <AvatarFallback className="text-xs">S</AvatarFallback>
            </Avatar>
          </div>
        </SidebarFooter>

        <SidebarRail />
      </Sidebar>

      {pendingNewChat ? (
        <NewChatDialog
          open={dialogOpen}
          onOpenChange={setDialogOpen}
          workspacePath={pendingNewChat.workspacePath}
          workspaceName={pendingNewChat.workspaceName}
          onCreateChat={handleCreateChat}
          isSubmitting={isCreating}
        />
      ) : null}
    </TooltipProvider>
  )
}
