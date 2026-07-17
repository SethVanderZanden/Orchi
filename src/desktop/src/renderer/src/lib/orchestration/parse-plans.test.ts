import { describe, expect, it } from 'vitest'

import {
  parseOrchestrationPlansFromMessages,
  parsePlans,
  parsePlansFromMessages
} from './parse-plans'

describe('parsePlans', () => {
  it('extracts plan blocks with ids and titles', () => {
    const content = `
Some intro text.

<!-- orchi-plan:auth-refactor -->
# Auth Refactor

## Summary
Refactor auth.

## Tasks
- [ ] Add JWT
<!-- /orchi-plan -->

<!-- orchi-plan:ui-polish -->
# UI Polish

Polish the UI.
<!-- /orchi-plan -->
`

    const plans = parsePlans(content)

    expect(plans).toHaveLength(2)
    expect(plans[0]).toEqual({
      planId: 'auth-refactor',
      title: 'Auth Refactor',
      contentMarkdown: expect.stringContaining('## Summary')
    })
    expect(plans[1]?.planId).toBe('ui-polish')
  })

  it('dedupes plans by id keeping the latest in message order', () => {
    const messages = [
      {
        role: 'assistant',
        content: `<!-- orchi-plan:auth-refactor -->
# Auth Refactor v1
<!-- /orchi-plan -->`
      },
      {
        role: 'assistant',
        content: `<!-- orchi-plan:auth-refactor -->
# Auth Refactor v2
<!-- /orchi-plan -->`
      }
    ]

    const plans = parsePlansFromMessages(messages)

    expect(plans).toHaveLength(1)
    expect(plans[0]?.title).toBe('Auth Refactor v2')
  })
})

describe('parseOrchestrationPlansFromMessages', () => {
  it('returns plans and sequence metadata from assistant messages', () => {
    const messages = [
      {
        role: 'assistant',
        content: `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->

<!-- orchi-plan:ui-polish -->
# UI Polish
<!-- /orchi-plan -->

<!-- orchi-plan-sequence -->
auth-refactor
ui-polish
<!-- /orchi-plan-sequence -->`
      }
    ]

    const result = parseOrchestrationPlansFromMessages(messages)

    expect(result.plans).toHaveLength(2)
    expect(result.sequencePlanIds).toEqual(['auth-refactor', 'ui-polish'])
  })
})
