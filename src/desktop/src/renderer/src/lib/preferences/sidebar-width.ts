export const SIDEBAR_DEFAULT_WIDTH = 300
export const SIDEBAR_MIN_WIDTH = 240
export const SIDEBAR_MAX_WIDTH = 520

const STORAGE_KEY = 'orchi.sidebarWidth.v1'

export function clampSidebarWidth(width: number): number {
  return Math.min(SIDEBAR_MAX_WIDTH, Math.max(SIDEBAR_MIN_WIDTH, width))
}

export function getSidebarWidth(): number {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return SIDEBAR_DEFAULT_WIDTH
    }

    const parsed = Number.parseInt(raw, 10)
    if (!Number.isFinite(parsed)) {
      return SIDEBAR_DEFAULT_WIDTH
    }

    return clampSidebarWidth(parsed)
  } catch {
    return SIDEBAR_DEFAULT_WIDTH
  }
}

export function setSidebarWidth(width: number): void {
  try {
    localStorage.setItem(STORAGE_KEY, String(clampSidebarWidth(width)))
  } catch {
    // ignore
  }
}
