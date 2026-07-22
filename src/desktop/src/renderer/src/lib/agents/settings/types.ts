export type AgentSettingsCapability =
  | 'modelSync'
  | 'manualModels'
  | 'curatedModels'
  | 'contextSizes'
  | 'reasoningEffort'
  | 'approvalPolicy'

export type AgentSettingsStrategy = {
  agentId: string
  label: string
  capabilities: ReadonlySet<AgentSettingsCapability>
  modelsDocs?: { label: string; href: string }
  manualModelPlaceholder: string
}

export function strategySupports(
  strategy: AgentSettingsStrategy,
  capability: AgentSettingsCapability
): boolean {
  return strategy.capabilities.has(capability)
}
