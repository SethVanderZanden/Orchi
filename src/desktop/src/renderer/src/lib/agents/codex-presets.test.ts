import { describe, expect, it } from 'vitest'

import { formatCodexRuntimeLabel, MODE_DEFAULT_SETUP_MODES } from '@/lib/agents/codex-presets'
import { formatModelReasoningLabel } from '@/lib/agents/format-runtime-label'
import { resolveAgentSettingsStrategy } from '@/lib/agents/settings/resolve-agent-settings-strategy'

describe('formatCodexRuntimeLabel', () => {
  it('combines model and reasoning like the Codex UI', () => {
    expect(formatCodexRuntimeLabel('5.6 Terra', 'Medium')).toBe('5.6 Terra Medium')
    expect(formatCodexRuntimeLabel('5.6 Luna', 'Medium')).toBe('5.6 Luna Medium')
  })

  it('falls back when parts are missing', () => {
    expect(formatCodexRuntimeLabel('5.6 Sol', null)).toBe('5.6 Sol')
    expect(formatCodexRuntimeLabel(null, 'High')).toBe('High')
    expect(formatCodexRuntimeLabel(null, null)).toBe('Default (CLI)')
  })
})

describe('formatModelReasoningLabel', () => {
  it('only combines for Codex', () => {
    expect(formatModelReasoningLabel('codex', '5.6 Terra', 'Medium')).toBe('5.6 Terra Medium')
    expect(formatModelReasoningLabel('cursor', 'claude-4', 'Medium')).toBe('claude-4')
  })
})

describe('resolveAgentSettingsStrategy', () => {
  it('exposes Codex curated models and reasoning', () => {
    const strategy = resolveAgentSettingsStrategy('codex')
    expect(strategy.capabilities.has('curatedModels')).toBe(true)
    expect(strategy.capabilities.has('reasoningEffort')).toBe(true)
    expect(strategy.capabilities.has('modelSync')).toBe(false)
  })

  it('exposes Cursor model sync without reasoning', () => {
    const strategy = resolveAgentSettingsStrategy('cursor')
    expect(strategy.capabilities.has('modelSync')).toBe(true)
    expect(strategy.capabilities.has('reasoningEffort')).toBe(false)
  })
})

describe('MODE_DEFAULT_SETUP_MODES', () => {
  it('covers orchestrator, default/implementation, and review', () => {
    expect(MODE_DEFAULT_SETUP_MODES.map((mode) => mode.mode)).toEqual([
      'orchestration',
      'default',
      'review'
    ])
  })
})
