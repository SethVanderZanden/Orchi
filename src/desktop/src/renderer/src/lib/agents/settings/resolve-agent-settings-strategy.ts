import type { AgentSettingsStrategy } from '@/lib/agents/settings/types'

export const cursorAgentSettingsStrategy: AgentSettingsStrategy = {
  agentId: 'cursor',
  label: 'Cursor',
  capabilities: new Set(['modelSync', 'manualModels', 'contextSizes']),
  modelsDocs: {
    label: 'Cursor CLI models',
    href: 'https://cursor.com/docs/cli/reference/parameters'
  },
  manualModelPlaceholder: 'e.g. claude-sonnet-4'
}

export const codexAgentSettingsStrategy: AgentSettingsStrategy = {
  agentId: 'codex',
  label: 'Codex',
  capabilities: new Set([
    'curatedModels',
    'manualModels',
    'contextSizes',
    'reasoningEffort',
    'approvalPolicy'
  ]),
  modelsDocs: {
    label: 'Codex models',
    href: 'https://developers.openai.com/codex/models'
  },
  manualModelPlaceholder: 'e.g. gpt-5.6-terra'
}

const STRATEGIES: Record<string, AgentSettingsStrategy> = {
  cursor: cursorAgentSettingsStrategy,
  codex: codexAgentSettingsStrategy
}

export function resolveAgentSettingsStrategy(agentId: string): AgentSettingsStrategy {
  const normalized = agentId.trim().toLowerCase()
  return (
    STRATEGIES[normalized] ?? {
      ...cursorAgentSettingsStrategy,
      agentId: normalized,
      label: agentId
    }
  )
}
