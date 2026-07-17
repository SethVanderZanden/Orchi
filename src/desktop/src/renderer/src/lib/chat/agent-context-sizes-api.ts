import type { AgentContextSizeListResponse, AgentInfo } from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

export async function listAgents(): Promise<AgentInfo[]> {
  const response = await fetch(`${getApiBaseUrl()}/agents`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const payload = (await response.json()) as { agents: AgentInfo[] }
  return payload.agents
}

export async function listAgentContextSizes(
  agentId: string,
  includeDisabled = false
): Promise<AgentContextSizeListResponse> {
  const params = new URLSearchParams()
  if (includeDisabled) {
    params.set('includeDisabled', 'true')
  }

  const query = params.toString()
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/context-sizes${query ? `?${query}` : ''}`
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentContextSizeListResponse
}

export async function addAgentContextSize(
  agentId: string,
  sizeId: string,
  label: string,
  tokenCount: number
): Promise<AgentContextSizeListResponse['contextSizes'][number]> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/context-sizes`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ sizeId, label, tokenCount })
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentContextSizeListResponse['contextSizes'][number]
}

export async function updateAgentContextSizeEnabled(
  agentId: string,
  sizeId: string,
  isEnabled: boolean
): Promise<AgentContextSizeListResponse['contextSizes'][number]> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/context-sizes/${encodeURIComponent(sizeId)}`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isEnabled })
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentContextSizeListResponse['contextSizes'][number]
}

export async function removeAgentContextSize(agentId: string, sizeId: string): Promise<void> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/context-sizes/${encodeURIComponent(sizeId)}`,
    { method: 'DELETE' }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}
