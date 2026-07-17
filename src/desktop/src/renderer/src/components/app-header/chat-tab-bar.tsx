import { Columns3 } from 'lucide-react'
import { useMatch, useNavigate } from '@tanstack/react-router'

import { ChatTab } from '@/components/app-header/chat-tab'
import { ShortcutHint } from '@/components/app-header/shortcut-hint'
import { useComposerDraftRevision } from '@/hooks/use-composer-draft-revision'
import { hasComposerDraft } from '@/lib/chat/composer-drafts'
import { cn } from '@/lib/utils'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

const CHAT_TAB_MIME = 'application/x-orchi-chat-tab'

export function ChatTabBar(): React.JSX.Element {
  const navigate = useNavigate()
  const boardMatch = useMatch({ from: '/_app/board', shouldThrow: false })
  const settingsMatch = useMatch({ from: '/_app/settings', shouldThrow: false })
  const isBoardActive = Boolean(boardMatch)
  const isNavTabActive = isBoardActive || Boolean(settingsMatch)

  useComposerDraftRevision()

  const { openTabIds, activeTabId, splitTabId, openChat, closeTab, moveTabToSplit } = useChatTabs()
  const { getChat, getChatStatusVariant } = useChat()
  const { projects } = useProjects()

  return (
    <div
      className="flex min-w-0 flex-1 items-center gap-0.5 overflow-x-auto py-0.5 pr-2 [scrollbar-width:thin]"
      onDragOver={(event) => {
        if (!event.dataTransfer.types.includes(CHAT_TAB_MIME)) {
          return
        }

        event.preventDefault()
        event.dataTransfer.dropEffect = 'move'
      }}
      onDrop={(event) => {
        const draggedId = event.dataTransfer.getData(CHAT_TAB_MIME)
        if (!draggedId || draggedId === activeTabId) {
          return
        }

        event.preventDefault()
        moveTabToSplit(draggedId)
      }}
    >
      <button
        type="button"
        onClick={() => void navigate({ to: '/board' })}
        aria-current={isBoardActive ? 'page' : undefined}
        title="Agents board (Ctrl+B)"
        className={cn(
          'flex h-8 shrink-0 items-center gap-1.5 rounded-md px-2 text-xs',
          isBoardActive
            ? 'bg-accent font-medium text-accent-foreground'
            : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground'
        )}
      >
        <Columns3 className="size-3.5 shrink-0" />
        <span>Board</span>
        <ShortcutHint>Ctrl+B</ShortcutHint>
      </button>

      {openTabIds.length > 0 ? (
        <div className="mx-0.5 h-4 w-px shrink-0 bg-border" aria-hidden />
      ) : null}

      {openTabIds.map((chatId) => {
        const chat = getChat(chatId)
        const title = chat?.title ?? 'Chat'
        const isViewing = chatId === activeTabId || chatId === splitTabId
        const statusVariant =
          hasComposerDraft(chatId) && !isViewing
            ? ('draft' as const)
            : chat
              ? getChatStatusVariant(chat, { isViewing })
              : ('standard' as const)
        const projectName = projects.find((project) => project.id === chat?.projectId)?.name ?? null

        return (
          <ChatTab
            key={chatId}
            chatId={chatId}
            title={title}
            projectName={projectName}
            statusVariant={statusVariant}
            mode={chat?.mode ?? 'default'}
            isActive={!isNavTabActive && chatId === activeTabId}
            isSplit={chatId === splitTabId}
            onSelect={() => openChat(chatId)}
            onClose={() => closeTab(chatId)}
          />
        )
      })}
    </div>
  )
}
