export type AvailableAgentOption = {
  id: string
  label: string
  description: string
}

/** Agents Orchi can run today — keep in sync with API AgentIds. */
export const AVAILABLE_AGENT_OPTIONS: AvailableAgentOption[] = [
  {
    id: 'cursor',
    label: 'Cursor',
    description: 'Cursor CLI agent (`agent`).'
  },
  {
    id: 'codex',
    label: 'Codex',
    description: 'OpenAI Codex CLI agent (`codex`).'
  }
]

export function getAvailableAgentOptions(): AvailableAgentOption[] {
  return AVAILABLE_AGENT_OPTIONS
}

export function getAvailableAgentLabel(agentId: string): string {
  return AVAILABLE_AGENT_OPTIONS.find((agent) => agent.id === agentId)?.label ?? agentId
}

export function needsAgentSetup(enabledAgentIds: string[] | undefined): boolean {
  return (enabledAgentIds?.length ?? 0) === 0
}

/** Prefer this over `needsAgentSetup(undefined)` when the preferences query may have failed. */
export function needsAgentSetupAfterLoad(
  isSuccess: boolean,
  enabledAgentIds: string[] | undefined
): boolean {
  return isSuccess && needsAgentSetup(enabledAgentIds)
}

export function filterAgentsByEnabled<T extends { id: string }>(
  agents: T[],
  enabledAgentIds: string[]
): T[] {
  if (enabledAgentIds.length === 0) {
    return []
  }

  const enabled = new Set(enabledAgentIds.map((id) => id.toLowerCase()))
  return agents.filter((agent) => enabled.has(agent.id.toLowerCase()))
}
