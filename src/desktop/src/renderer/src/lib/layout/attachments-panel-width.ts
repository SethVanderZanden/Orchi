export const ATTACHMENTS_PANEL_MIN_WIDTH = 280

export const ATTACHMENTS_PANEL_MAX_RATIO = 0.45

export const ATTACHMENTS_PANEL_DEFAULT_WIDTH = 360

export function getAttachmentsPanelWidthBounds(containerWidth: number): {
  min: number
  max: number
} {
  const max = Math.max(
    ATTACHMENTS_PANEL_MIN_WIDTH,
    Math.floor(containerWidth * ATTACHMENTS_PANEL_MAX_RATIO)
  )

  return {
    min: ATTACHMENTS_PANEL_MIN_WIDTH,
    max
  }
}

export function clampAttachmentsPanelWidth(width: number, containerWidth: number): number {
  const { min, max } = getAttachmentsPanelWidthBounds(containerWidth)
  return Math.min(max, Math.max(min, width))
}
