import { useCallback, useState } from 'react'

import type { BoardDateRange, BoardFilters, BoardProjectFilter } from '@/lib/kanban/board-filters'
import { getBoardFilters, setBoardFilters } from '@/lib/preferences/board-filters'

type UseBoardFiltersResult = {
  filters: BoardFilters
  setProjectFilter: (projectFilter: BoardProjectFilter) => void
  setDateRange: (dateRange: BoardDateRange) => void
}

export function useBoardFilters(): UseBoardFiltersResult {
  const [filters, setFiltersState] = useState<BoardFilters>(() => getBoardFilters())

  const setProjectFilter = useCallback((projectFilter: BoardProjectFilter) => {
    setFiltersState((current) => {
      const next = { ...current, projectFilter }
      setBoardFilters(next)
      return next
    })
  }, [])

  const setDateRange = useCallback((dateRange: BoardDateRange) => {
    setFiltersState((current) => {
      const next = { ...current, dateRange }
      setBoardFilters(next)
      return next
    })
  }, [])

  return {
    filters,
    setProjectFilter,
    setDateRange
  }
}
