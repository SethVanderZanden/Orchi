import { createFileRoute, Navigate } from '@tanstack/react-router'
import { FolderPlus, GitBranch, MessageSquare, Plus } from 'lucide-react'

import { ShortcutHint } from '@/components/app-header/shortcut-hint'
import { ChatStatusDot } from '@/components/chat/chat-status-dot'
import { EmptyState } from '@/components/empty-state'
import { Button } from '@/components/ui/button'
import { requestOpenBranchReview } from '@/lib/branch-review/events'
import type { ChatStatusVariant } from '@/lib/chat/chat-status-variant'
import type { ChatThread } from '@/lib/chat/types'
import { getRecentChats } from '@/lib/chat-finder/build-chat-finder-groups'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

export const Route = createFileRoute('/_app/')({
  component: AppIndexPage
})

function AppIndexPage(): React.JSX.Element {
  const {
    openTabIds,
    activeTabId,
    createAndOpenTab,
    openChat,
    registerProjectAndOpenTab,
    isCreatingTab
  } = useChatTabs()
  const { chats, getChatStatusVariant } = useChat()
  const { projects } = useProjects()
  const chatId = activeTabId ?? openTabIds[0]
  const hasProjects = projects.length > 0
  const recentChats = getRecentChats(chats)
  const projectNameById = new Map(projects.map((project) => [project.id, project.name]))

  if (chatId) {
    return <Navigate to="/chat/$chatId" params={{ chatId }} replace />
  }

  if (!hasProjects) {
    return (
      <EmptyState
        title="Add a project to get started"
        description="Chats need a workspace folder. Register a project, then you can open new chats."
        icon={<FolderPlus className="size-8" />}
      >
        <Button
          type="button"
          disabled={isCreatingTab}
          onClick={() => void registerProjectAndOpenTab()}
          className="gap-1.5"
        >
          <FolderPlus className="size-4" />
          Add project & new chat
        </Button>
      </EmptyState>
    )
  }

  return (
    <EmptyState
      title="No chats open"
      description="Open a new chat, pick a recent one, or find any chat with Ctrl+P."
      icon={<MessageSquare className="size-8" />}
    >
      <Button
        type="button"
        disabled={isCreatingTab}
        onClick={() => void createAndOpenTab()}
        className="gap-1.5"
      >
        <Plus className="size-4" />
        New chat
      </Button>
      {projects.length === 1 ? (
        <Button
          type="button"
          variant="outline"
          className="gap-1.5"
          onClick={() => requestOpenBranchReview({ projectId: projects[0]?.id })}
        >
          <GitBranch className="size-4" />
          Review branch
        </Button>
      ) : null}
      <div className="flex w-full max-w-xs flex-col gap-1.5 text-left text-sm">
        <ShortcutRow label="New chat tab" shortcut="Ctrl+N" />
        <ShortcutRow label="Agents board" shortcut="Ctrl+B" />
        <ShortcutRow label="Open chat beside" shortcut="Ctrl+ArrowRight" />
        <ShortcutRow label="Find chat / Review branch" shortcut="Ctrl+P" />
      </div>
      {recentChats.length > 0 ? (
        <div className="mt-1 flex w-full max-w-sm flex-col gap-1 text-left">
          <p className="px-2 text-xs font-medium text-muted-foreground">Recent</p>
          <ul className="flex flex-col gap-0.5">
            {recentChats.map((chat) => (
              <RecentChatRow
                key={chat.id}
                chat={chat}
                projectName={chat.projectId ? (projectNameById.get(chat.projectId) ?? null) : null}
                statusVariant={getChatStatusVariant(chat)}
                onOpen={() => openChat(chat.id)}
              />
            ))}
          </ul>
        </div>
      ) : null}
    </EmptyState>
  )
}

function RecentChatRow({
  chat,
  projectName,
  statusVariant,
  onOpen
}: {
  chat: ChatThread
  projectName: string | null
  statusVariant: ChatStatusVariant
  onOpen: () => void
}): React.JSX.Element {
  return (
    <li>
      <button
        type="button"
        onClick={onOpen}
        className="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm transition-colors hover:bg-accent hover:text-accent-foreground"
      >
        <ChatStatusDot variant={statusVariant} mode={chat.mode} />
        <span className="min-w-0 flex-1 truncate font-medium">{chat.title}</span>
        {projectName ? (
          <span className="max-w-[40%] shrink-0 truncate text-xs text-muted-foreground">
            {projectName}
          </span>
        ) : null}
      </button>
    </li>
  )
}

function ShortcutRow({ label, shortcut }: { label: string; shortcut: string }): React.JSX.Element {
  return (
    <div className="flex items-center justify-between gap-6">
      <span className="text-muted-foreground">{label}</span>
      <ShortcutHint>{shortcut}</ShortcutHint>
    </div>
  )
}
