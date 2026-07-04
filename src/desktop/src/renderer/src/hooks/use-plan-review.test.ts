import { describe, expect, it } from 'vitest'

import { initialReviewState, reviewReducer } from '@/hooks/use-plan-review'

// Export initial state for tests - need to export it from hook file
// Actually initialReviewState is not exported - let me use inline initial state in test or export it

describe('reviewReducer', () => {
  it('toggle-panel opens panel with first plan when closed', () => {
    const result = reviewReducer(initialReviewState, {
      type: 'toggle-panel',
      planId: 'plan-1'
    })

    expect(result).toEqual({
      panelOpen: true,
      openTabIds: ['plan-1'],
      activeTabId: 'plan-1'
    })
  })

  it('toggle-panel closes panel when open', () => {
    const state = {
      panelOpen: true,
      openTabIds: ['plan-1'],
      activeTabId: 'plan-1'
    }

    const result = reviewReducer(state, { type: 'toggle-panel', planId: 'plan-1' })

    expect(result.panelOpen).toBe(false)
    expect(result.openTabIds).toEqual(['plan-1'])
  })

  it('highlight-review opens panel and selects plan', () => {
    const result = reviewReducer(initialReviewState, {
      type: 'highlight-review',
      planId: 'plan-2'
    })

    expect(result).toEqual({
      panelOpen: true,
      openTabIds: ['plan-2'],
      activeTabId: 'plan-2'
    })
  })
})
