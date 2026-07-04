export function normalizeWorkspacePath(path: string): string {
  const trimmed = path.trim().replace(/[/\\]+$/, '')
  const withBackslashes = trimmed.replace(/\//g, '\\')

  if (/^[a-zA-Z]:\\/.test(withBackslashes)) {
    return withBackslashes.toLowerCase()
  }

  return trimmed.replace(/\\/g, '/')
}

export function displayWorkspacePath(path: string): string {
  return path.trim().replace(/[/\\]+$/, '')
}

export function workspaceNameFromPath(path: string): string {
  const normalized = displayWorkspacePath(path)
  const segments = normalized.split(/[/\\]/).filter(Boolean)
  return segments.at(-1) ?? normalized
}
