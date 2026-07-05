import type { AgentModel, AgentModelListResponse, AgentModelSyncResponse } from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

function encodeModelId(modelId: string): string {
  return encodeURIComponent(modelId)
}

export async function listAgentModels(
  agentId: string,
  includeDisabled = false
): Promise<AgentModelListResponse> {
  const params = new URLSearchParams()
  if (includeDisabled) {
    params.set('includeDisabled', 'true')
  }

  const query = params.toString()
  const url = `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/models${query ? `?${query}` : ''}`
  const response = await fetch(url)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentModelListResponse
}

export async function syncAgentModels(agentId: string): Promise<AgentModelSyncResponse> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/models/sync`,
    { method: 'POST' }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentModelSyncResponse
}

export async function addAgentModel(agentId: string, modelId: string): Promise<AgentModel> {
  const response = await fetch(`${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/models`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ modelId })
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentModel
}

export async function updateAgentModelEnabled(
  agentId: string,
  modelId: string,
  enabled: boolean
): Promise<AgentModel> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/models/${encodeModelId(modelId)}`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ enabled })
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentModel
}

export async function removeAgentModel(agentId: string, modelId: string): Promise<void> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/models/${encodeModelId(modelId)}`,
    { method: 'DELETE' }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}
