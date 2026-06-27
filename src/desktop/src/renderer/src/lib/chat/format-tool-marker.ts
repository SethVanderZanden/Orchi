const TOOL_LABELS: Record<string, string> = {
  readToolCall: 'Reading',
  writeToolCall: 'Writing',
  listToolCall: 'Listing',
  grepToolCall: 'Searching',
  searchToolCall: 'Searching',
  shellToolCall: 'Running',
  bashToolCall: 'Running'
}

export function formatToolMarker(name: string, status: string, detail?: string | null): string {
  const label = TOOL_LABELS[name] ?? formatToolName(name)
  const statusLabel = status === 'started' ? '' : ` (${status})`

  if (detail) {
    return `${label} ${detail}${statusLabel}`
  }

  return `${label}${statusLabel}`
}

function formatToolName(name: string): string {
  if (name.endsWith('ToolCall')) {
    return name.slice(0, -'ToolCall'.length)
  }

  return name
}
