import { describe, expect, it } from 'vitest'

import {
  activateAdjacentChatTab,
  activateChatTab,
  activateChatTabAtIndex,
  clearChatSplit,
  closeChatTab,
  createEmptyChatTabsState,
  deactivateChatTab,
  ensureChatTabOpen,
  migrateChatTabId,
  moveChatTabToSplit,
  openChatInSplit,
  openChatTab,
  type ChatTabsState
} from '@/lib/chat-tabs/tab-state'

describe('chat tab state', () => {
  const empty = createEmptyChatTabsState()

  it('opens a chat as the active tab', () => {
    const next = openChatTab(empty, 'a')
    expect(next).toEqual({ openTabIds: ['a'], activeTabId: 'a', splitTabId: null })
  })

  it('activates an already-open tab without duplicating', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: null }
    expect(openChatTab(state, 'b')).toEqual({
      openTabIds: ['a', 'b'],
      activeTabId: 'b',
      splitTabId: null
    })
  })

  it('closes the active tab and activates the right neighbor', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b', 'c'], activeTabId: 'b', splitTabId: null }
    expect(closeChatTab(state, 'b')).toEqual({
      openTabIds: ['a', 'c'],
      activeTabId: 'c',
      splitTabId: null
    })
  })

  it('closes the last tab and clears active', () => {
    const state: ChatTabsState = { openTabIds: ['a'], activeTabId: 'a', splitTabId: null }
    expect(closeChatTab(state, 'a')).toEqual({
      openTabIds: [],
      activeTabId: null,
      splitTabId: null
    })
  })

  it('closes a non-active tab without changing active', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: null }
    expect(closeChatTab(state, 'b')).toEqual({
      openTabIds: ['a'],
      activeTabId: 'a',
      splitTabId: null
    })
  })

  it('clears split when the split tab is closed', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: 'b' }
    expect(closeChatTab(state, 'b')).toEqual({
      openTabIds: ['a'],
      activeTabId: 'a',
      splitTabId: null
    })
  })

  it('activates the next tab with wrap-around', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b', 'c'], activeTabId: 'b', splitTabId: null }
    expect(activateAdjacentChatTab(state, 'next')).toEqual({
      openTabIds: ['a', 'b', 'c'],
      activeTabId: 'c',
      splitTabId: null
    })
    expect(activateAdjacentChatTab({ ...state, activeTabId: 'c' }, 'next')).toEqual({
      openTabIds: ['a', 'b', 'c'],
      activeTabId: 'a',
      splitTabId: null
    })
  })

  it('activates the previous tab with wrap-around', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b', 'c'], activeTabId: 'b', splitTabId: null }
    expect(activateAdjacentChatTab(state, 'previous')).toEqual({
      openTabIds: ['a', 'b', 'c'],
      activeTabId: 'a',
      splitTabId: null
    })
    expect(activateAdjacentChatTab({ ...state, activeTabId: 'a' }, 'previous')).toEqual({
      openTabIds: ['a', 'b', 'c'],
      activeTabId: 'c',
      splitTabId: null
    })
  })

  it('does not change state when fewer than two tabs are open', () => {
    const single: ChatTabsState = { openTabIds: ['a'], activeTabId: 'a', splitTabId: null }
    expect(activateAdjacentChatTab(single, 'next')).toBe(single)
    expect(activateAdjacentChatTab(empty, 'previous')).toBe(empty)
  })

  it('activates by index', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b', 'c'], activeTabId: 'a', splitTabId: null }
    expect(activateChatTabAtIndex(state, 2)).toEqual({
      openTabIds: ['a', 'b', 'c'],
      activeTabId: 'c',
      splitTabId: null
    })
    expect(activateChatTabAtIndex(state, 9)).toEqual(state)
  })

  it('ensures route chat is open and active', () => {
    expect(ensureChatTabOpen(empty, 'x')).toEqual({
      openTabIds: ['x'],
      activeTabId: 'x',
      splitTabId: null
    })
    expect(activateChatTab({ openTabIds: ['x'], activeTabId: 'x', splitTabId: null }, 'x')).toEqual(
      {
        openTabIds: ['x'],
        activeTabId: 'x',
        splitTabId: null
      }
    )
  })

  it('migrates tab ids when a draft is promoted', () => {
    const state: ChatTabsState = {
      openTabIds: ['local-1', 'b'],
      activeTabId: 'local-1',
      splitTabId: 'local-1'
    }
    expect(migrateChatTabId(state, 'local-1', 'server-1')).toEqual({
      openTabIds: ['server-1', 'b'],
      activeTabId: 'server-1',
      splitTabId: 'server-1'
    })
  })

  it('moves a tab into the split pane', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: null }
    expect(moveChatTabToSplit(state, 'b')).toEqual({
      openTabIds: ['a', 'b'],
      activeTabId: 'a',
      splitTabId: 'b'
    })
  })

  it('does not split the active tab onto itself', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: null }
    expect(moveChatTabToSplit(state, 'a')).toEqual(state)
  })

  it('opens a chat in split while keeping the primary', () => {
    const state: ChatTabsState = { openTabIds: ['a'], activeTabId: 'a', splitTabId: null }
    expect(openChatInSplit(state, 'b')).toEqual({
      openTabIds: ['a', 'b'],
      activeTabId: 'a',
      splitTabId: 'b'
    })
  })

  it('clears the split pane', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: 'b' }
    expect(clearChatSplit(state)).toEqual({
      openTabIds: ['a', 'b'],
      activeTabId: 'a',
      splitTabId: null
    })
  })

  it('clears split when the split tab becomes the active primary', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: 'b' }
    expect(openChatTab(state, 'b')).toEqual({
      openTabIds: ['a', 'b'],
      activeTabId: 'b',
      splitTabId: null
    })
  })

  it('deactivates the active tab without removing it from the bar', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b', 'c'], activeTabId: 'b', splitTabId: null }
    expect(deactivateChatTab(state, 'b')).toEqual({
      openTabIds: ['a', 'b', 'c'],
      activeTabId: 'c',
      splitTabId: null
    })
  })

  it('deactivates the only open tab', () => {
    const state: ChatTabsState = { openTabIds: ['a'], activeTabId: 'a', splitTabId: null }
    expect(deactivateChatTab(state, 'a')).toEqual({
      openTabIds: ['a'],
      activeTabId: null,
      splitTabId: null
    })
  })

  it('clears split when deactivating the split tab', () => {
    const state: ChatTabsState = { openTabIds: ['a', 'b'], activeTabId: 'a', splitTabId: 'b' }
    expect(deactivateChatTab(state, 'b')).toEqual({
      openTabIds: ['a', 'b'],
      activeTabId: 'a',
      splitTabId: null
    })
  })
})
