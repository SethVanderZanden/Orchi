import { describe, expect, it } from 'vitest'

import {
  FALLBACK_MODE_OPTIONS,
  getNextAgentMode,
  resolveAgentModeOptions
} from '@/lib/chat/agent-mode-utils'

describe('getNextAgentMode', () => {
  it('cycles through all fallback modes and wraps back to default', () => {
    expect(getNextAgentMode('default', FALLBACK_MODE_OPTIONS)).toBe('orchestration')
    expect(getNextAgentMode('orchestration', FALLBACK_MODE_OPTIONS)).toBe('review')
    expect(getNextAgentMode('review', FALLBACK_MODE_OPTIONS)).toBe('default')
  })

  it('matches modes case-insensitively', () => {
    expect(getNextAgentMode('REVIEW', FALLBACK_MODE_OPTIONS)).toBe('default')
    expect(getNextAgentMode('Orchestration', FALLBACK_MODE_OPTIONS)).toBe('review')
  })

  it('returns current mode when options are empty', () => {
    expect(getNextAgentMode('review', [])).toBe('review')
  })
})

describe('resolveAgentModeOptions', () => {
  it('uses fallback options when API data is missing or empty', () => {
    expect(resolveAgentModeOptions(undefined)).toBe(FALLBACK_MODE_OPTIONS)
    expect(resolveAgentModeOptions([])).toBe(FALLBACK_MODE_OPTIONS)
  })

  it('returns API options when present', () => {
    const apiOptions = [{ id: 'custom', label: 'Custom', description: null }]
    expect(resolveAgentModeOptions(apiOptions)).toBe(apiOptions)
  })
})
