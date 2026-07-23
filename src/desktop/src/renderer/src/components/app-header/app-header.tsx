import { useState } from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'
import {
  ChevronDown,
  FolderPlus,
  GitBranch,
  MessageSquare,
  MoreHorizontal,
  Search,
  Settings
} from 'lucide-react'

import { ChatTabBar } from '@/components/app-header/chat-tab-bar'
import { ShortcutHint } from '@/components/app-header/shortcut-hint'
import { ChatCommandDialog } from '@/components/chat-finder/chat-command-dialog'
import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { Button } from '@/components/ui/button'
import { ButtonGroup } from '@/components/ui/button-group'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { useAppShortcuts } from '@/hooks/use-app-shortcuts'
import { requestOpenBranchReview } from '@/lib/branch-review/events'
import { getRecentChats } from '@/lib/chat-finder/build-chat-finder-groups'
import { cn } from '@/lib/utils'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

export function AppHeader(): React.JSX.Element {
  const navigate = useNavigate()
  const settingsMatch = useMatch({ from: '/_app/settings', shouldThrow: false })
  const isSettingsActive = Boolean(settingsMatch)
  const { activeTabId, createAndOpenTab, openChat, isCreatingTab, finderOpen, setFinderOpen } =
    useChatTabs()
  const { chats, getChat, getChatStatusVariant } = useChat()
  const { addProject, pickDirectory, isPendingProjects, projects } = useProjects()
  const [isAddingProject, setIsAddingProject] = useState(false)

  const recentChats = getRecentChats(chats)
  const activeChat = activeTabId ? getChat(activeTabId) : null
  const canReviewBranch = Boolean(activeChat?.projectId) || projects.length === 1

  useAppShortcuts()

  async function handleAddProject(): Promise<void> {
    if (isAddingProject || isPendingProjects) {
      return
    }

    setIsAddingProject(true)
    try {
      const path = await pickDirectory()
      if (!path) {
        return
      }
      await addProject(path)
    } finally {
      setIsAddingProject(false)
    }
  }

  return (
    <>
      <header className="flex h-11 shrink-0 items-center gap-1.5 border-b border-border bg-background px-2">
        <ChatTabBar />
        <div className="flex shrink-0 items-center gap-1">
          <Button
            type="button"
            variant="default"
            size="sm"
            className="h-8 gap-1.5 px-2.5 text-sm font-normal"
            aria-label="New chat"
            disabled={isCreatingTab}
            onClick={() => void createAndOpenTab()}
          >
            <MessageSquare className="size-3.5" />
            <span className="hidden sm:inline">New Chat</span>
            <ShortcutHint>Ctrl+N</ShortcutHint>
          </Button>
          <ButtonGroup aria-label="Find chat">
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 gap-1.5 px-2.5 text-sm font-normal"
              aria-label="Find chat"
              onClick={() => setFinderOpen(true)}
            >
              <Search className="size-3.5" />
              <span className="hidden sm:inline">Search</span>
              <ShortcutHint>Ctrl+P</ShortcutHint>
            </Button>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  type="button"
                  variant="outline"
                  size="icon"
                  className="size-8"
                  aria-label="Recent chats"
                >
                  <ChevronDown className="size-3.5" />
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end" className="w-64">
                <DropdownMenuLabel>Recent</DropdownMenuLabel>
                <DropdownMenuSeparator />
                {recentChats.length === 0 ? (
                  <DropdownMenuItem disabled>No recent chats</DropdownMenuItem>
                ) : (
                  recentChats.map((chat) => (
                    <DropdownMenuItem key={chat.id} onClick={() => openChat(chat.id)}>
                      <ChatStatusDot variant={getChatStatusVariant(chat)} mode={chat.mode} />
                      <span className="truncate">{chat.title}</span>
                    </DropdownMenuItem>
                  ))
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </ButtonGroup>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn(
              'size-8',
              isSettingsActive ? 'bg-accent text-accent-foreground' : 'text-muted-foreground'
            )}
            aria-label="Settings"
            aria-current={isSettingsActive ? 'page' : undefined}
            title="Settings"
            onClick={() => void navigate({ to: '/settings' })}
          >
            <Settings className="size-3.5" />
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-8 text-muted-foreground"
                aria-label="More"
              >
                <MoreHorizontal className="size-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                disabled={isAddingProject || isPendingProjects}
                onClick={() => void handleAddProject()}
              >
                <FolderPlus className="size-4" />
                Add project
              </DropdownMenuItem>
              <DropdownMenuItem
                disabled={!canReviewBranch}
                title={
                  canReviewBranch
                    ? 'Compare two branches and start a review chat'
                    : 'Open a project chat to review a branch.'
                }
                onClick={() =>
                  requestOpenBranchReview(
                    activeChat?.projectId ? { projectId: activeChat.projectId } : undefined
                  )
                }
              >
                <GitBranch className="size-4" />
                Review branch…
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>
      <ChatCommandDialog open={finderOpen} onOpenChange={setFinderOpen} />
    </>
  )
}
