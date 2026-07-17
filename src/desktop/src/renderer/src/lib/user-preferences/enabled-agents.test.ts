import { describe, expect, it } from 'vitest'

import { filterAgentsByEnabled, needsAgentSetup } from '@/lib/user-preferences/enabled-agents'

describe('needsAgentSetup', () => {
  it('is true when no agents are enabled (fresh preferences)', () => {
    expect(needsAgentSetup([])).toBe(true)
    expect(needsAgentSetup(undefined)).toBe(true)
  })

  it('is false once at least one agent is enabled', () => {
    expect(needsAgentSetup(['cursor'])).toBe(false)
    expect(needsAgentSetup(['codex', 'cursor'])).toBe(false)
  })
})

describe('filterAgentsByEnabled', () => {
  const agents = [
    { id: 'cursor', label: 'Cursor' },
    { id: 'codex', label: 'Codex' }
  ]

  it('returns no agents when none are enabled', () => {
    expect(filterAgentsByEnabled(agents, [])).toEqual([])
  })

  it('keeps only enabled agents', () => {
    expect(filterAgentsByEnabled(agents, ['codex'])).toEqual([{ id: 'codex', label: 'Codex' }])
  })
})
