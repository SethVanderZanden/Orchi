import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react'

const EXPANDED_PROJECTS_KEY = 'orchi.expandedProjects.v1'

type WorkspaceLayoutContextValue = {
  expandedProjectIds: ReadonlySet<string>
  isProjectExpanded: (projectId: string) => boolean
  toggleProjectExpanded: (projectId: string) => void
  ensureProjectExpanded: (projectId: string) => void
}

const WorkspaceLayoutContext = createContext<WorkspaceLayoutContextValue | null>(null)

function readExpandedProjectIds(): Set<string> {
  try {
    const raw = localStorage.getItem(EXPANDED_PROJECTS_KEY)
    if (!raw) {
      return new Set()
    }

    const parsed = JSON.parse(raw) as unknown
    if (!Array.isArray(parsed)) {
      return new Set()
    }

    return new Set(parsed.filter((entry): entry is string => typeof entry === 'string'))
  } catch {
    return new Set()
  }
}

export function WorkspaceLayoutProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const [expandedProjectIds, setExpandedProjectIds] = useState<Set<string>>(readExpandedProjectIds)

  useEffect(() => {
    try {
      localStorage.setItem(EXPANDED_PROJECTS_KEY, JSON.stringify([...expandedProjectIds]))
    } catch {
      // ignore persistence errors
    }
  }, [expandedProjectIds])

  const isProjectExpanded = useCallback(
    (projectId: string) => expandedProjectIds.has(projectId),
    [expandedProjectIds]
  )

  const toggleProjectExpanded = useCallback((projectId: string) => {
    setExpandedProjectIds((current) => {
      const next = new Set(current)
      if (next.has(projectId)) {
        next.delete(projectId)
      } else {
        next.add(projectId)
      }

      return next
    })
  }, [])

  const ensureProjectExpanded = useCallback((projectId: string) => {
    setExpandedProjectIds((current) => {
      if (current.has(projectId)) {
        return current
      }

      const next = new Set(current)
      next.add(projectId)
      return next
    })
  }, [])

  const value = useMemo(
    () => ({
      expandedProjectIds,
      isProjectExpanded,
      toggleProjectExpanded,
      ensureProjectExpanded
    }),
    [expandedProjectIds, isProjectExpanded, toggleProjectExpanded, ensureProjectExpanded]
  )

  return (
    <WorkspaceLayoutContext.Provider value={value}>{children}</WorkspaceLayoutContext.Provider>
  )
}

export function useWorkspaceLayout(): WorkspaceLayoutContextValue {
  const context = useContext(WorkspaceLayoutContext)
  if (!context) {
    throw new Error('useWorkspaceLayout must be used within WorkspaceLayoutProvider')
  }

  return context
}
