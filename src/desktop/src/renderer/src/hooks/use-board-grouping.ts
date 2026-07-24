import { useCallback, useEffect, useState } from 'react'

import {
  BOARD_GROUPING_CHANGED_EVENT,
  BOARD_GROUPING_STORAGE_KEY,
  getBoardGrouping,
  setBoardGrouping,
  type BoardGroupingMode
} from '@/lib/preferences/board-grouping'

type UseBoardGroupingResult = {
  grouping: BoardGroupingMode
  setGrouping: (mode: BoardGroupingMode) => void
}

export function useBoardGrouping(): UseBoardGroupingResult {
  const [grouping, setGroupingState] = useState<BoardGroupingMode>(() => getBoardGrouping())

  useEffect(() => {
    function syncFromStorage(): void {
      setGroupingState(getBoardGrouping())
    }

    function onStorage(event: StorageEvent): void {
      if (event.key !== BOARD_GROUPING_STORAGE_KEY) {
        return
      }

      syncFromStorage()
    }

    window.addEventListener('storage', onStorage)
    window.addEventListener(BOARD_GROUPING_CHANGED_EVENT, syncFromStorage)
    return () => {
      window.removeEventListener('storage', onStorage)
      window.removeEventListener(BOARD_GROUPING_CHANGED_EVENT, syncFromStorage)
    }
  }, [])

  const setGrouping = useCallback((mode: BoardGroupingMode) => {
    setBoardGrouping(mode)
    setGroupingState(mode)
  }, [])

  return {
    grouping,
    setGrouping
  }
}
