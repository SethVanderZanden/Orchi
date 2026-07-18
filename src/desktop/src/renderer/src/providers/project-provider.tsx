/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useMemo } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { createProject, deleteProject, listProjects, updateProject } from '@/lib/projects/api'
import { migrateLocalWorkspacesIfNeeded } from '@/lib/projects/migrate-local-workspaces'
import { workspaceNameFromPath } from '@/lib/projects/paths'
import type { Project, UpdateProjectRequest } from '@/lib/projects/types'
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
  updateProjectSettings: (
    projectId: string,
    request: UpdateProjectRequest
  ) => Promise<Project | null>
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

  const updateProjectMutation = useMutation({
    mutationFn: ({ projectId, request }: { projectId: string; request: UpdateProjectRequest }) =>
      updateProject(projectId, request),
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

  const updateProjectSettings = useCallback(
    async (projectId: string, request: UpdateProjectRequest) => {
      return updateProjectMutation.mutateAsync({ projectId, request })
    },
    [updateProjectMutation]
  )

  const pickDirectory = useCallback(async () => {
    if (!window.api?.openDirectory) {
      return null
    }

    return window.api.openDirectory()
  }, [])

  const value = useMemo<ProjectContextValue>(
    () => ({
      projects: projectsQuery.data ?? [],
      isLoadingProjects: projectsQuery.isLoading,
      isPendingProjects: projectsQuery.isPending,
      projectsError: projectsQuery.error as Error | null,
      refetchProjects,
      addProject,
      removeProject,
      renameProject,
      updateProjectSettings,
      pickDirectory
    }),
    [
      projectsQuery.data,
      projectsQuery.isLoading,
      projectsQuery.isPending,
      projectsQuery.error,
      refetchProjects,
      addProject,
      removeProject,
      renameProject,
      updateProjectSettings,
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
