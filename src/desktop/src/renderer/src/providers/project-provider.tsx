import { createContext, useCallback, useContext, useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import {
  createProject,
  deleteProject,
  listProjects,
  updateProject
} from '@/lib/projects/api'
import { migrateLocalWorkspacesIfNeeded } from '@/lib/projects/migrate-local-workspaces'
import { workspaceNameFromPath } from '@/lib/projects/paths'
import type { Project } from '@/lib/projects/types'
import { projectKeys } from '@/lib/query-keys'

type ProjectContextValue = {
  projects: Project[]
  isLoadingProjects: boolean
  isPendingProjects: boolean
  projectsError: Error | null
  refetchProjects: () => Promise<unknown>
  addProject: (path: string) => Promise<Project | null>
  removeProject: (projectId: string) => Promise<void>
  renameProject: (projectId: string, name: string) => Promise<Project | null>
  pickDirectory: () => Promise<string | null>
}

const ProjectContext = createContext<ProjectContextValue | null>(null)

export function ProjectProvider({ children }: { children: React.ReactNode }): React.JSX.Element {
  const queryClient = useQueryClient()

  const projectsQuery = useQuery({
    queryKey: projectKeys.lists(),
    queryFn: async () => {
      await migrateLocalWorkspacesIfNeeded()
      return listProjects()
    },
    refetchOnMount: 'always',
    retry: 3,
    retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000)
  })

  const refetchProjects = useCallback(async () => {
    return projectsQuery.refetch()
  }, [projectsQuery])

  const addProjectMutation = useMutation({
    mutationFn: async (path: string) => {
      const name = workspaceNameFromPath(path)
      return createProject({ name, defaultWorkspacePath: path })
    },
    onSuccess: (project) => {
      queryClient.setQueryData<Project[]>(projectKeys.lists(), (current = []) => {
        const existing = current.find((entry) => entry.id === project.id)
        if (existing) {
          return current.map((entry) => (entry.id === project.id ? project : entry))
        }

        return [...current, project]
      })
    }
  })

  const removeProjectMutation = useMutation({
    mutationFn: deleteProject,
    onSuccess: (_, projectId) => {
      queryClient.setQueryData<Project[]>(projectKeys.lists(), (current = []) =>
        current.filter((project) => project.id !== projectId)
      )
      queryClient.removeQueries({ queryKey: projectKeys.detail(projectId) })
    }
  })

  const renameProjectMutation = useMutation({
    mutationFn: ({ projectId, name }: { projectId: string; name: string }) =>
      updateProject(projectId, { name }),
    onSuccess: (project) => {
      queryClient.setQueryData<Project[]>(projectKeys.lists(), (current = []) =>
        current.map((entry) => (entry.id === project.id ? project : entry))
      )
      queryClient.setQueryData(projectKeys.detail(project.id), project)
    }
  })

  const addProject = useCallback(
    async (path: string) => {
      const trimmed = path.trim()
      if (!trimmed) {
        return null
      }

      return addProjectMutation.mutateAsync(trimmed)
    },
    [addProjectMutation]
  )

  const removeProject = useCallback(
    async (projectId: string) => {
      await removeProjectMutation.mutateAsync(projectId)
    },
    [removeProjectMutation]
  )

  const renameProject = useCallback(
    async (projectId: string, name: string) => {
      const trimmed = name.trim()
      if (!trimmed) {
        return null
      }

      return renameProjectMutation.mutateAsync({ projectId, name: trimmed })
    },
    [renameProjectMutation]
  )

  const pickDirectory = useCallback(async () => {
    if (!window.api?.openDirectory) {
      return null
    }

    return window.api.openDirectory()
  }, [])

  const projects = projectsQuery.data ?? []

  const value = useMemo<ProjectContextValue>(
    () => ({
      projects,
      isLoadingProjects: projectsQuery.isLoading,
      isPendingProjects: projectsQuery.isPending,
      projectsError: projectsQuery.error as Error | null,
      refetchProjects,
      addProject,
      removeProject,
      renameProject,
      pickDirectory
    }),
    [
      projects,
      projectsQuery.isLoading,
      projectsQuery.isPending,
      projectsQuery.error,
      refetchProjects,
      addProject,
      removeProject,
      renameProject,
      pickDirectory
    ]
  )

  return <ProjectContext.Provider value={value}>{children}</ProjectContext.Provider>
}

export function useProjects(): ProjectContextValue {
  const context = useContext(ProjectContext)

  if (!context) {
    throw new Error('useProjects must be used within ProjectProvider')
  }

  return context
}

/** @deprecated Use useProjects instead */
export function useWorkspaces(): {
  workspaces: Array<{ id: string; path: string; name: string; addedAt: string }>
  addWorkspace: (path: string) => Promise<{ id: string; path: string; name: string; addedAt: string } | null>
  removeWorkspace: (id: string) => Promise<void>
  renameWorkspace: (id: string, name: string) => Promise<{ id: string; path: string; name: string; addedAt: string } | null>
  refreshWorkspaces: () => Promise<unknown>
  pickDirectory: () => Promise<string | null>
} {
  const {
    projects,
    addProject,
    removeProject,
    renameProject,
    refetchProjects,
    pickDirectory
  } = useProjects()

  const workspaces = useMemo(
    () =>
      projects.map((project) => {
        const defaultWorkspace =
          project.workspaces.find((workspace) => workspace.isDefault) ?? project.workspaces[0]

        return {
          id: project.id,
          path: defaultWorkspace?.path ?? '',
          name: project.name,
          addedAt: project.createdAt
        }
      }),
    [projects]
  )

  return {
    workspaces,
    addWorkspace: async (path: string) => {
      const project = await addProject(path)
      if (!project) {
        return null
      }

      const defaultWorkspace =
        project.workspaces.find((workspace) => workspace.isDefault) ?? project.workspaces[0]

      return {
        id: project.id,
        path: defaultWorkspace?.path ?? path,
        name: project.name,
        addedAt: project.createdAt
      }
    },
    removeWorkspace: removeProject,
    renameWorkspace: async (id: string, name: string) => {
      const project = await renameProject(id, name)
      if (!project) {
        return null
      }

      const defaultWorkspace =
        project.workspaces.find((workspace) => workspace.isDefault) ?? project.workspaces[0]

      return {
        id: project.id,
        path: defaultWorkspace?.path ?? '',
        name: project.name,
        addedAt: project.createdAt
      }
    },
    refreshWorkspaces: refetchProjects,
    pickDirectory
  }
}
