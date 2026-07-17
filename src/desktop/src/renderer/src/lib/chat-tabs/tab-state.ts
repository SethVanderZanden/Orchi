import { MAX_PINNED_TABS } from '@/lib/chat-tabs/tab-visibility'

export type ChatTabsState = {
  openTabIds: string[]
  activeTabId: string | null
  /** Chat shown in the secondary resizable pane; null when not split. */
  splitTabId: string | null
  /** Up to three chats kept visible when the tab bar overflows. */
  pinnedTabIds: string[]
}

export function createEmptyChatTabsState(): ChatTabsState {
  return { openTabIds: [], activeTabId: null, splitTabId: null, pinnedTabIds: [] }
}

export function openChatTab(state: ChatTabsState, chatId: string): ChatTabsState {
  const splitTabId = state.splitTabId === chatId ? null : state.splitTabId

  if (state.openTabIds.includes(chatId)) {
    return { ...state, activeTabId: chatId, splitTabId }
  }

  return {
    ...state,
    openTabIds: [...state.openTabIds, chatId],
    activeTabId: chatId,
    splitTabId
  }
}

/** Deactivate a tab but keep it in the tab bar (e.g. closed with a composer draft). */
export function deactivateChatTab(state: ChatTabsState, chatId: string): ChatTabsState {
  const index = state.openTabIds.indexOf(chatId)
  if (index < 0) {
    return state
  }

  const splitTabId = state.splitTabId === chatId ? null : state.splitTabId

  if (state.activeTabId !== chatId) {
    return { ...state, splitTabId }
  }

  const openTabIds = state.openTabIds
  const neighbor =
    openTabIds.find((id) => id === splitTabId && id !== chatId) ??
    openTabIds[index + 1] ??
    openTabIds[index - 1] ??
    null

  return {
    ...state,
    openTabIds,
    activeTabId: neighbor,
    splitTabId: neighbor && splitTabId === neighbor ? null : splitTabId
  }
}

export function closeChatTab(state: ChatTabsState, chatId: string): ChatTabsState {
  const index = state.openTabIds.indexOf(chatId)
  if (index < 0) {
    return state
  }

  const openTabIds = state.openTabIds.filter((id) => id !== chatId)
  const splitTabId = state.splitTabId === chatId ? null : state.splitTabId
  const pinnedTabIds = state.pinnedTabIds.filter((id) => id !== chatId)

  if (state.activeTabId !== chatId) {
    return { ...state, openTabIds, splitTabId, pinnedTabIds }
  }

  const neighbor =
    openTabIds.find((id) => id === splitTabId) ?? openTabIds[index] ?? openTabIds[index - 1] ?? null

  return {
    openTabIds,
    activeTabId: neighbor,
    splitTabId: neighbor && splitTabId === neighbor ? null : splitTabId,
    pinnedTabIds
  }
}

export function activateChatTab(state: ChatTabsState, chatId: string): ChatTabsState {
  if (!state.openTabIds.includes(chatId)) {
    return state
  }

  const splitTabId = state.splitTabId === chatId ? null : state.splitTabId
  return { ...state, activeTabId: chatId, splitTabId }
}

export function activateChatTabAtIndex(state: ChatTabsState, index: number): ChatTabsState {
  const chatId = state.openTabIds[index]
  if (!chatId) {
    return state
  }

  return activateChatTab(state, chatId)
}

export function activateAdjacentChatTab(
  state: ChatTabsState,
  direction: 'next' | 'previous'
): ChatTabsState {
  const { openTabIds, activeTabId } = state
  if (openTabIds.length <= 1) {
    return state
  }

  const currentIndex = activeTabId ? openTabIds.indexOf(activeTabId) : 0
  const startIndex = currentIndex >= 0 ? currentIndex : 0
  const delta = direction === 'next' ? 1 : -1
  const nextIndex = (startIndex + delta + openTabIds.length) % openTabIds.length

  return activateChatTabAtIndex(state, nextIndex)
}

export function ensureChatTabOpen(state: ChatTabsState, chatId: string): ChatTabsState {
  if (state.openTabIds.includes(chatId) && state.activeTabId === chatId) {
    return state
  }

  return openChatTab(state, chatId)
}

export function migrateChatTabId(
  state: ChatTabsState,
  fromId: string,
  toId: string
): ChatTabsState {
  if (!state.openTabIds.includes(fromId) && state.splitTabId !== fromId) {
    return state
  }

  const openTabIds = state.openTabIds.map((id) => (id === fromId ? toId : id))
  const activeTabId = state.activeTabId === fromId ? toId : state.activeTabId
  const splitTabId = state.splitTabId === fromId ? toId : state.splitTabId
  const pinnedTabIds = state.pinnedTabIds.map((id) => (id === fromId ? toId : id))
  return { openTabIds, activeTabId, splitTabId, pinnedTabIds }
}

/** Place `chatId` in the secondary pane beside the active (primary) tab. */
export function moveChatTabToSplit(state: ChatTabsState, chatId: string): ChatTabsState {
  if (!chatId || state.activeTabId === chatId) {
    return state
  }

  const withTab = state.openTabIds.includes(chatId)
    ? state
    : { ...state, openTabIds: [...state.openTabIds, chatId] }

  return { ...withTab, splitTabId: chatId }
}

/** Open a chat in the split pane (creating the tab if needed). */
export function openChatInSplit(state: ChatTabsState, chatId: string): ChatTabsState {
  if (!chatId) {
    return state
  }

  const openTabIds = state.openTabIds.includes(chatId)
    ? state.openTabIds
    : [...state.openTabIds, chatId]

  // Prefer keeping the current primary; if none, use another open tab.
  let activeTabId = state.activeTabId
  if (!activeTabId || activeTabId === chatId) {
    activeTabId = openTabIds.find((id) => id !== chatId) ?? chatId
  }

  if (activeTabId === chatId) {
    return { ...state, openTabIds, activeTabId, splitTabId: null }
  }

  return { ...state, openTabIds, activeTabId, splitTabId: chatId }
}

export function clearChatSplit(state: ChatTabsState): ChatTabsState {
  if (!state.splitTabId) {
    return state
  }

  return { ...state, splitTabId: null }
}

export function toggleChatTabPin(state: ChatTabsState, chatId: string): ChatTabsState {
  if (!state.openTabIds.includes(chatId)) {
    return state
  }

  if (state.pinnedTabIds.includes(chatId)) {
    return {
      ...state,
      pinnedTabIds: state.pinnedTabIds.filter((id) => id !== chatId)
    }
  }

  if (state.pinnedTabIds.length >= MAX_PINNED_TABS) {
    return state
  }

  return {
    ...state,
    pinnedTabIds: [...state.pinnedTabIds, chatId]
  }
}
