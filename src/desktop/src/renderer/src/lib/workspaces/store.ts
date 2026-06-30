export type Workspace = {
  id: string
  path: string
  name: string
  addedAt: string
}

const STORAGE_KEY = 'orchi.workspaces.v1'

type WorkspaceStore = {
  workspaces: Workspace[]
}

function readStore(): WorkspaceStore {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) {
      return { workspaces: [] }
    }

    const parsed = JSON.parse(raw) as WorkspaceStore
    if (!Array.isArray(parsed.workspaces)) {
      return { workspaces: [] }
    }

    return parsed
  } catch {
    return { workspaces: [] }
  }
}

function writeStore(store: WorkspaceStore): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(store))
}

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

function workspaceNameFromPath(path: string): string {
  const normalized = displayWorkspacePath(path)
  const segments = normalized.split(/[/\\]/).filter(Boolean)
  return segments.at(-1) ?? normalized
}

export function listWorkspaces(): Workspace[] {
  return readStore().workspaces
}

export function addWorkspace(path: string): Workspace | null {
  const displayPath = displayWorkspacePath(path)
  if (!displayPath) {
    return null
  }

  const normalized = normalizeWorkspacePath(displayPath)
  const store = readStore()

  const existing = store.workspaces.find(
    (workspace) => normalizeWorkspacePath(workspace.path) === normalized
  )
  if (existing) {
    return existing
  }

  const workspace: Workspace = {
    id: crypto.randomUUID(),
    path: displayPath,
    name: workspaceNameFromPath(displayPath),
    addedAt: new Date().toISOString()
  }

  store.workspaces.push(workspace)
  writeStore(store)
  return workspace
}

export function removeWorkspace(id: string): void {
  const store = readStore()
  store.workspaces = store.workspaces.filter((workspace) => workspace.id !== id)
  writeStore(store)
}

export function renameWorkspace(id: string, name: string): Workspace | null {
  const trimmed = name.trim()
  if (!trimmed) {
    return null
  }

  const store = readStore()
  const workspace = store.workspaces.find((entry) => entry.id === id)
  if (!workspace) {
    return null
  }

  workspace.name = trimmed
  writeStore(store)
  return workspace
}

export function findWorkspaceByPath(path: string): Workspace | undefined {
  const normalized = normalizeWorkspacePath(path)
  return readStore().workspaces.find(
    (workspace) => normalizeWorkspacePath(workspace.path) === normalized
  )
}
