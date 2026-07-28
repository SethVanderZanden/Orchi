export type ParsedOpenEditorCommand = {
  workspacePath: string
  relativePath: string
  line: number
  column?: number
}

const OPEN_EDITOR_COMMAND_PATTERN =
  /^(?:code|cursor)\s+(?:"([^"]+)"|'([^']+)'|(\S+))\s+-g\s+(.+)$/i

const GOTO_TARGET_PATTERN = /^(.+?):(\d+)(?::(\d+))?$/

export function parseOpenEditorCommand(command: string): ParsedOpenEditorCommand | null {
  const trimmed = command.trim()
  const match = trimmed.match(OPEN_EDITOR_COMMAND_PATTERN)
  if (!match) {
    return null
  }

  const workspacePath = (match[1] ?? match[2] ?? match[3] ?? '').trim()
  const gotoTarget = (match[4] ?? '').trim()
  if (!workspacePath || !gotoTarget) {
    return null
  }

  const gotoMatch = gotoTarget.match(GOTO_TARGET_PATTERN)
  if (!gotoMatch) {
    return null
  }

  const relativePath = gotoMatch[1]?.trim()
  const line = Number.parseInt(gotoMatch[2] ?? '', 10)
  const columnRaw = gotoMatch[3]
  const column = columnRaw ? Number.parseInt(columnRaw, 10) : undefined

  if (!relativePath || !Number.isFinite(line) || line < 1) {
    return null
  }

  if (column !== undefined && (!Number.isFinite(column) || column < 1)) {
    return null
  }

  return {
    workspacePath,
    relativePath,
    line,
    ...(column !== undefined ? { column } : {})
  }
}

export function formatOpenEditorLinkLabel(command: ParsedOpenEditorCommand): string {
  const { relativePath, line, column } = command
  return column !== undefined ? `${relativePath}:${line}:${column}` : `${relativePath}:${line}`
}
