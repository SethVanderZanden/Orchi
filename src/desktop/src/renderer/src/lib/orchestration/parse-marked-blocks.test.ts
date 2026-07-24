import { describe, expect, it } from 'vitest'

import { ORCHI_MARKERS, getIdBlockParseConfig } from './orchi-markers'
import { parseMarkedBlocks } from './parse-marked-blocks'

describe('parseMarkedBlocks', () => {
  const reviewPlanConfig = getIdBlockParseConfig(ORCHI_MARKERS.reviewPlan)

  it('extracts complete review blocks', () => {
    const content = `<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review
<!-- /orchi-review-plan -->`

    expect(parseMarkedBlocks(content, reviewPlanConfig)).toEqual([
      {
        id: 'auth-refactor',
        body: '# Auth Refactor Review'
      }
    ])
  })

  it('extracts incomplete review blocks without a closing marker', () => {
    const content = `I'm reviewing the supplied diff.<!-- orchi-review-plan:crew-sheet-run-sheet-order -->
# Crew sheet order

## Review TLDR
- Verdict: ship with fixes`

    expect(parseMarkedBlocks(content, reviewPlanConfig)).toEqual([
      {
        id: 'crew-sheet-run-sheet-order',
        body: `# Crew sheet order

## Review TLDR
- Verdict: ship with fixes`
      }
    ])
  })

  it('prefers complete blocks when both complete and open markers exist', () => {
    const content = `<!-- orchi-review-plan:auth-refactor -->
# Complete title
<!-- /orchi-review-plan -->

<!-- orchi-review-plan:auth-refactor -->
# Incomplete title`

    expect(parseMarkedBlocks(content, reviewPlanConfig)).toEqual([
      {
        id: 'auth-refactor',
        body: '# Complete title'
      }
    ])
  })

  it('splits multiple incomplete review blocks by the next opening marker', () => {
    const content = `<!-- orchi-review-plan:alpha -->
# Alpha

<!-- orchi-review-plan:beta -->
# Beta`

    expect(parseMarkedBlocks(content, reviewPlanConfig)).toEqual([
      { id: 'alpha', body: '# Alpha' },
      { id: 'beta', body: '# Beta' }
    ])
  })

  it('ignores preamble before the opening marker', () => {
    const content = `Some intro text.

<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review
<!-- /orchi-review-plan -->`

    expect(parseMarkedBlocks(content, reviewPlanConfig)[0]?.body).toContain('# Auth Refactor Review')
    expect(parseMarkedBlocks(content, reviewPlanConfig)[0]?.body).not.toContain('Some intro text')
  })
})
