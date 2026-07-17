import { useCallback } from 'react'
import { useNavigate } from '@tanstack/react-router'

import { openWorkspaceInPreferredEditor } from '@/lib/preferences/open-in-editor'
import { useKeyboardShortcut, useKeyboardShortcutCombo } from '@/hooks/use-keyboard-shortcut'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'

export function useAppShortcuts(): void {
  const navigate = useNavigate()
  const { getChat } = useChat()
  const {
    createAndOpenTab,
    createAndOpenSplitTab,
    closeTab,
    activeTabId,
    openTabIds,
    activateTabAtIndex,
    activateAdjacentTab,
    setFinderOpen,
    isCreatingTab
  } = useChatTabs()

  const openFinder = useCallback(() => {
    setFinderOpen(true)
  }, [setFinderOpen])

  const closeActiveTab = useCallback(() => {
    if (!activeTabId) {
      return
    }
    closeTab(activeTabId)
  }, [activeTabId, closeTab])

  const createTab = useCallback(() => {
    void createAndOpenTab()
  }, [createAndOpenTab])

  const createSplitTab = useCallback(() => {
    void createAndOpenSplitTab()
  }, [createAndOpenSplitTab])

  const openSettings = useCallback(() => {
    void navigate({ to: '/settings' })
  }, [navigate])

  const openBoard = useCallback(() => {
    void navigate({ to: '/board' })
  }, [navigate])

  const openInEditor = useCallback(() => {
    if (!activeTabId) {
      return
    }

    const chat = getChat(activeTabId)
    if (!chat?.workspacePath) {
      return
    }

    void openWorkspaceInPreferredEditor(chat.workspacePath)
  }, [activeTabId, getChat])

  useKeyboardShortcut('n', createTab, { enabled: !isCreatingTab })

  useKeyboardShortcutCombo({ key: 'k', ctrl: true }, openFinder, {
    allowInTextarea: true
  })

  useKeyboardShortcutCombo({ key: 'w', ctrl: true }, closeActiveTab, {
    allowInTextarea: true,
    enabled: Boolean(activeTabId)
  })

  useKeyboardShortcutCombo({ key: 'ArrowRight', ctrl: true }, createSplitTab, {
    enabled: !isCreatingTab
  })

  useKeyboardShortcutCombo({ key: 'e', ctrl: true }, openInEditor, {
    allowInTextarea: true,
    enabled: Boolean(activeTabId)
  })

  useKeyboardShortcutCombo({ key: 'Tab', ctrl: true }, () => activateAdjacentTab('next'), {
    allowInTextarea: true,
    enabled: openTabIds.length > 1
  })

  useKeyboardShortcutCombo(
    { key: 'Tab', ctrl: true, shift: true },
    () => activateAdjacentTab('previous'),
    {
      allowInTextarea: true,
      enabled: openTabIds.length > 1
    }
  )

  useKeyboardShortcutCombo({ key: ',', ctrl: true }, openSettings, {
    allowInTextarea: true
  })

  useKeyboardShortcutCombo({ key: 'b', ctrl: true }, openBoard, {
    allowInTextarea: true
  })

  useKeyboardShortcutCombo({ key: '1', ctrl: true }, () => activateTabAtIndex(0), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '2', ctrl: true }, () => activateTabAtIndex(1), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '3', ctrl: true }, () => activateTabAtIndex(2), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '4', ctrl: true }, () => activateTabAtIndex(3), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '5', ctrl: true }, () => activateTabAtIndex(4), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '6', ctrl: true }, () => activateTabAtIndex(5), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '7', ctrl: true }, () => activateTabAtIndex(6), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '8', ctrl: true }, () => activateTabAtIndex(7), {
    allowInTextarea: true
  })
  useKeyboardShortcutCombo({ key: '9', ctrl: true }, () => activateTabAtIndex(8), {
    allowInTextarea: true
  })
}
