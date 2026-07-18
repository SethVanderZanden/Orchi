import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

import type {
  GitHostReadiness,
  GitHostReadinessOptions,
  RunGitActionRequest,
  RunGitActionResponse,
  SuggestedCommitMessageResponse
} from './types'

export async function getSuggestedCommitMessage(chatId: string): Promise<string | null> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/git/suggested-commit-message`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const raw = (await response.json()) as SuggestedCommitMessageResponse
  return mapSuggestedCommitMessage(raw)
}

export async function runChatGitAction(
  chatId: string,
  request: RunGitActionRequest
): Promise<RunGitActionResponse> {
  const response = await fetch(`${getApiBaseUrl()}/chats/${chatId}/git/actions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const raw = (await response.json()) as RunGitActionResponseDto
  return mapRunGitActionResponse(raw)
}

export async function getGitHostReadiness(
  options?: GitHostReadinessOptions
): Promise<GitHostReadiness> {
  const params = new URLSearchParams()
  if (options?.projectId) {
    params.set('projectId', options.projectId)
  }
  if (options?.workspacePath) {
    params.set('workspacePath', options.workspacePath)
  }
  if (options?.provider) {
    params.set('provider', options.provider)
  }

  const query = params.toString()
  const response = await fetch(
    `${getApiBaseUrl()}/git/hosting/readiness${query ? `?${query}` : ''}`
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as GitHostReadiness
}

type RunGitActionResponseDto = {
  succeeded: boolean
  steps: Array<{ label: string; output: string; succeeded: boolean }>
  pullRequestUrl: string | null
}

function mapSuggestedCommitMessage(dto: SuggestedCommitMessageResponse): string | null {
  return dto.message
}

function mapRunGitActionResponse(dto: RunGitActionResponseDto): RunGitActionResponse {
  return {
    succeeded: dto.succeeded,
    pullRequestUrl: dto.pullRequestUrl,
    steps: dto.steps.map((step) => ({
      label: step.label,
      output: step.output,
      succeeded: step.succeeded
    }))
  }
}
