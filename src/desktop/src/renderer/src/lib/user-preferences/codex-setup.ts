export const CODEX_APPROVAL_SETUP_OPTIONS = [
  {
    id: 'on-request',
    label: 'Ask on risky actions',
    description:
      'Codex runs freely in the workspace but pauses before network access or edits outside the project. Recommended for everyday use.'
  },
  {
    id: 'never',
    label: 'Automatic',
    description:
      'Codex never asks for approval. Best for trusted automation and long-running tasks in Orchi.'
  },
  {
    id: 'untrusted',
    label: 'Strict',
    description:
      'Approve most actions beyond safe read-only browsing. Use when you want maximum control.'
  }
] as const

export type CodexApprovalSetupOptionId = (typeof CODEX_APPROVAL_SETUP_OPTIONS)[number]['id']

export const DEFAULT_CODEX_APPROVAL_POLICY_ID: CodexApprovalSetupOptionId = 'never'
