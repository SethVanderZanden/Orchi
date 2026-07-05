import { describe, expect, it } from 'vitest'

import type { ParsedPlan } from './parse-plans'
import {
  getSequenceStepNumber,
  hasSequentialKickoff,
  parsePlanSequence,
  parsePlanSequenceFromMessages,
  resolveKickoffGroups
} from './plan-sequence'

const samplePlans: ParsedPlan[] = [
  { planId: 'auth-refactor', title: 'Auth', contentMarkdown: '# Auth' },
  { planId: 'ui-polish', title: 'UI', contentMarkdown: '# UI' },
  { planId: 'api-layer', title: 'API', contentMarkdown: '# API' }
]

describe('parsePlanSequence', () => {
  it('parses plan IDs from a sequence block', () => {
    const content = `
<!-- orchi-plan-sequence -->
auth-refactor
ui-polish
<!-- /orchi-plan-sequence -->
`

    expect(parsePlanSequence(content)).toEqual(['auth-refactor', 'ui-polish'])
  })

  it('ignores blank lines and markdown list prefixes', () => {
    const content = `<!-- orchi-plan-sequence -->

- auth-refactor

ui-polish

<!-- /orchi-plan-sequence -->`

    expect(parsePlanSequence(content)).toEqual(['auth-refactor', 'ui-polish'])
  })

  it('dedupes IDs while preserving first occurrence', () => {
    const content = `<!-- orchi-plan-sequence -->
auth-refactor
ui-polish
auth-refactor
<!-- /orchi-plan-sequence -->`

    expect(parsePlanSequence(content)).toEqual(['auth-refactor', 'ui-polish'])
  })

  it('returns null when no sequence block is present', () => {
    expect(parsePlanSequence('No sequence here')).toBeNull()
  })

  it('uses the latest block when multiple blocks exist in one message', () => {
    const content = `<!-- orchi-plan-sequence -->
auth-refactor
<!-- /orchi-plan-sequence -->

<!-- orchi-plan-sequence -->
ui-polish
api-layer
<!-- /orchi-plan-sequence -->`

    expect(parsePlanSequence(content)).toEqual(['ui-polish', 'api-layer'])
  })
})

describe('parsePlanSequenceFromMessages', () => {
  it('uses the latest assistant message that contains a sequence block', () => {
    const messages = [
      {
        role: 'assistant',
        content: `<!-- orchi-plan-sequence -->
auth-refactor
<!-- /orchi-plan-sequence -->`
      },
      {
        role: 'user',
        content: 'Revise the order'
      },
      {
        role: 'assistant',
        content: `<!-- orchi-plan-sequence -->
api-layer
ui-polish
<!-- /orchi-plan-sequence -->`
      }
    ]

    expect(parsePlanSequenceFromMessages(messages)).toEqual(['api-layer', 'ui-polish'])
  })

  it('keeps the previous sequence when a later assistant message has no block', () => {
    const messages = [
      {
        role: 'assistant',
        content: `<!-- orchi-plan-sequence -->
auth-refactor
<!-- /orchi-plan-sequence -->`
      },
      {
        role: 'assistant',
        content: 'Here is an update without a sequence block.'
      }
    ]

    expect(parsePlanSequenceFromMessages(messages)).toEqual(['auth-refactor'])
  })
})

describe('resolveKickoffGroups', () => {
  it('partitions plans into sequenced and independent groups', () => {
    const { sequenced, independent } = resolveKickoffGroups(samplePlans, [
      'auth-refactor',
      'unknown-plan',
      'api-layer'
    ])

    expect(sequenced.map((plan) => plan.planId)).toEqual(['auth-refactor', 'api-layer'])
    expect(independent.map((plan) => plan.planId)).toEqual(['ui-polish'])
  })

  it('treats all plans as independent when sequence is empty', () => {
    const { sequenced, independent } = resolveKickoffGroups(samplePlans, [])

    expect(sequenced).toEqual([])
    expect(independent).toHaveLength(3)
  })
})

describe('hasSequentialKickoff', () => {
  it('returns true when a sequence ID matches a plan', () => {
    expect(hasSequentialKickoff(['auth-refactor', 'missing'], samplePlans)).toBe(true)
  })

  it('returns false when sequence is empty or has no matching plans', () => {
    expect(hasSequentialKickoff([], samplePlans)).toBe(false)
    expect(hasSequentialKickoff(['missing-only'], samplePlans)).toBe(false)
  })
})

describe('getSequenceStepNumber', () => {
  it('returns 1-based step numbers for sequenced plans', () => {
    expect(getSequenceStepNumber('ui-polish', ['auth-refactor', 'ui-polish'])).toBe(2)
    expect(getSequenceStepNumber('missing', ['auth-refactor'])).toBeNull()
  })
})
