export const REVIEW_PANEL_DEFAULT_WIDTH = 840
export const REVIEW_PANEL_MIN_WIDTH = 300
export const REVIEW_PANEL_MAX_WIDTH_FRACTION = 0.6
export const CHAT_MIN_WIDTH = 320

export function getReviewPanelWidthBounds(containerWidth: number): { min: number; max: number } {
  if (containerWidth <= 0) {
    return {
      min: REVIEW_PANEL_MIN_WIDTH,
      max: REVIEW_PANEL_DEFAULT_WIDTH
    }
  }

  const maxWidth = Math.min(
    containerWidth * REVIEW_PANEL_MAX_WIDTH_FRACTION,
    containerWidth - CHAT_MIN_WIDTH
  )

  return {
    min: REVIEW_PANEL_MIN_WIDTH,
    max: Math.max(REVIEW_PANEL_MIN_WIDTH, maxWidth)
  }
}

export function clampReviewPanelWidth(width: number, containerWidth: number): number {
  const { min, max } = getReviewPanelWidthBounds(containerWidth)
  return Math.min(max, Math.max(min, width))
}
