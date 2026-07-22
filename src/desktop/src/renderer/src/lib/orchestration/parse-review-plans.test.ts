import { describe, expect, it } from 'vitest'

import { parseReviewPlans, parseReviewPlansFromMessages } from './parse-review-plans'

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
})
