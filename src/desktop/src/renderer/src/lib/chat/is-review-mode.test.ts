import { describe, expect, it } from 'vitest'

import { isReviewFamilyMode } from '@/lib/chat/is-review-mode'

describe('isReviewFamilyMode', () => {
  it('returns true for review and branch-review', () => {
    expect(isReviewFamilyMode('review')).toBe(true)
    expect(isReviewFamilyMode('branch-review')).toBe(true)
    expect(isReviewFamilyMode('REVIEW')).toBe(true)
    expect(isReviewFamilyMode('Branch-Review')).toBe(true)
  })

  it('returns false for other modes and empty values', () => {
    expect(isReviewFamilyMode('default')).toBe(false)
    expect(isReviewFamilyMode('orchestration')).toBe(false)
    expect(isReviewFamilyMode(null)).toBe(false)
    expect(isReviewFamilyMode(undefined)).toBe(false)
  })
})
