import { describe, expect, it } from 'vitest'

import {
  CHAT_MIN_WIDTH,
  REVIEW_PANEL_DEFAULT_WIDTH,
  REVIEW_PANEL_MAX_WIDTH_FRACTION,
  REVIEW_PANEL_MIN_WIDTH,
  clampReviewPanelWidth,
  getReviewPanelWidthBounds
} from '@/lib/layout/review-panel-width'

describe('getReviewPanelWidthBounds', () => {
  it('returns default max when container width is unknown', () => {
    expect(getReviewPanelWidthBounds(0)).toEqual({
      min: REVIEW_PANEL_MIN_WIDTH,
      max: REVIEW_PANEL_DEFAULT_WIDTH
    })
  })

  it('caps at 60% of container width on wide layouts', () => {
    const containerWidth = 1200
    const { max } = getReviewPanelWidthBounds(containerWidth)

    expect(max).toBe(containerWidth * REVIEW_PANEL_MAX_WIDTH_FRACTION)
  })

  it('respects chat minimum width before the 60% cap', () => {
    const containerWidth = 700
    const { max } = getReviewPanelWidthBounds(containerWidth)

    expect(max).toBe(containerWidth - CHAT_MIN_WIDTH)
  })

  it('never returns a max below the review panel minimum', () => {
    const { max } = getReviewPanelWidthBounds(REVIEW_PANEL_MIN_WIDTH + CHAT_MIN_WIDTH - 1)

    expect(max).toBe(REVIEW_PANEL_MIN_WIDTH)
  })
})

describe('clampReviewPanelWidth', () => {
  it('clamps to the computed max', () => {
    expect(clampReviewPanelWidth(900, 900)).toBe(900 * REVIEW_PANEL_MAX_WIDTH_FRACTION)
  })

  it('clamps to the computed min', () => {
    expect(clampReviewPanelWidth(100, 900)).toBe(REVIEW_PANEL_MIN_WIDTH)
  })

  it('preserves width within bounds', () => {
    expect(clampReviewPanelWidth(450, 900)).toBe(450)
  })
})
