import { AgentCliOptionsCard } from '@/components/settings/agent-cli-options-card'
import { AgentContextSizesCard } from '@/components/settings/agent-context-sizes-card'
import { AgentModelsCard } from '@/components/settings/agent-models-card'
import { CodexAgentModelsCard } from '@/components/settings/codex-agent-models-card'
import { resolveAgentSettingsStrategy } from '@/lib/agents/settings/resolve-agent-settings-strategy'

type AgentSettingsSectionProps = {
  agentId: string
  agentLabel: string
}

export function AgentSettingsSection({
  agentId,
  agentLabel
}: AgentSettingsSectionProps): React.JSX.Element {
  const strategy = resolveAgentSettingsStrategy(agentId)
  const showModels =
    strategy.capabilities.has('modelSync') ||
    strategy.capabilities.has('curatedModels') ||
    strategy.capabilities.has('manualModels')
  const showContext = strategy.capabilities.has('contextSizes')
  const showReasoning = strategy.capabilities.has('reasoningEffort')
  const showApproval = strategy.capabilities.has('approvalPolicy')
  const useCodexModels = strategy.capabilities.has('curatedModels')

  return (
    <div className="flex flex-col gap-8">
      {showModels ? (
        useCodexModels ? (
          <CodexAgentModelsCard agentId={agentId} agentLabel={agentLabel} />
        ) : (
          <AgentModelsCard agentId={agentId} agentLabel={agentLabel} />
        )
      ) : null}
      {showContext ? <AgentContextSizesCard agentId={agentId} agentLabel={agentLabel} /> : null}
      {showReasoning ? (
        <AgentCliOptionsCard
          agentId={agentId}
          agentLabel={agentLabel}
          kind="model_reasoning_effort"
        />
      ) : null}
      {showApproval ? (
        <AgentCliOptionsCard agentId={agentId} agentLabel={agentLabel} kind="approval_policy" />
      ) : null}
    </div>
  )
}
