import type { GitCommitDialogMode, GitHostReadiness } from './types'

export function isGitActionsDisabled(workspacePath: string): boolean {
  return !workspacePath.trim()
}

export function getPrimaryGitActionLabel(): string {
  return 'Commit and push'
}

export function getCommitDialogSubmitLabel(mode: GitCommitDialogMode): string {
  return mode === 'commitAndPush' ? 'Commit and push' : 'Commit'
}

export function getCreatePullRequestDisabledReason(options: {
  projectId: string | null
  readiness: GitHostReadiness | undefined
}): string | null {
  if (!options.projectId) {
    return 'Assign a project to create a pull request.'
  }

  if (options.readiness && options.readiness.status !== 'ready') {
    return options.readiness.message
  }

  return null
}

export function formatGitActionSuccessMessage(
  steps: Array<{ output: string; succeeded: boolean }>
): string {
  const successfulOutputs = steps.filter((step) => step.succeeded).map((step) => step.output)
  if (successfulOutputs.length > 0) {
    return successfulOutputs[successfulOutputs.length - 1] ?? 'Git action completed.'
  }

  const failedStep = steps.find((step) => !step.succeeded)
  return failedStep?.output ?? 'Git action failed.'
}

export function formatDefaultPullRequestTitle(headBranch: string | null | undefined): string {
  if (headBranch?.trim()) {
    return `Orchi: ${headBranch.trim()}`
  }

  return 'Orchi: workspace changes'
}

export function formatDefaultPullRequestBody(): string {
  return 'Changes from an Orchi chat workspace.'
}
