import { describe, expect, it } from 'vitest'

import {
  formatDefaultPullRequestTitle,
  getCommitDialogSubmitLabel,
  getCreatePullRequestDisabledReason,
  getPrimaryGitActionLabel,
  isGitActionsDisabled
} from './git-action-labels'

describe('git-action-labels', () => {
  it('disables git actions when workspace path is empty', () => {
    expect(isGitActionsDisabled('')).toBe(true)
    expect(isGitActionsDisabled('   ')).toBe(true)
    expect(isGitActionsDisabled('/repo')).toBe(false)
  })

  it('uses commit and push as the primary label', () => {
    expect(getPrimaryGitActionLabel()).toBe('Commit and push')
  })

  it('labels commit dialog submit buttons by mode', () => {
    expect(getCommitDialogSubmitLabel('commit')).toBe('Commit')
    expect(getCommitDialogSubmitLabel('commitAndPush')).toBe('Commit and push')
  })

  it('returns disabled reasons for pull request actions', () => {
    expect(getCreatePullRequestDisabledReason({ projectId: null, readiness: undefined })).toBe(
      'Assign a project to create a pull request.'
    )

    expect(
      getCreatePullRequestDisabledReason({
        projectId: 'project-1',
        readiness: {
          providerId: 'github',
          status: 'missingCli',
          message: 'Install gh CLI.',
          requiredCli: 'gh'
        }
      })
    ).toBe('Install gh CLI.')
  })

  it('formats default pull request titles', () => {
    expect(formatDefaultPullRequestTitle('feature/orchi')).toBe('Orchi: feature/orchi')
    expect(formatDefaultPullRequestTitle(null)).toBe('Orchi: workspace changes')
  })
})
