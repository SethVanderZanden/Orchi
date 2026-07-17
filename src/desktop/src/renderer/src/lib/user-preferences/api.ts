import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'
import type {
  PostMessageBehavior,
  UpdateUserPreferencesRequest,
  UserPreferences
} from '@/lib/user-preferences/types'

export async function getUserPreferences(): Promise<UserPreferences> {
  const response = await fetch(`${getApiBaseUrl()}/user-preferences`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const payload = (await response.json()) as UserPreferences
  return {
    ...payload,
    enabledAgentIds: payload.enabledAgentIds ?? [],
    autoKickOffReview: payload.autoKickOffReview ?? true
  }
}

export async function updateUserPreferences(
  request: UpdateUserPreferencesRequest
): Promise<UserPreferences> {
  const response = await fetch(`${getApiBaseUrl()}/user-preferences`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const payload = (await response.json()) as UserPreferences
  return {
    ...payload,
    enabledAgentIds: payload.enabledAgentIds ?? [],
    autoKickOffReview: payload.autoKickOffReview ?? true
  }
}

export const DEFAULT_POST_MESSAGE_BEHAVIOR: PostMessageBehavior = 'stayOnChat'
export const DEFAULT_AUTO_KICK_OFF_REVIEW = true
