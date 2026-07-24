import { useCallback, useEffect, useState } from 'react'

import {
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
    function onStorage(event: StorageEvent): void {
      if (event.key !== BOARD_GROUPING_STORAGE_KEY) {
        return
      }

      setGroupingState(getBoardGrouping())
    }

    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
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
