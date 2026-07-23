import { describe, expect, it } from 'vitest'

import { resolveBranchReviewProjectId } from '@/lib/branch-review/resolve-project-id'

describe('resolveBranchReviewProjectId', () => {
  it('prefers an explicit requested project when it exists', () => {
    expect(
      resolveBranchReviewProjectId({
        requestedProjectId: 'proj-b',
        activeProjectId: 'proj-a',
        projectIds: ['proj-a', 'proj-b']
      })
    ).toBe('proj-b')
  })

  it('falls back to the active chat project', () => {
    expect(
      resolveBranchReviewProjectId({
        requestedProjectId: null,
        activeProjectId: 'proj-a',
        projectIds: ['proj-a', 'proj-b']
      })
    ).toBe('proj-a')
  })

  it('uses the only project when nothing else is selected', () => {
    expect(
      resolveBranchReviewProjectId({
        requestedProjectId: null,
        activeProjectId: null,
        projectIds: ['solo']
      })
    ).toBe('solo')
  })

  it('returns null when multiple projects exist and none are active', () => {
    expect(
      resolveBranchReviewProjectId({
        requestedProjectId: 'missing',
        activeProjectId: null,
        projectIds: ['proj-a', 'proj-b']
      })
    ).toBeNull()
  })
})
