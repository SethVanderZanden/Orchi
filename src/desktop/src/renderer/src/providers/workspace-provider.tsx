import { createContext, useCallback, useContext, useMemo, useState } from 'react'

import {
  addWorkspace as addWorkspaceToStore,
  listWorkspaces,
  removeWorkspace as removeWorkspaceFromStore,
  renameWorkspace as renameWorkspaceInStore,
  type Workspace
} from '@/lib/workspaces/store'

type WorkspaceContextValue = {
  workspaces: Workspace[]
  addWorkspace: (path: string) => Workspace | null
  removeWorkspace: (id: string) => void
  renameWorkspace: (id: string, name: string) => Workspace | null
  refreshWorkspaces: () => void
  pickDirectory: () => Promise<string | null>
}

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null)

export function WorkspaceProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const [workspaces, setWorkspaces] = useState<Workspace[]>(() => listWorkspaces())

  const refreshWorkspaces = useCallback(() => {
    setWorkspaces(listWorkspaces())
  }, [])

  const addWorkspace = useCallback(
    (path: string) => {
      const workspace = addWorkspaceToStore(path)
      refreshWorkspaces()
      return workspace
    },
    [refreshWorkspaces]
  )

  const removeWorkspace = useCallback(
    (id: string) => {
      removeWorkspaceFromStore(id)
      refreshWorkspaces()
    },
    [refreshWorkspaces]
  )

  const renameWorkspace = useCallback(
    (id: string, name: string) => {
      const workspace = renameWorkspaceInStore(id, name)
      refreshWorkspaces()
      return workspace
    },
    [refreshWorkspaces]
  )

  const pickDirectory = useCallback(async () => {
    if (!window.api?.openDirectory) {
      return null
    }

    return window.api.openDirectory()
  }, [])

  const value = useMemo<WorkspaceContextValue>(
    () => ({
      workspaces,
      addWorkspace,
      removeWorkspace,
      renameWorkspace,
      refreshWorkspaces,
      pickDirectory
    }),
    [workspaces, addWorkspace, removeWorkspace, renameWorkspace, refreshWorkspaces, pickDirectory]
  )

  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>
}

export function useWorkspaces(): WorkspaceContextValue {
  const context = useContext(WorkspaceContext)

  if (!context) {
    throw new Error('useWorkspaces must be used within WorkspaceProvider')
  }

  return context
}
