import { describe, expect, it } from 'vitest'

import {
  CHAT_TAB_GAP_PX,
  CHAT_TAB_MIN_WIDTH_PX,
  CHAT_TAB_OVERFLOW_BUTTON_WIDTH_PX,
  MAX_PINNED_TABS,
  calculateMaxVisibleChatTabs,
  countTabsFittingWidth,
  resolveTabVisibility
} from '@/lib/chat-tabs/tab-visibility'

function widthForTabs(count: number): number {
  if (count <= 0) {
    return 0
  }

  return count * CHAT_TAB_MIN_WIDTH_PX + (count - 1) * CHAT_TAB_GAP_PX
}

describe('countTabsFittingWidth', () => {
  it('returns zero for empty or negative space', () => {
    expect(countTabsFittingWidth(0)).toBe(0)
    expect(countTabsFittingWidth(-10)).toBe(0)
  })

  it('counts how many min-width tabs fit including gaps', () => {
    expect(countTabsFittingWidth(widthForTabs(1))).toBe(1)
    expect(countTabsFittingWidth(widthForTabs(4))).toBe(4)
    expect(countTabsFittingWidth(widthForTabs(4) - 1)).toBe(3)
  })
})

describe('calculateMaxVisibleChatTabs', () => {
  it('shows every tab when they fit without an overflow button', () => {
    expect(calculateMaxVisibleChatTabs(widthForTabs(5), 5)).toBe(5)
    expect(calculateMaxVisibleChatTabs(widthForTabs(8), 3)).toBe(3)
  })

  it('reserves overflow chrome once tabs no longer fit', () => {
    const stripWidth = widthForTabs(5)
    expect(calculateMaxVisibleChatTabs(stripWidth, 6)).toBe(4)

    const widthForThreeTabsPlusOverflow =
      widthForTabs(3) + CHAT_TAB_GAP_PX + CHAT_TAB_OVERFLOW_BUTTON_WIDTH_PX
    expect(calculateMaxVisibleChatTabs(widthForThreeTabsPlusOverflow, 10)).toBe(3)
  })

  it('scales with wider strips instead of a fixed cap', () => {
    expect(calculateMaxVisibleChatTabs(widthForTabs(12), 20)).toBe(11)
  })
})

describe('resolveTabVisibility', () => {
  it('shows every tab before the strip has been measured', () => {
    expect(resolveTabVisibility(['a', 'b', 'c', 'd'], [], 'b', 0)).toEqual({
      visibleTabIds: ['a', 'b', 'c', 'd'],
      overflowTabIds: [],
      maxVisibleTabs: 4
    })
  })

  it('shows every tab when they fit in the measured strip', () => {
    expect(resolveTabVisibility(['a', 'b', 'c'], [], 'b', widthForTabs(8))).toEqual({
      visibleTabIds: ['a', 'b', 'c'],
      overflowTabIds: [],
      maxVisibleTabs: 3
    })
  })

  it('keeps pinned tabs visible and moves the rest into overflow', () => {
    const openTabIds = ['a', 'b', 'c', 'd', 'e', 'f', 'g']
    const pinnedTabIds = ['c', 'a']
    const tabStripWidthPx = widthForTabs(5)

    const result = resolveTabVisibility(openTabIds, pinnedTabIds, 'g', tabStripWidthPx)

    expect(result.maxVisibleTabs).toBe(4)
    expect(result.visibleTabIds).toEqual(['a', 'b', 'c', 'g'])
    expect(result.overflowTabIds).toEqual(['d', 'e', 'f'])
  })

  it('caps pins at the configured maximum', () => {
    const openTabIds = ['a', 'b', 'c', 'd', 'e', 'f']
    const pinnedTabIds = ['a', 'b', 'c', 'd']
    const tabStripWidthPx = widthForTabs(5)

    const result = resolveTabVisibility(openTabIds, pinnedTabIds, 'f', tabStripWidthPx)

    expect(result.visibleTabIds.filter((id) => pinnedTabIds.includes(id))).toHaveLength(
      MAX_PINNED_TABS
    )
    expect(result.visibleTabIds).toContain('f')
  })
})
