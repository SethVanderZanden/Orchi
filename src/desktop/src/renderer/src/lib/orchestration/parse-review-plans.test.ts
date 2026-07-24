import { describe, expect, it } from 'vitest'

import { parseReviewPlans, parseReviewPlansFromMessages, resolveReviewContentFromMessages } from './parse-review-plans'

describe('parseReviewPlans', () => {
  it('extracts review plan blocks with ids and titles', () => {
    const content = `
Some intro text.

<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review

## Review TLDR
- Verdict: ship with fixes
- Check JWT expiry edge case

## Findings
### Oversights
- Missing refresh-token test
<!-- /orchi-review-plan -->
`

    const plans = parseReviewPlans(content)

    expect(plans).toHaveLength(1)
    expect(plans[0]).toEqual({
      planId: 'auth-refactor',
      title: 'Auth Refactor Review',
      contentMarkdown: expect.stringContaining('Review TLDR')
    })
  })

  it('parses review plans when the opening marker is inline with preamble text', () => {
    const content = `I'm reviewing the supplied diff against the branch intent, with emphasis on test coverage and whether the test-fixture changes preserve the production paths they are meant to exercise.<!-- orchi-review-plan:crew-sheet-run-sheet-order -->
# Crew sheet run sheet order

## Review TLDR
- Verdict: ship with fixes
`

    const plans = parseReviewPlans(content)

    expect(plans).toHaveLength(1)
    expect(plans[0]).toEqual({
      planId: 'crew-sheet-run-sheet-order',
      title: 'Crew sheet run sheet order',
      contentMarkdown: expect.stringContaining('Review TLDR')
    })
  })

  it('parses incomplete review plans without a closing marker', () => {
    const content = `<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review

## Review TLDR
- Verdict: ship with fixes`

    const plans = parseReviewPlans(content)

    expect(plans).toHaveLength(1)
    expect(plans[0]?.planId).toBe('auth-refactor')
    expect(plans[0]?.contentMarkdown).toContain('Review TLDR')
  })

  it('dedupes review plans by id keeping the latest in message order', () => {
    const messages = [
      {
        role: 'assistant',
        content: `<!-- orchi-review-plan:auth-refactor -->
# Review v1
<!-- /orchi-review-plan -->`
      },
      {
        role: 'assistant',
        content: `<!-- orchi-review-plan:auth-refactor -->
# Review v2
<!-- /orchi-review-plan -->`
      }
    ]

    const plans = parseReviewPlansFromMessages(messages)

    expect(plans).toHaveLength(1)
    expect(plans[0]?.title).toBe('Review v2')
  })

  it('falls back to the latest complete assistant message when no review blocks exist', () => {
    const review = resolveReviewContentFromMessages([
      {
        role: 'user',
        content: 'Begin review.',
        status: 'complete'
      },
      {
        role: 'assistant',
        content: `# Auth Refactor Review

## Review TLDR
- Verdict: ship`,
        status: 'complete'
      }
    ], 'auth-refactor')

    expect(review).toMatchObject({
      planId: 'auth-refactor',
      title: 'Auth Refactor Review',
      contentMarkdown: expect.stringContaining('Review TLDR')
    })
  })
})
