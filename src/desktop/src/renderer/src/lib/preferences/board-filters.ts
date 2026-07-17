import {
  getDefaultBoardFilters,
  isBoardDateRange,
  isBoardProjectFilter,
  type BoardFilters
} from '@/lib/kanban/board-filters'

const STORAGE_KEY = 'orchi.boardFilters'

export function getBoardFilters(): BoardFilters {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return getDefaultBoardFilters()
    }

    const parsed = JSON.parse(raw) as Partial<BoardFilters>
    const projectFilter =
      parsed.projectFilter === 'none' || !isBoardProjectFilter(parsed.projectFilter)
        ? getDefaultBoardFilters().projectFilter
        : parsed.projectFilter
    const dateRange = isBoardDateRange(parsed.dateRange)
      ? parsed.dateRange
      : getDefaultBoardFilters().dateRange

    return { projectFilter, dateRange }
  } catch {
    return getDefaultBoardFilters()
  }
}

export function setBoardFilters(filters: BoardFilters): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(filters))
  } catch {
    // ignore storage failures (private mode, etc.)
  }
}

export { STORAGE_KEY as BOARD_FILTERS_STORAGE_KEY }
