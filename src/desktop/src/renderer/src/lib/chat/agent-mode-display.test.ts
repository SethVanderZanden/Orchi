import { Bot, Network, Shield } from 'lucide-react'
import { describe, expect, it } from 'vitest'

import { getAgentModeDisplay } from './agent-mode-display'

describe('getAgentModeDisplay', () => {
  it('returns Network for orchestration mode', () => {
    const display = getAgentModeDisplay('orchestration')
    expect(display.Icon).toBe(Network)
    expect(display.label).toBe('Orchestration')
    expect(display.badgeClassName).toContain('amber')
  })

  it('returns Bot for default mode', () => {
    const display = getAgentModeDisplay('default')
    expect(display.Icon).toBe(Bot)
    expect(display.label).toBe('Agent')
    expect(display.badgeClassName).toContain('primary')
  })

  it('returns Bot for implementation mode', () => {
    const display = getAgentModeDisplay('implementation')
    expect(display.Icon).toBe(Bot)
    expect(display.label).toBe('Agent')
  })

  it('returns Shield for review mode', () => {
    const display = getAgentModeDisplay('review')
    expect(display.Icon).toBe(Shield)
    expect(display.label).toBe('Review')
    expect(display.badgeClassName).toContain('violet')
  })

  it('uses the same review badge for branch-review with its own label', () => {
    const display = getAgentModeDisplay('branch-review')
    expect(display.Icon).toBe(Shield)
    expect(display.label).toBe('Branch review')
    expect(display.badgeClassName).toContain('violet')
  })

  it('matches modes case-insensitively', () => {
    expect(getAgentModeDisplay('Orchestration').Icon).toBe(Network)
    expect(getAgentModeDisplay('REVIEW').Icon).toBe(Shield)
    expect(getAgentModeDisplay('BRANCH-REVIEW').label).toBe('Branch review')
  })

  it('falls back to Bot for unknown modes', () => {
    const display = getAgentModeDisplay('unknown-mode')
    expect(display.Icon).toBe(Bot)
    expect(display.label).toBe('Agent')
  })
})
