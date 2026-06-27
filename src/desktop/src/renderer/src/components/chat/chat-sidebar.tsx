import { useState } from 'react'
import { Link, useMatch } from '@tanstack/react-router'
import { formatDistanceToNow } from 'date-fns'
import { MessageSquarePlusIcon, SearchIcon, SettingsIcon } from 'lucide-react'

import { OrchiAiIcon } from '@/components/brand/orchi-ai-icon'
import { NewChatDialog } from '@/components/chat/new-chat-dialog'
import { useChat } from '@/providers/chat-provider'
import { Button } from '@/components/ui/button'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
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

export function ChatSidebar(): React.JSX.Element {
  const {
    chats,
    searchQuery,
    setSearchQuery,
    createChat,
    isLoadingChats
  } = useChat()
  const [dialogOpen, setDialogOpen] = useState(false)
  const [isCreating, setIsCreating] = useState(false)

  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const activeChatId = chatMatch?.params.chatId ?? null

  const settingsMatch = useMatch({
    from: '/_app/settings',
    shouldThrow: false
  })

  const filteredChats = chats.filter((chat) => {
    const query = searchQuery.trim().toLowerCase()
    if (!query) {
      return true
    }

    return (
      chat.title.toLowerCase().includes(query) || chat.preview.toLowerCase().includes(query)
    )
  })

  async function handleCreateChat(workspacePath: string): Promise<void> {
    setIsCreating(true)
    try {
      await createChat(workspacePath)
    } finally {
      setIsCreating(false)
    }
  }

  return (
    <>
      <Sidebar collapsible="icon" className="border-r">
        <SidebarHeader className="gap-3 border-b p-3">
          <div className="flex items-center justify-between gap-2 group-data-[collapsible=icon]:justify-center">
            <div className="flex min-w-0 items-center gap-2 group-data-[collapsible=icon]:hidden">
              <Avatar size="default">
                <AvatarFallback className="bg-muted/40">
                  <OrchiAiIcon className="size-5" />
                </AvatarFallback>
              </Avatar>
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold">Orchi</p>
                <p className="text-muted-foreground truncate text-xs">AI orchestrator</p>
              </div>
            </div>
            <Button
              size="icon-sm"
              variant="outline"
              className="shrink-0"
              onClick={() => setDialogOpen(true)}
              aria-label="New chat"
            >
              <MessageSquarePlusIcon />
            </Button>
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
          <SidebarGroup>
            <SidebarGroupLabel>Chats</SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                {isLoadingChats ? (
                  <p className="text-muted-foreground px-2 py-3 text-xs">Loading chats…</p>
                ) : filteredChats.length === 0 ? (
                  <p className="text-muted-foreground px-2 py-3 text-xs">
                    No chats yet. Create one to get started.
                  </p>
                ) : (
                  filteredChats.map((chat) => (
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
                                {formatDistanceToNow(new Date(chat.updatedAt), { addSuffix: true })}
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

        <SidebarFooter className="border-t p-3">
          <div className="flex items-center gap-2 rounded-lg border px-2.5 py-2 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:border-transparent group-data-[collapsible=icon]:px-0">
            <Avatar size="sm">
              <AvatarFallback className="text-xs">S</AvatarFallback>
            </Avatar>
            <div className="min-w-0 group-data-[collapsible=icon]:hidden">
              <p className="truncate text-sm font-medium">Workspace</p>
              <p className="text-muted-foreground truncate text-xs">Local dev</p>
            </div>
          </div>
        </SidebarFooter>

        <SidebarRail />
      </Sidebar>

      <NewChatDialog
        open={dialogOpen}
        onOpenChange={setDialogOpen}
        onCreateChat={handleCreateChat}
        isSubmitting={isCreating}
      />
    </>
  )
}
