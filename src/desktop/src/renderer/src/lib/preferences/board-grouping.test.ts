import { describe, expect, it, beforeEach } from 'vitest'

import { getBoardGrouping, isBoardGroupingMode, setBoardGrouping } from './board-grouping'

describe('board-grouping preference', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('defaults to state grouping', () => {
    expect(getBoardGrouping()).toBe('state')
  })

  it('persists and reads grouping mode', () => {
    setBoardGrouping('project')
    expect(getBoardGrouping()).toBe('project')
  })

  it('ignores invalid stored values', () => {
    localStorage.setItem('orchi.boardGrouping', 'nope')
    expect(getBoardGrouping()).toBe('state')
  })

  it('validates grouping mode values', () => {
    expect(isBoardGroupingMode('state')).toBe(true)
    expect(isBoardGroupingMode('project')).toBe(true)
    expect(isBoardGroupingMode('status')).toBe(false)
  })
})
