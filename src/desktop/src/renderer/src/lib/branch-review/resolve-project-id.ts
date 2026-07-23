export function resolveBranchReviewProjectId(options: {
  requestedProjectId?: string | null
  activeProjectId: string | null
  projectIds: string[]
}): string | null {
  const requested = options.requestedProjectId?.trim()
  if (requested && options.projectIds.includes(requested)) {
    return requested
  }

  if (options.activeProjectId && options.projectIds.includes(options.activeProjectId)) {
    return options.activeProjectId
  }

  if (options.projectIds.length === 1) {
    return options.projectIds[0] ?? null
  }

  return null
}
