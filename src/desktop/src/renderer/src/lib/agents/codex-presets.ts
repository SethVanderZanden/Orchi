/**
 * Codex GPT-5.6 capability tiers. Model id goes to `codex exec --model`;
 * label matches Codex UI naming (e.g. "5.6 Terra").
 */
export const CODEX_MODEL_PRESETS = [
  {
    id: 'gpt-5.6-sol',
    label: '5.6 Sol',
    description: 'Flagship — deepest reasoning for complex, high-value work.'
  },
  {
    id: 'gpt-5.6-terra',
    label: '5.6 Terra',
    description: 'Balanced everyday workhorse. Good default for orchestration and review.'
  },
  {
    id: 'gpt-5.6-luna',
    label: '5.6 Luna',
    description: 'Fast and efficient — best for clear, high-volume tasks.'
  }
] as const

export type CodexModelPresetId = (typeof CODEX_MODEL_PRESETS)[number]['id']

export const DEFAULT_CODEX_MODEL_ID: CodexModelPresetId = 'gpt-5.6-terra'

export const CODEX_REASONING_PRESETS = [
  { id: 'none', label: 'None', cliValue: 'none' },
  { id: 'minimal', label: 'Minimal', cliValue: 'minimal' },
  { id: 'low', label: 'Low', cliValue: 'low' },
  { id: 'medium', label: 'Medium', cliValue: 'medium' },
  { id: 'high', label: 'High', cliValue: 'high' },
  { id: 'xhigh', label: 'Extra high', cliValue: 'xhigh' },
  { id: 'max', label: 'Max', cliValue: 'max' }
] as const

export type CodexReasoningPresetId = (typeof CODEX_REASONING_PRESETS)[number]['id']

export const DEFAULT_CODEX_REASONING_EFFORT_ID: CodexReasoningPresetId = 'medium'

/** Modes users configure during first-run setup (implementation inherits from parent). */
export const MODE_DEFAULT_SETUP_MODES = [
  {
    mode: 'orchestration',
    label: 'Orchestrator',
    description: 'Plans work and kicks off implementation agents.',
    suggestedModelId: 'gpt-5.6-terra' as CodexModelPresetId,
    suggestedReasoningEffortId: 'medium' as CodexReasoningPresetId
  },
  {
    mode: 'default',
    label: 'Implementation / default',
    description: 'Everyday chat and plan kickoff children.',
    suggestedModelId: 'gpt-5.6-luna' as CodexModelPresetId,
    suggestedReasoningEffortId: 'medium' as CodexReasoningPresetId
  },
  {
    mode: 'review',
    label: 'Review',
    description: 'Verifies implementation against the original plan.',
    suggestedModelId: 'gpt-5.6-terra' as CodexModelPresetId,
    suggestedReasoningEffortId: 'medium' as CodexReasoningPresetId
  }
] as const

export function formatCodexRuntimeLabel(
  modelLabel: string | null | undefined,
  reasoningLabel: string | null | undefined
): string {
  const model = modelLabel?.trim()
  const reasoning = reasoningLabel?.trim()

  if (model && reasoning) {
    return `${model} ${reasoning}`
  }

  if (model) {
    return model
  }

  if (reasoning) {
    return reasoning
  }

  return 'Default (CLI)'
}
