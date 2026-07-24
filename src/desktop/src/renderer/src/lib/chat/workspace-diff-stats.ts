export const WORKSPACE_DIFF_STATS_MARKER = '<!-- orchi-workspace-diff-stats -->'

export function isWorkspaceDiffStatsMessage(content: string): boolean {
  return content.includes(WORKSPACE_DIFF_STATS_MARKER)
}

export function stripWorkspaceDiffStatsMarker(content: string): string {
  return content.replace(WORKSPACE_DIFF_STATS_MARKER, '').trimStart()
}

export function formatDiffStatsCell(value: string): { text: string; tone: 'added' | 'removed' | null } {
  const trimmed = value.trim()
  if (/^\+\d+$/.test(trimmed)) {
    return { text: trimmed, tone: 'added' }
  }

  if (/^-\d+$/.test(trimmed)) {
    return { text: trimmed, tone: 'removed' }
  }

  return { text: value, tone: null }
}
