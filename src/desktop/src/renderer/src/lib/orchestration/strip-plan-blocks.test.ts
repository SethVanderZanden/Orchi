import { describe, expect, it } from 'vitest'

import { stripPlanBlocksForChatDisplay, stripReviewPlanBlocksForChatDisplay } from './strip-plan-blocks'

describe('stripPlanBlocksForChatDisplay', () => {
  it('removes complete plan and sequence blocks, keeping surrounding prose', () => {
    const content = `Here is the breakdown.

<!-- orchi-plan:auth-refactor -->
# Auth Refactor

## Summary
Refactor auth.
<!-- /orchi-plan -->

<!-- orchi-plan-sequence -->
auth-refactor
<!-- /orchi-plan-sequence -->

Ready for review.`

    expect(stripPlanBlocksForChatDisplay(content)).toBe(
      'Here is the breakdown.\n\nReady for review.'
    )
  })

  it('truncates incomplete plan blocks while streaming', () => {
    const content = `Intro text.

<!-- orchi-plan:auth-refactor -->
# Auth Refactor

## Summary
Still streaming`

    expect(stripPlanBlocksForChatDisplay(content)).toBe('Intro text.')
  })

  it('truncates incomplete sequence blocks while streaming', () => {
    const content = `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->

<!-- orchi-plan-sequence -->
auth-refactor`

    expect(stripPlanBlocksForChatDisplay(content)).toBe('')
  })

  it('returns empty string when content is only plan blocks', () => {
    const content = `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->`

    expect(stripPlanBlocksForChatDisplay(content)).toBe('')
  })

  it('leaves non-plan content unchanged', () => {
    expect(stripPlanBlocksForChatDisplay('Just a normal reply.')).toBe('Just a normal reply.')
  })
})

describe('stripReviewPlanBlocksForChatDisplay', () => {
  it('removes complete review plan blocks, keeping surrounding prose', () => {
    const content = `Intro.

<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review
<!-- /orchi-review-plan -->

Done.`

    expect(stripReviewPlanBlocksForChatDisplay(content)).toBe('Intro.\n\nDone.')
  })

  it('truncates incomplete review plan blocks while streaming', () => {
    const content = `I'm reviewing the supplied diff.<!-- orchi-review-plan:crew-sheet-run-sheet-order -->
# Crew sheet run sheet order`

    expect(stripReviewPlanBlocksForChatDisplay(content)).toBe(
      "I'm reviewing the supplied diff."
    )
  })
})
