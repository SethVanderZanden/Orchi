export type ParentChatExpansionInput = {
  expandedParentChatIds: ReadonlySet<string>
  collapsedParentChatIds: ReadonlySet<string>
  stickyExpandedParentIds: ReadonlySet<string>
  activeParentChatId: string | null
}

export type WorkspaceExpansionInput = {
  expandedWorkspaceIds: ReadonlySet<string>
  collapsedWorkspaceIds: ReadonlySet<string>
  activeWorkspaceId: string | null
}

export type ProjectExpansionInput = {
  projectId: string
  expandedProjectIds: ReadonlySet<string>
  collapsedProjectIds: ReadonlySet<string>
  activeProjectId: string | null
  projectGroupIds: readonly string[]
}

/**
 * Priority: active child context forces open; user collapse wins over sticky/user
 * expansion; otherwise union user expansion, sticky parents, and active ancestors.
 */
export function isParentChatVisibleExpanded(
  parentChatId: string,
  input: ParentChatExpansionInput
): boolean {
  if (input.activeParentChatId === parentChatId) {
    return true
  }

  if (input.collapsedParentChatIds.has(parentChatId)) {
    return false
  }

  return (
    input.expandedParentChatIds.has(parentChatId) || input.stickyExpandedParentIds.has(parentChatId)
  )
}

export function resolveVisibleParentChatIds(input: ParentChatExpansionInput): Set<string> {
  const candidates = new Set(input.expandedParentChatIds)
  for (const parentChatId of input.stickyExpandedParentIds) {
    candidates.add(parentChatId)
  }
  if (input.activeParentChatId) {
    candidates.add(input.activeParentChatId)
  }

  const visible = new Set<string>()
  for (const parentChatId of candidates) {
    if (isParentChatVisibleExpanded(parentChatId, input)) {
      visible.add(parentChatId)
    }
  }

  return visible
}

export function isWorkspaceVisibleExpanded(
  workspaceId: string,
  input: WorkspaceExpansionInput
): boolean {
  if (input.activeWorkspaceId === workspaceId) {
    return true
  }

  if (input.collapsedWorkspaceIds.has(workspaceId)) {
    return false
  }

  return input.expandedWorkspaceIds.has(workspaceId)
}

export function resolveVisibleWorkspaceIds(input: WorkspaceExpansionInput): Set<string> {
  const candidates = new Set(input.expandedWorkspaceIds)
  if (input.activeWorkspaceId) {
    candidates.add(input.activeWorkspaceId)
  }

  const visible = new Set<string>()
  for (const workspaceId of candidates) {
    if (isWorkspaceVisibleExpanded(workspaceId, input)) {
      visible.add(workspaceId)
    }
  }

  return visible
}

export function resolveIsProjectExpanded(input: ProjectExpansionInput): boolean {
  const { projectId, expandedProjectIds, collapsedProjectIds, activeProjectId, projectGroupIds } =
    input

  if (activeProjectId === projectId) {
    return true
  }

  if (collapsedProjectIds.has(projectId)) {
    return false
  }

  if (expandedProjectIds.has(projectId)) {
    return true
  }

  const hasAnyExpanded = projectGroupIds.some(
    (id) => expandedProjectIds.has(id) || id === activeProjectId
  )
  if (!hasAnyExpanded && projectGroupIds.length > 0) {
    const firstProjectId = projectGroupIds[0]!
    if (collapsedProjectIds.has(firstProjectId)) {
      return false
    }
    return firstProjectId === projectId
  }

  return false
}

export function shouldStickyExpandParent(
  parentChatId: string,
  collapsedParentChatIds: ReadonlySet<string>
): boolean {
  return !collapsedParentChatIds.has(parentChatId)
}
