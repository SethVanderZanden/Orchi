import { describe, expect, it } from 'vitest'

import {
  formatDiffStatsCell,
  isWorkspaceDiffStatsMessage,
  stripWorkspaceDiffStatsMarker,
  WORKSPACE_DIFF_STATS_MARKER
} from '@/lib/chat/workspace-diff-stats'

describe('workspace-diff-stats', () => {
  it('detects diff stats marker', () => {
    expect(isWorkspaceDiffStatsMessage(`${WORKSPACE_DIFF_STATS_MARKER}\n\n### Workspace changes`)).toBe(
      true
    )
    expect(isWorkspaceDiffStatsMessage('plain message')).toBe(false)
  })

  it('strips marker before rendering', () => {
    expect(stripWorkspaceDiffStatsMarker(`${WORKSPACE_DIFF_STATS_MARKER}\n\n### Workspace changes`)).toBe(
      '### Workspace changes'
    )
  })

  it('formats added and removed cell tones', () => {
    expect(formatDiffStatsCell('+42')).toEqual({ text: '+42', tone: 'added' })
    expect(formatDiffStatsCell('-7')).toEqual({ text: '-7', tone: 'removed' })
    expect(formatDiffStatsCell('**+42**')).toEqual({ text: '**+42**', tone: null })
  })
})
