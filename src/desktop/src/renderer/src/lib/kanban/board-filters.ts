import type { ChatThread } from '@/lib/chat/types'

export type BoardDateRange =
  | 'all'
  | 'lastHour'
  | 'last9Hours'
  | 'last24Hours'
  | 'last3Days'
  | 'last7Days'

/** `all` shows every project; otherwise a project id. */
export type BoardProjectFilter = 'all' | (string & {})

export type BoardFilters = {
  projectFilter: BoardProjectFilter
  dateRange: BoardDateRange
}

export const DEFAULT_BOARD_DATE_RANGE: BoardDateRange = 'last24Hours'
export const DEFAULT_BOARD_PROJECT_FILTER: BoardProjectFilter = 'all'

export const BOARD_DATE_RANGE_OPTIONS: ReadonlyArray<{
  value: BoardDateRange
  label: string
}> = [
  { value: 'lastHour', label: 'Last hour' },
  { value: 'last9Hours', label: 'Last 9 hours' },
  { value: 'last24Hours', label: 'Last 24 hours' },
  { value: 'last3Days', label: 'Last 3 days' },
  { value: 'last7Days', label: 'Last 7 days' },
  { value: 'all', label: 'All' }
]

const BOARD_DATE_RANGE_SET = new Set<BoardDateRange>(BOARD_DATE_RANGE_OPTIONS.map((o) => o.value))

export function isBoardDateRange(value: unknown): value is BoardDateRange {
  return typeof value === 'string' && BOARD_DATE_RANGE_SET.has(value as BoardDateRange)
}

export function isBoardProjectFilter(value: unknown): value is BoardProjectFilter {
  return value === 'all' || (typeof value === 'string' && value.length > 0)
}

export function getBoardDateRangeLabel(dateRange: BoardDateRange): string {
  return BOARD_DATE_RANGE_OPTIONS.find((option) => option.value === dateRange)?.label ?? dateRange
}

export function getDateCutoff(dateRange: BoardDateRange, now: Date = new Date()): Date | null {
  switch (dateRange) {
    case 'all':
      return null
    case 'lastHour':
      return new Date(now.getTime() - 60 * 60 * 1000)
    case 'last9Hours':
      return new Date(now.getTime() - 9 * 60 * 60 * 1000)
    case 'last24Hours':
      return new Date(now.getTime() - 24 * 60 * 60 * 1000)
    case 'last3Days':
      return new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000)
    case 'last7Days':
      return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000)
  }
}

export function matchesBoardProjectFilter(
  chat: ChatThread,
  projectFilter: BoardProjectFilter
): boolean {
  if (projectFilter === 'all') {
    return true
  }

  return chat.projectId === projectFilter
}

export function matchesBoardDateRange(
  chat: ChatThread,
  dateRange: BoardDateRange,
  now: Date = new Date()
): boolean {
  const cutoff = getDateCutoff(dateRange, now)
  if (cutoff === null) {
    return true
  }

  return new Date(chat.updatedAt).getTime() >= cutoff.getTime()
}

export function filterBoardChats(
  chats: ChatThread[],
  filters: BoardFilters,
  now: Date = new Date()
): ChatThread[] {
  return chats.filter(
    (chat) =>
      matchesBoardProjectFilter(chat, filters.projectFilter) &&
      matchesBoardDateRange(chat, filters.dateRange, now)
  )
}

export function getDefaultBoardFilters(): BoardFilters {
  return {
    projectFilter: DEFAULT_BOARD_PROJECT_FILTER,
    dateRange: DEFAULT_BOARD_DATE_RANGE
  }
}
