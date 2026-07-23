/** Minimum default width — wider than the old 900px shell for sidebar + chat. */
export const DEFAULT_WINDOW_MIN_WIDTH = 1200

/** Minimum default height — a bit taller than the old 670px shell. */
export const DEFAULT_WINDOW_MIN_HEIGHT = 780

/** Cap so the window does not dominate ultra-wide / 4K displays. */
export const DEFAULT_WINDOW_MAX_WIDTH = 1600

/** Cap so the window does not dominate tall displays. */
export const DEFAULT_WINDOW_MAX_HEIGHT = 1000

/** Fraction of the monitor work area used for the preferred width. */
export const DEFAULT_WINDOW_WIDTH_RATIO = 0.78

/** Fraction of the monitor work area used for the preferred height. */
export const DEFAULT_WINDOW_HEIGHT_RATIO = 0.82

export type WorkAreaSize = {
  width: number
  height: number
}

export type WindowSize = {
  width: number
  height: number
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value))
}

/**
 * Picks an initial BrowserWindow size from the primary display work area:
 * prefer ~78%×82% of available space, floored above the old 900×670 defaults,
 * and capped so large monitors do not open an oversized window.
 */
export function getDefaultWindowSize(workArea: WorkAreaSize): WindowSize {
  const availableWidth = Math.max(1, Math.floor(workArea.width))
  const availableHeight = Math.max(1, Math.floor(workArea.height))

  const preferredWidth = Math.round(availableWidth * DEFAULT_WINDOW_WIDTH_RATIO)
  const preferredHeight = Math.round(availableHeight * DEFAULT_WINDOW_HEIGHT_RATIO)

  return {
    width: clamp(
      preferredWidth,
      Math.min(DEFAULT_WINDOW_MIN_WIDTH, availableWidth),
      Math.min(DEFAULT_WINDOW_MAX_WIDTH, availableWidth)
    ),
    height: clamp(
      preferredHeight,
      Math.min(DEFAULT_WINDOW_MIN_HEIGHT, availableHeight),
      Math.min(DEFAULT_WINDOW_MAX_HEIGHT, availableHeight)
    )
  }
}
