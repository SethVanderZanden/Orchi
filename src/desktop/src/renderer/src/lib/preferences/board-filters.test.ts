import { beforeEach, describe, expect, it } from 'vitest'

import { getDefaultBoardFilters } from '@/lib/kanban/board-filters'
import {
  BOARD_FILTERS_STORAGE_KEY,
  getBoardFilters,
  setBoardFilters
} from '@/lib/preferences/board-filters'

describe('board filter preferences', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('returns defaults when storage is empty', () => {
    expect(getBoardFilters()).toEqual(getDefaultBoardFilters())
  })

  it('persists valid filters', () => {
    setBoardFilters({ projectFilter: 'project-1', dateRange: 'last7Days' })

    expect(localStorage.getItem(BOARD_FILTERS_STORAGE_KEY)).toBeTruthy()
    expect(getBoardFilters()).toEqual({ projectFilter: 'project-1', dateRange: 'last7Days' })
  })

  it('falls back to defaults for invalid stored values', () => {
    localStorage.setItem(
      BOARD_FILTERS_STORAGE_KEY,
      JSON.stringify({ projectFilter: '', dateRange: 'bad' })
    )

    expect(getBoardFilters()).toEqual(getDefaultBoardFilters())
  })

  it('migrates legacy no-project filter to all projects', () => {
    localStorage.setItem(
      BOARD_FILTERS_STORAGE_KEY,
      JSON.stringify({ projectFilter: 'none', dateRange: 'last7Days' })
    )

    expect(getBoardFilters()).toEqual({ projectFilter: 'all', dateRange: 'last7Days' })
  })
})
