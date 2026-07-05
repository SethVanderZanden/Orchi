import type {
  AgentModeModelDefaultsListResponse,
  UpdateAgentModeModelDefaultRequest,
  UpdateAgentModeModelDefaultResponse
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

export async function listAgentModeModelDefaults(
  agentId: string
): Promise<AgentModeModelDefaultsListResponse> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/mode-model-defaults`
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentModeModelDefaultsListResponse
}

export async function updateAgentModeModelDefault(
  agentId: string,
  mode: string,
  modelId: string | null
): Promise<UpdateAgentModeModelDefaultResponse> {
  const body: UpdateAgentModeModelDefaultRequest = { modelId }
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/mode-model-defaults/${encodeURIComponent(mode)}`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body)
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as UpdateAgentModeModelDefaultResponse
}
