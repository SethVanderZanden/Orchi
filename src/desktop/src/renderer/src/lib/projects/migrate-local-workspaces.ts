import { createProject, listProjects } from './api'
import { normalizeWorkspacePath, workspaceNameFromPath } from './paths'

const LEGACY_STORAGE_KEY = 'orchi.workspaces.v1'

type LegacyWorkspace = {
  id: string
  path: string
  name: string
  addedAt: string
}

type LegacyWorkspaceStore = {
  workspaces: LegacyWorkspace[]
}

function readLegacyWorkspaces(): LegacyWorkspace[] {
  try {
    const raw = localStorage.getItem(LEGACY_STORAGE_KEY)
    if (!raw) {
      return []
    }

    const parsed = JSON.parse(raw) as LegacyWorkspaceStore
    if (!Array.isArray(parsed.workspaces)) {
      return []
    }

    return parsed.workspaces
  } catch {
    return []
  }
}

function clearLegacyWorkspaces(): void {
  localStorage.removeItem(LEGACY_STORAGE_KEY)
}

export async function migrateLocalWorkspacesIfNeeded(): Promise<void> {
  const legacyWorkspaces = readLegacyWorkspaces()
  if (legacyWorkspaces.length === 0) {
    return
  }

  const existingProjects = await listProjects()
  if (existingProjects.length > 0) {
    return
  }

  const seenPaths = new Set<string>()

  for (const legacy of legacyWorkspaces) {
    const normalized = normalizeWorkspacePath(legacy.path)
    if (seenPaths.has(normalized)) {
      continue
    }

    seenPaths.add(normalized)
    await createProject({
      name: legacy.name.trim() || workspaceNameFromPath(legacy.path),
      defaultWorkspacePath: legacy.path
    })
  }

  clearLegacyWorkspaces()
}
