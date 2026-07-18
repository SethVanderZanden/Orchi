/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react'
import { useMatch, useNavigate } from '@tanstack/react-router'

import { onChatDeleted } from '@/lib/chat-tabs/chat-deleted'
import {
  planNewChatTab,
  type ResolvedNewChatWorkspace
} from '@/lib/chat-tabs/resolve-workspace-for-new-tab'
import {
  activateAdjacentChatTab,
  activateChatTabAtIndex,
  clearChatSplit,
  closeChatTab,
  createEmptyChatTabsState,
  deactivateChatTab,
  ensureChatTabOpen,
  migrateChatTabId,
  moveChatTabToSplit,
  openChatInSplit as applyOpenChatInSplit,
  openChatTab,
  toggleChatTabPin,
  type ChatTabsState
} from '@/lib/chat-tabs/tab-state'
import { MAX_PINNED_TABS } from '@/lib/chat-tabs/tab-visibility'
import {
  clearComposerDraft,
  hasComposerDraft,
  migrateComposerDraft,
  setComposerDraft
} from '@/lib/chat/composer-drafts'
import { migrateWorktreeIntent } from '@/lib/chat/worktree-intent'
import { isDisposableEmptyChat } from '@/lib/chat/is-disposable-empty-chat'
import { registerChatIdMigrator } from '@/lib/chat/migrate-chat-client-state'
import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import { useChat } from '@/providers/chat-context'
import { useProjects } from '@/providers/project-provider'

export type OpenSplitChatOptions = {
  /** Prefill the composer (user sends manually). */
  initialDraft?: string
  /** Send this message immediately after opening the split chat. */
  sendContent?: string
}

type ChatTabsContextValue = {
  openTabIds: string[]
  activeTabId: string | null
  splitTabId: string | null
  pinnedTabIds: string[]
  openChat: (chatId: string) => void
  /** Opens a chat in the resizable split pane beside the active tab. */
  openChatInSplit: (chatId: string) => void
  closeTab: (chatId: string) => void
  closeAllTabs: (options?: { keepPinned?: boolean }) => void
  togglePin: (chatId: string) => void
  canPinTab: (chatId: string) => boolean
  activateTabAtIndex: (index: number) => void
  activateAdjacentTab: (direction: 'next' | 'previous') => void
  moveTabToSplit: (chatId: string) => void
  clearSplit: () => void
  createAndOpenTab: () => Promise<void>
  /** Pick a folder, register it as a project, then open a new chat there. */
  registerProjectAndOpenTab: () => Promise<void>
  /** Opens a new chat in the resizable split pane. Optionally prefill or auto-send. */
  createAndOpenSplitTab: (options?: OpenSplitChatOptions) => Promise<void>
  isCreatingTab: boolean
  finderOpen: boolean
  setFinderOpen: (open: boolean) => void
}

const ChatTabsContext = createContext<ChatTabsContextValue | null>(null)

export function ChatTabsProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const navigate = useNavigate()
  const chatMatch = useMatch({
    from: '/_app/chat/$chatId',
    shouldThrow: false
  })
  const routeChatId = chatMatch?.params?.chatId ?? null

  const { getChat, createChat, markChatRead, sendMessage, deleteChat } = useChat()
  const { projects, addProject, pickDirectory } = useProjects()

  const [state, setState] = useState<ChatTabsState>(createEmptyChatTabsState)
  const [isCreatingTab, setIsCreatingTab] = useState(false)
  const [finderOpen, setFinderOpen] = useState(false)
  const [trackedRouteChatId, setTrackedRouteChatId] = useState(routeChatId)

  if (routeChatId !== trackedRouteChatId) {
    setTrackedRouteChatId(routeChatId)
    if (routeChatId) {
      setState((current) => ensureChatTabOpen(current, routeChatId))
    }
  }

  const navigateToTab = useCallback(
    (chatId: string | null) => {
      if (!chatId) {
        void navigate({ to: '/' })
        return
      }

      void navigate({ to: '/chat/$chatId', params: { chatId } })
    },
    [navigate]
  )

  useEffect(() => {
    return registerChatIdMigrator((fromId, toId) => {
      migrateComposerDraft(fromId, toId)
      migrateWorktreeIntent(fromId, toId)
      setState((current) => migrateChatTabId(current, fromId, toId))
    })
  }, [])

  useEffect(() => {
    return onChatDeleted((chatId) => {
      setState((current) => {
        const next = closeChatTab(current, chatId)
        if (current.activeTabId === chatId) {
          queueMicrotask(() => navigateToTab(next.activeTabId))
        }
        return next
      })
    })
  }, [navigateToTab])

  useEffect(() => {
    if (!state.splitTabId) {
      return
    }

    markChatRead(state.splitTabId)
  }, [markChatRead, state.splitTabId])

  const openChat = useCallback(
    (chatId: string) => {
      setState((current) => openChatTab(current, chatId))
      navigateToTab(chatId)
    },
    [navigateToTab]
  )

  const openChatInSplitPane = useCallback(
    (chatId: string) => {
      setState((current) => {
        const next = applyOpenChatInSplit(current, chatId)
        queueMicrotask(() => {
          if (next.splitTabId === chatId && next.activeTabId && chatMatch) {
            navigateToTab(next.activeTabId)
            return
          }

          if (!next.splitTabId || next.activeTabId === chatId) {
            navigateToTab(chatId)
          }
        })
        return next
      })
    },
    [chatMatch, navigateToTab]
  )

  const closeTab = useCallback(
    (chatId: string) => {
      const chat = getChat(chatId)

      if (isDisposableEmptyChat(chat, chatId)) {
        clearComposerDraft(chatId)
        void deleteChat(chatId)
        return
      }

      if (hasComposerDraft(chatId)) {
        setState((current) => {
          const isActive = current.activeTabId === chatId
          const isSplit = current.splitTabId === chatId

          if (!isActive && !isSplit) {
            return closeChatTab(current, chatId)
          }

          const next = deactivateChatTab(current, chatId)
          if (isActive) {
            queueMicrotask(() => navigateToTab(next.activeTabId))
          }
          return next
        })
        return
      }

      setState((current) => {
        const next = closeChatTab(current, chatId)
        if (current.activeTabId === chatId) {
          queueMicrotask(() => navigateToTab(next.activeTabId))
        }
        return next
      })
    },
    [deleteChat, getChat, navigateToTab]
  )

  const closeAllTabs = useCallback(
    (options?: { keepPinned?: boolean }) => {
      const idsToClose = state.openTabIds.filter(
        (chatId) => !options?.keepPinned || !state.pinnedTabIds.includes(chatId)
      )

      if (idsToClose.length === 0) {
        return
      }

      const activeId = state.activeTabId
      const inactiveIds = idsToClose.filter((chatId) => chatId !== activeId)

      for (const chatId of inactiveIds) {
        closeTab(chatId)
      }

      if (activeId && idsToClose.includes(activeId)) {
        closeTab(activeId)
      }
    },
    [closeTab, state.activeTabId, state.openTabIds, state.pinnedTabIds]
  )

  const activateTabAtIndex = useCallback(
    (index: number) => {
      setState((current) => {
        const next = activateChatTabAtIndex(current, index)
        if (next.activeTabId && next.activeTabId !== current.activeTabId) {
          queueMicrotask(() => navigateToTab(next.activeTabId))
        }
        return next
      })
    },
    [navigateToTab]
  )

  const activateAdjacentTab = useCallback(
    (direction: 'next' | 'previous') => {
      setState((current) => {
        const next = activateAdjacentChatTab(current, direction)
        if (next.activeTabId && next.activeTabId !== current.activeTabId) {
          queueMicrotask(() => navigateToTab(next.activeTabId))
        }
        return next
      })
    },
    [navigateToTab]
  )

  const moveTabToSplit = useCallback((chatId: string) => {
    setState((current) => moveChatTabToSplit(current, chatId))
  }, [])

  const togglePin = useCallback((chatId: string) => {
    setState((current) => toggleChatTabPin(current, chatId))
  }, [])

  const canPinTab = useCallback(
    (chatId: string) => {
      if (!state.openTabIds.includes(chatId)) {
        return false
      }

      return state.pinnedTabIds.includes(chatId) || state.pinnedTabIds.length < MAX_PINNED_TABS
    },
    [state.openTabIds, state.pinnedTabIds]
  )

  const clearSplit = useCallback(() => {
    setState((current) => clearChatSplit(current))
  }, [])

  const resolveNewTabPlan = useCallback(() => {
    const activeChat = state.activeTabId ? getChat(state.activeTabId) : undefined
    return planNewChatTab(activeChat, projects)
  }, [getChat, projects, state.activeTabId])

  const createChatFromWorkspace = useCallback(
    async (workspace: ResolvedNewChatWorkspace, navigateToChat = true) => {
      return createChat({
        workspaceId: workspace.workspaceId,
        workspacePath: workspace.workspacePath,
        projectId: workspace.projectId ?? undefined,
        navigate: navigateToChat
      })
    },
    [createChat]
  )

  const workspaceFromRegisteredProject =
    useCallback(async (): Promise<ResolvedNewChatWorkspace | null> => {
      const path = await pickDirectory()
      if (!path) {
        return null
      }

      const project = await addProject(path)
      if (!project) {
        return null
      }

      const workspace = getDefaultWorkspace(project)
      if (!workspace) {
        return null
      }

      return {
        workspaceId: workspace.id,
        workspacePath: workspace.path,
        projectId: project.id
      }
    }, [addProject, pickDirectory])

  const createAndOpenTab = useCallback(async () => {
    if (isCreatingTab) {
      return
    }

    const plan = resolveNewTabPlan()
    if (plan.kind === 'needsProject') {
      // Explain that a project/workspace is required (home empty state).
      void navigate({ to: '/' })
      return
    }

    setIsCreatingTab(true)
    try {
      await createChatFromWorkspace(plan.workspace)
    } finally {
      setIsCreatingTab(false)
    }
  }, [createChatFromWorkspace, isCreatingTab, navigate, resolveNewTabPlan])

  const registerProjectAndOpenTab = useCallback(async () => {
    if (isCreatingTab) {
      return
    }

    setIsCreatingTab(true)
    try {
      const workspace = await workspaceFromRegisteredProject()
      if (!workspace) {
        return
      }

      await createChatFromWorkspace(workspace)
    } finally {
      setIsCreatingTab(false)
    }
  }, [createChatFromWorkspace, isCreatingTab, workspaceFromRegisteredProject])

  const createAndOpenSplitTab = useCallback(
    async (options?: OpenSplitChatOptions) => {
      if (isCreatingTab) {
        return
      }

      // Need a primary pane to split beside.
      if (!state.activeTabId && !routeChatId) {
        await createAndOpenTab()
        return
      }

      const plan = resolveNewTabPlan()
      if (plan.kind === 'needsProject') {
        void navigate({ to: '/' })
        return
      }

      setIsCreatingTab(true)
      try {
        const chat = await createChatFromWorkspace(plan.workspace, false)

        const sendContent = options?.sendContent?.trim()
        if (!sendContent && options?.initialDraft?.trim()) {
          setComposerDraft(chat.id, options.initialDraft)
        }

        setState((current) => applyOpenChatInSplit(current, chat.id))

        if (sendContent) {
          await sendMessage(chat.id, sendContent)
        }
      } finally {
        setIsCreatingTab(false)
      }
    },
    [
      createAndOpenTab,
      createChatFromWorkspace,
      isCreatingTab,
      navigate,
      resolveNewTabPlan,
      routeChatId,
      sendMessage,
      state.activeTabId
    ]
  )

  const value = useMemo<ChatTabsContextValue>(
    () => ({
      openTabIds: state.openTabIds,
      activeTabId: state.activeTabId,
      splitTabId: state.splitTabId,
      pinnedTabIds: state.pinnedTabIds,
      openChat,
      openChatInSplit: openChatInSplitPane,
      closeTab,
      closeAllTabs,
      togglePin,
      canPinTab,
      activateTabAtIndex,
      activateAdjacentTab,
      moveTabToSplit,
      clearSplit,
      createAndOpenTab,
      registerProjectAndOpenTab,
      createAndOpenSplitTab,
      isCreatingTab,
      finderOpen,
      setFinderOpen
    }),
    [
      activateAdjacentTab,
      activateTabAtIndex,
      canPinTab,
      clearSplit,
      closeAllTabs,
      closeTab,
      createAndOpenSplitTab,
      createAndOpenTab,
      finderOpen,
      isCreatingTab,
      moveTabToSplit,
      openChat,
      openChatInSplitPane,
      registerProjectAndOpenTab,
      state.activeTabId,
      state.openTabIds,
      state.pinnedTabIds,
      state.splitTabId,
      togglePin
    ]
  )

  return <ChatTabsContext.Provider value={value}>{children}</ChatTabsContext.Provider>
}

export function useChatTabs(): ChatTabsContextValue {
  const context = useContext(ChatTabsContext)
  if (!context) {
    throw new Error('useChatTabs must be used within ChatTabsProvider')
  }

  return context
}
