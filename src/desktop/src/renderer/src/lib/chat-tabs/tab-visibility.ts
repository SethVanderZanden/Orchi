export const MAX_PINNED_TABS = 3

/**
 * Minimum width reserved per visible tab chip.
 * Keep in sync with `min-w` on `ChatTab`.
 */
export const CHAT_TAB_MIN_WIDTH_PX = 140
export const CHAT_TAB_OVERFLOW_BUTTON_WIDTH_PX = 52
export const CHAT_TAB_GAP_PX = 2

export type TabVisibility = {
  visibleTabIds: string[]
  overflowTabIds: string[]
  maxVisibleTabs: number
}

/** How many tab slots fit in `availableWidthPx` at the configured min width + gap. */
export function countTabsFittingWidth(availableWidthPx: number): number {
  if (availableWidthPx <= 0) {
    return 0
  }

  const slotPx = CHAT_TAB_MIN_WIDTH_PX + CHAT_TAB_GAP_PX
  return Math.max(0, Math.floor((availableWidthPx + CHAT_TAB_GAP_PX) / slotPx))
}

/**
 * Max tabs to show inline for a measured strip width.
 * Reserves space for the overflow chevron whenever more tabs exist than fit.
 */
export function calculateMaxVisibleChatTabs(tabStripWidthPx: number, openTabCount: number): number {
  if (tabStripWidthPx <= 0 || openTabCount <= 0) {
    return Math.max(openTabCount, 0)
  }

  const maxWithoutOverflow = countTabsFittingWidth(tabStripWidthPx)
  if (openTabCount <= maxWithoutOverflow) {
    return openTabCount
  }

  const widthForTabs = tabStripWidthPx - CHAT_TAB_OVERFLOW_BUTTON_WIDTH_PX - CHAT_TAB_GAP_PX
  return Math.max(1, countTabsFittingWidth(widthForTabs))
}

export function resolveTabVisibility(
  openTabIds: string[],
  pinnedTabIds: string[],
  activeTabId: string | null,
  tabStripWidthPx: number
): TabVisibility {
  // Before the strip is measured, show every tab to avoid a false overflow flash.
  if (tabStripWidthPx <= 0) {
    return {
      visibleTabIds: openTabIds,
      overflowTabIds: [],
      maxVisibleTabs: openTabIds.length
    }
  }

  const maxVisibleTabs = calculateMaxVisibleChatTabs(tabStripWidthPx, openTabIds.length)

  if (openTabIds.length <= maxVisibleTabs) {
    return {
      visibleTabIds: openTabIds,
      overflowTabIds: [],
      maxVisibleTabs: openTabIds.length
    }
  }

  const validPins = openTabIds.filter((id) => pinnedTabIds.includes(id)).slice(0, MAX_PINNED_TABS)
  const visible = new Set<string>(validPins)

  if (activeTabId && openTabIds.includes(activeTabId)) {
    visible.add(activeTabId)
  }

  for (const id of openTabIds) {
    if (visible.size >= maxVisibleTabs) {
      break
    }

    visible.add(id)
  }

  const visibleTabIds = openTabIds.filter((id) => visible.has(id))
  const overflowTabIds = openTabIds.filter((id) => !visible.has(id))

  return { visibleTabIds, overflowTabIds, maxVisibleTabs }
}
