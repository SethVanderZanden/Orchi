import type {
  AgentModeModelDefaultsListResponse,
  UpdateAgentModeModelDefaultRequest,
  UpdateAgentModeModelDefaultResponse
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as {
      message?: string
      Message?: string
      title?: string
      detail?: string
      errors?: Record<string, string[]>
    }

    if (body.errors) {
      const messages = Object.values(body.errors).flat()
      if (messages.length > 0) {
        return messages[0]!
      }
    }

    return body.detail ?? body.message ?? body.Message ?? `API error: ${response.status}`
  } catch {
    return `API error: ${response.status}`
  }
}

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
