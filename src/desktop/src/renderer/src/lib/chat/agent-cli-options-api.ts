import type {
  AgentCliOption,
  AgentCliOptionKind,
  AgentCliOptionListResponse
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

export async function listAgentCliOptions(
  agentId: string,
  kind: AgentCliOptionKind | string,
  includeDisabled = false
): Promise<AgentCliOptionListResponse> {
  const params = new URLSearchParams()
  if (includeDisabled) {
    params.set('includeDisabled', 'true')
  }

  const query = params.toString()
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/cli-options/${encodeURIComponent(kind)}${query ? `?${query}` : ''}`
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentCliOptionListResponse
}

export async function addAgentCliOption(
  agentId: string,
  kind: AgentCliOptionKind | string,
  optionId: string,
  label: string,
  cliValue?: string | null
): Promise<AgentCliOption> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/cli-options/${encodeURIComponent(kind)}`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ optionId, label, cliValue: cliValue ?? null })
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentCliOption
}

export async function updateAgentCliOptionEnabled(
  agentId: string,
  kind: AgentCliOptionKind | string,
  optionId: string,
  isEnabled: boolean
): Promise<AgentCliOption> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/cli-options/${encodeURIComponent(kind)}/${encodeURIComponent(optionId)}`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ isEnabled })
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as AgentCliOption
}

export async function removeAgentCliOption(
  agentId: string,
  kind: AgentCliOptionKind | string,
  optionId: string
): Promise<void> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/${encodeURIComponent(agentId)}/cli-options/${encodeURIComponent(kind)}/${encodeURIComponent(optionId)}`,
    { method: 'DELETE' }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}
