import { useCallback, useState } from 'react'

import {
  clampSidebarWidth,
  getSidebarWidth,
  setSidebarWidth as persistSidebarWidth
} from '@/lib/preferences/sidebar-width'

type UseSidebarWidthResult = {
  sidebarWidth: number
  setSidebarWidth: (width: number) => void
}

export function useSidebarWidth(): UseSidebarWidthResult {
  const [sidebarWidth, setSidebarWidthState] = useState(() => getSidebarWidth())

  const setSidebarWidth = useCallback((width: number) => {
    const next = clampSidebarWidth(width)
    persistSidebarWidth(next)
    setSidebarWidthState(next)
  }, [])

  return {
    sidebarWidth,
    setSidebarWidth
  }
}
