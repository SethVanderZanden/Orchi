export type GitActionKind = 'commit' | 'commitAndPush' | 'createPullRequest'

export type GitCommitDialogMode = 'commit' | 'commitAndPush'

export type GitHostProvider = 'github' | 'azureDevOps'

export type GitHostReadiness = {
  providerId: string
  status: 'ready' | 'missingCli' | 'notAuthenticated' | 'repoNotDetected' | string
  message: string
  requiredCli: string | null
}

export type GitActionStepResult = {
  label: string
  output: string
  succeeded: boolean
}

export type RunGitActionResponse = {
  succeeded: boolean
  steps: GitActionStepResult[]
  pullRequestUrl: string | null
}

export type SuggestedCommitMessageResponse = {
  message: string | null
}

export type RunGitActionRequest = {
  action: GitActionKind
  commitMessage?: string | null
  generateCommitMessage?: boolean
  pullRequestTitle?: string | null
  pullRequestBody?: string | null
  targetBranch?: string | null
}

export type GitHostReadinessOptions = {
  projectId?: string
  workspacePath?: string
  provider?: string
}
