/* eslint-disable react-refresh/only-export-components */
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
const LEGACY_EXPANDED_PROJECT_IDS_KEY = 'orchi.expandedProjectIds'
const LEGACY_EXPANDED_WORKSPACE_IDS_KEY = 'orchi.expandedWorkspaceIds'
const SIDEBAR_WIDTH_KEY = 'orchi.sidebarWidth.v1'

export const SIDEBAR_DEFAULT_WIDTH = 300
export const SIDEBAR_MIN_WIDTH = 240
export const SIDEBAR_MAX_WIDTH = 520

type ProjectLayoutContextValue = {
  expandedProjectIds: ReadonlySet<string>
  isProjectExpanded: (projectId: string) => boolean
  toggleProjectExpanded: (projectId: string) => void
  ensureProjectExpanded: (projectId: string) => void
  sidebarWidth: number
  setSidebarWidth: (width: number) => void
}

const ProjectLayoutContext = createContext<ProjectLayoutContextValue | null>(null)

function parseExpandedProjectIds(raw: string): Set<string> {
  const parsed = JSON.parse(raw) as unknown
  if (!Array.isArray(parsed)) {
    return new Set()
  }

  return new Set(parsed.filter((entry): entry is string => typeof entry === 'string'))
}

function readExpandedProjectIds(): Set<string> {
  try {
    const current = localStorage.getItem(EXPANDED_PROJECTS_KEY)
    if (current) {
      return parseExpandedProjectIds(current)
    }

    for (const legacyKey of [LEGACY_EXPANDED_PROJECT_IDS_KEY, LEGACY_EXPANDED_WORKSPACE_IDS_KEY]) {
      const legacy = localStorage.getItem(legacyKey)
      if (!legacy) {
        continue
      }

      localStorage.setItem(EXPANDED_PROJECTS_KEY, legacy)
      localStorage.removeItem(legacyKey)
      return parseExpandedProjectIds(legacy)
    }

    return new Set()
  } catch {
    return new Set()
  }
}

function readSidebarWidth(): number {
  try {
    const raw = localStorage.getItem(SIDEBAR_WIDTH_KEY)
    if (!raw) {
      return SIDEBAR_DEFAULT_WIDTH
    }

    const parsed = Number.parseInt(raw, 10)
    if (!Number.isFinite(parsed)) {
      return SIDEBAR_DEFAULT_WIDTH
    }

    return Math.min(SIDEBAR_MAX_WIDTH, Math.max(SIDEBAR_MIN_WIDTH, parsed))
  } catch {
    return SIDEBAR_DEFAULT_WIDTH
  }
}

export function ProjectLayoutProvider({ children }: { children: ReactNode }): React.JSX.Element {
  const [expandedProjectIds, setExpandedProjectIds] = useState<Set<string>>(readExpandedProjectIds)
  const [sidebarWidth, setSidebarWidthState] = useState<number>(readSidebarWidth)

  useEffect(() => {
    try {
      localStorage.setItem(EXPANDED_PROJECTS_KEY, JSON.stringify([...expandedProjectIds]))
    } catch {
      // ignore persistence errors
    }
  }, [expandedProjectIds])

  useEffect(() => {
    try {
      localStorage.setItem(SIDEBAR_WIDTH_KEY, String(sidebarWidth))
    } catch {
      // ignore persistence errors
    }
  }, [sidebarWidth])

  const setSidebarWidth = useCallback((width: number) => {
    setSidebarWidthState(Math.min(SIDEBAR_MAX_WIDTH, Math.max(SIDEBAR_MIN_WIDTH, width)))
  }, [])

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
      ensureProjectExpanded,
      sidebarWidth,
      setSidebarWidth
    }),
    [
      expandedProjectIds,
      isProjectExpanded,
      toggleProjectExpanded,
      ensureProjectExpanded,
      sidebarWidth,
      setSidebarWidth
    ]
  )

  return <ProjectLayoutContext.Provider value={value}>{children}</ProjectLayoutContext.Provider>
}

export function useProjectLayout(): ProjectLayoutContextValue {
  const context = useContext(ProjectLayoutContext)
  if (!context) {
    throw new Error('useProjectLayout must be used within ProjectLayoutProvider')
  }

  return context
}
