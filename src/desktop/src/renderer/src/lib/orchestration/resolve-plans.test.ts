import { describe, expect, it } from 'vitest'

import { mergeOrchestrationPlans } from '@/lib/orchestration/resolve-plans'

describe('mergeOrchestrationPlans', () => {
  it('prefers backend plans over message plans for the same id', () => {
    const merged = mergeOrchestrationPlans(
      [
        {
          planId: 'auth-refactor',
          title: 'Auth refactor v2',
          contentMarkdown: '# Auth refactor v2\n\nUpdated.'
        }
      ],
      [
        {
          planId: 'auth-refactor',
          title: 'Auth refactor',
          contentMarkdown: '# Auth refactor\n\nInitial.'
        }
      ]
    )

    expect(merged).toHaveLength(1)
    expect(merged[0]?.title).toBe('Auth refactor v2')
    expect(merged[0]?.contentMarkdown).toContain('Updated.')
  })

  it('keeps message-only plans until backend sync arrives', () => {
    const merged = mergeOrchestrationPlans(
      [],
      [
        {
          planId: 'ui-polish',
          title: 'UI polish',
          contentMarkdown: '# UI polish'
        }
      ]
    )

    expect(merged).toHaveLength(1)
    expect(merged[0]?.planId).toBe('ui-polish')
  })
})
