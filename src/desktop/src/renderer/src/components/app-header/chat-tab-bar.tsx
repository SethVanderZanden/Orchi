import { useMemo } from 'react'
import { useMatch } from '@tanstack/react-router'

import { ChatTab } from '@/components/app-header/chat-tab'
import { ChatTabOverflowMenu } from '@/components/app-header/chat-tab-overflow-menu'
import { useComposerDraftRevision } from '@/hooks/use-composer-draft-revision'
import { useElementWidth } from '@/hooks/use-element-width'
import { hasComposerDraft } from '@/lib/chat/composer-drafts'
import { resolveTabVisibility } from '@/lib/chat-tabs/tab-visibility'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

const CHAT_TAB_MIME = 'application/x-orchi-chat-tab'

export function ChatTabBar(): React.JSX.Element {
  const settingsMatch = useMatch({ from: '/_app/settings', shouldThrow: false })
  const isNavTabActive = Boolean(settingsMatch)

  useComposerDraftRevision()

  const {
    openTabIds,
    activeTabId,
    splitTabId,
    pinnedTabIds,
    openChat,
    openChatInSplit,
    closeTab,
    moveTabToSplit,
    togglePin,
    canPinTab
  } = useChatTabs()
  const { getChat, getChatStatusVariant } = useChat()
  const { projects } = useProjects()
  const { width: tabStripWidth, ref: tabStripRef } = useElementWidth<HTMLDivElement>()

  const { visibleTabIds, overflowTabIds } = useMemo(
    () => resolveTabVisibility(openTabIds, pinnedTabIds, activeTabId, tabStripWidth),
    [activeTabId, openTabIds, pinnedTabIds, tabStripWidth]
  )

  const overflowTabs = useMemo(
    () =>
      overflowTabIds.map((chatId) => {
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

        return {
          chatId,
          title,
          projectName,
          statusVariant,
          mode: chat?.mode ?? 'default',
          isActive: !isNavTabActive && chatId === activeTabId,
          isSplit: chatId === splitTabId
        }
      }),
    [
      activeTabId,
      getChat,
      getChatStatusVariant,
      isNavTabActive,
      overflowTabIds,
      projects,
      splitTabId
    ]
  )

  return (
    <div
      className="flex min-w-0 flex-1 items-center gap-0.5 py-0.5 pr-2"
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
      <div ref={tabStripRef} className="flex min-w-0 flex-1 items-center gap-0.5 overflow-hidden">
        {visibleTabIds.map((chatId) => {
          const chat = getChat(chatId)
          const title = chat?.title ?? 'Chat'
          const isViewing = chatId === activeTabId || chatId === splitTabId
          const statusVariant =
            hasComposerDraft(chatId) && !isViewing
              ? ('draft' as const)
              : chat
                ? getChatStatusVariant(chat, { isViewing })
                : ('standard' as const)
          const projectName =
            projects.find((project) => project.id === chat?.projectId)?.name ?? null

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
              isPinned={pinnedTabIds.includes(chatId)}
              canPin={canPinTab(chatId)}
              onSelect={() => openChat(chatId)}
              onOpenBeside={() => openChatInSplit(chatId)}
              onClose={() => closeTab(chatId)}
              onTogglePin={() => togglePin(chatId)}
            />
          )
        })}

        <ChatTabOverflowMenu tabs={overflowTabs} onSelect={openChat} />
      </div>
    </div>
  )
}
