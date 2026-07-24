export type BoardGroupingMode = 'state' | 'project'

const STORAGE_KEY = 'orchi.boardGrouping'
const DEFAULT_BOARD_GROUPING: BoardGroupingMode = 'state'

/** Dispatched in the same window when grouping changes (storage events are cross-tab only). */
export const BOARD_GROUPING_CHANGED_EVENT = 'orchi:board-grouping-changed'

export function isBoardGroupingMode(value: unknown): value is BoardGroupingMode {
  return value === 'state' || value === 'project'
}

export function getBoardGrouping(): BoardGroupingMode {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (isBoardGroupingMode(raw)) {
      return raw
    }
  } catch {
    // ignore storage failures (private mode, etc.)
  }

  return DEFAULT_BOARD_GROUPING
}

export function setBoardGrouping(mode: BoardGroupingMode): void {
  try {
    localStorage.setItem(STORAGE_KEY, mode)
  } catch {
    // ignore
  }

  window.dispatchEvent(new Event(BOARD_GROUPING_CHANGED_EVENT))
}

export function getBoardGroupingLabel(mode: BoardGroupingMode): string {
  return mode === 'project' ? 'Group by project' : 'Group by state'
}

export const BOARD_GROUPING_STORAGE_KEY = STORAGE_KEY
