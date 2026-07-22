import { formatCodexRuntimeLabel } from '@/lib/agents/codex-presets'

export function formatModelReasoningLabel(
  agentId: string,
  modelLabel: string | null | undefined,
  reasoningLabel: string | null | undefined
): string {
  if (agentId.toLowerCase() === 'codex') {
    return formatCodexRuntimeLabel(modelLabel, reasoningLabel)
  }

  return modelLabel?.trim() || 'Default (CLI)'
}
