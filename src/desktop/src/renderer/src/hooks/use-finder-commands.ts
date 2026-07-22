import { useCallback, useMemo } from 'react'
import { useNavigate } from '@tanstack/react-router'

import { openWorkspaceInPreferredEditor } from '@/lib/preferences/open-in-editor'
import { usePreferredEditor } from '@/hooks/use-preferred-editor'
import type { AppFinderCommand } from '@/lib/app-commands/finder-commands'
import { requestOpenBranchReview } from '@/lib/branch-review/events'
import { getOpenInEditorLabel } from '@/lib/preferences/preferred-editor'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

export function useFinderCommands(onComplete: () => void): AppFinderCommand[] {
  const navigate = useNavigate()
  const { getChat } = useChat()
  const { preferredEditor } = usePreferredEditor()
  const { addProject, pickDirectory, isPendingProjects } = useProjects()
  const {
    activeTabId,
    pinnedTabIds,
    openTabIds,
    splitTabId,
    createAndOpenTab,
    createAndOpenSplitTab,
    closeTab,
    closeAllTabs,
    openChat,
    openChatInSplit,
    activateAdjacentTab,
    activateTabAtIndex,
    togglePin,
    canPinTab,
    isCreatingTab
  } = useChatTabs()

  const complete = useCallback(
    (action: () => void | Promise<void>) => {
      void Promise.resolve(action()).finally(onComplete)
    },
    [onComplete]
  )

  const activeChat = activeTabId ? getChat(activeTabId) : null
  const activeWorkspacePath = activeChat?.workspacePath?.trim() ?? ''
  const parentChatId = activeChat?.parentChatId ?? null
  const unpinnedOpenTabCount = openTabIds.filter((chatId) => !pinnedTabIds.includes(chatId)).length

  return useMemo(() => {
    const commands: AppFinderCommand[] = [
      {
        id: 'new-chat',
        label: 'New chat',
        keywords: ['create', 'tab', 'message'],
        shortcut: 'Ctrl+N',
        disabled: isCreatingTab,
        onSelect: () => complete(() => createAndOpenTab())
      },
      {
        id: 'open-beside',
        label: 'Open chat beside',
        keywords: ['split', 'pane', 'beside', 'side'],
        shortcut: 'Ctrl+→',
        disabled: isCreatingTab,
        onSelect: () => complete(() => createAndOpenSplitTab())
      },
      {
        id: 'open-parent-beside',
        label: 'Open parent chat beside',
        keywords: ['parent', 'split', 'beside', 'orchestration'],
        shortcut: 'Ctrl+↑',
        disabled: !parentChatId,
        onSelect: () =>
          complete(() => {
            if (!parentChatId || !activeTabId) {
              return
            }

            if (splitTabId === activeTabId) {
              openChat(parentChatId)
              return
            }

            openChatInSplit(parentChatId)
          })
      },
      {
        id: 'open-in-editor',
        label: getOpenInEditorLabel(preferredEditor),
        keywords: ['open', 'editor', 'workspace', 'cursor', 'vscode', 'code'],
        shortcut: 'Ctrl+E',
        disabled: !activeWorkspacePath,
        onSelect: () =>
          complete(async () => {
            await openWorkspaceInPreferredEditor(activeWorkspacePath)
          })
      },
      {
        id: 'review-branch',
        label: 'Review branch',
        keywords: ['review', 'branch', 'pr', 'pull', 'request', 'git', 'diff'],
        disabled: !activeChat?.projectId,
        onSelect: () => complete(() => requestOpenBranchReview())
      },
      {
        id: 'close-tab',
        label: 'Close tab',
        keywords: ['close', 'tab'],
        shortcut: 'Ctrl+W',
        disabled: !activeTabId,
        onSelect: () => {
          if (!activeTabId) {
            return
          }
          complete(() => closeTab(activeTabId))
        }
      },
      {
        id: 'next-tab',
        label: 'Next tab',
        keywords: ['switch', 'tab', 'forward'],
        shortcut: 'Ctrl+Tab',
        disabled: openTabIds.length <= 1,
        onSelect: () => complete(() => activateAdjacentTab('next'))
      },
      {
        id: 'previous-tab',
        label: 'Previous tab',
        keywords: ['switch', 'tab', 'back'],
        shortcut: 'Ctrl+Shift+Tab',
        disabled: openTabIds.length <= 1,
        onSelect: () => complete(() => activateAdjacentTab('previous'))
      },
      {
        id: 'settings',
        label: 'Settings',
        keywords: ['preferences', 'config', 'options'],
        shortcut: 'Ctrl+,',
        onSelect: () => complete(() => navigate({ to: '/settings' }))
      },
      {
        id: 'add-project',
        label: 'Add project',
        keywords: ['project', 'folder', 'workspace', 'register'],
        disabled: isPendingProjects,
        onSelect: () =>
          complete(async () => {
            const path = await pickDirectory()
            if (!path) {
              return
            }
            await addProject(path)
          })
      },
      {
        id: 'close-all-chats',
        label: '> Close All Chats',
        keywords: ['close', 'all', 'tabs', 'clear'],
        disabled: openTabIds.length === 0,
        onSelect: () => complete(() => closeAllTabs())
      },
      {
        id: 'close-all-but-pinned',
        label: '> Close All but Pinned Chats',
        keywords: ['close', 'pinned', 'tabs', 'clear'],
        disabled: unpinnedOpenTabCount === 0,
        onSelect: () => complete(() => closeAllTabs({ keepPinned: true }))
      },
      {
        id: 'pin-chat',
        label: '> Pin Chat',
        keywords: ['pin', 'tab', 'chat'],
        disabled: !activeTabId || !canPinTab(activeTabId),
        onSelect: () =>
          complete(() => {
            if (!activeTabId || !canPinTab(activeTabId)) {
              return
            }

            togglePin(activeTabId)
          })
      }
    ]

    for (let index = 0; index < Math.min(openTabIds.length, 9); index += 1) {
      const tabNumber = index + 1
      commands.push({
        id: `tab-${tabNumber}`,
        label: `Go to tab ${tabNumber}`,
        keywords: ['switch', 'tab', String(tabNumber)],
        shortcut: `Ctrl+${tabNumber}`,
        onSelect: () => complete(() => activateTabAtIndex(index))
      })
    }

    return commands
  }, [
    activateAdjacentTab,
    activateTabAtIndex,
    activeChat?.projectId,
    activeTabId,
    activeWorkspacePath,
    addProject,
    canPinTab,
    closeAllTabs,
    closeTab,
    complete,
    createAndOpenSplitTab,
    createAndOpenTab,
    isCreatingTab,
    isPendingProjects,
    navigate,
    openChat,
    openChatInSplit,
    openTabIds.length,
    parentChatId,
    pickDirectory,
    preferredEditor,
    splitTabId,
    togglePin,
    unpinnedOpenTabCount
  ])
}
