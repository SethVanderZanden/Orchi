import { describe, expect, it } from 'vitest'

import {
  findProjectForWorkspace,
  findWorkspaceInProjects,
  resolveWorkspaceContext
} from '@/lib/projects/find-workspace'
import type { Project } from '@/lib/projects/types'

const projects: Project[] = [
  {
    id: 'project-a',
    name: 'Alpha',
    defaultBaseBranch: 'main',
    defaultWorktreeBranchPattern: 'orchi/{date}-{shortId}',
    gitHostProvider: 'github',
    useWorktreeOnKickoff: true,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    workspaces: [
      {
        id: 'workspace-a1',
        projectId: 'project-a',
        path: '/alpha',
        name: 'Alpha root',
        isDefault: true,
        kind: 'primary',
        branch: null,
        baseBranch: null,
        createdAt: '2026-01-01T00:00:00.000Z'
      },
      {
        id: 'workspace-a2',
        projectId: 'project-a',
        path: '/alpha/worktree',
        name: 'Alpha worktree',
        isDefault: false,
        kind: 'worktree',
        branch: 'feature/a',
        baseBranch: 'main',
        createdAt: '2026-01-01T00:00:00.000Z'
      }
    ]
  },
  {
    id: 'project-b',
    name: 'Beta',
    defaultBaseBranch: 'main',
    defaultWorktreeBranchPattern: 'orchi/{date}-{shortId}',
    gitHostProvider: 'github',
    useWorktreeOnKickoff: true,
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    workspaces: [
      {
        id: 'workspace-b1',
        projectId: 'project-b',
        path: '/beta',
        name: 'Beta root',
        isDefault: true,
        kind: 'primary',
        branch: null,
        baseBranch: null,
        createdAt: '2026-01-01T00:00:00.000Z'
      }
    ]
  }
]

describe('findWorkspaceInProjects', () => {
  it('finds workspaces across projects', () => {
    expect(findWorkspaceInProjects(projects, 'workspace-a2')?.name).toBe('Alpha worktree')
    expect(findWorkspaceInProjects(projects, 'workspace-b1')?.path).toBe('/beta')
    expect(findWorkspaceInProjects(projects, 'missing')).toBeUndefined()
    expect(findWorkspaceInProjects(projects, null)).toBeUndefined()
  })
})

describe('findProjectForWorkspace', () => {
  it('returns the owning project for a workspace id', () => {
    expect(findProjectForWorkspace(projects, 'workspace-b1')?.name).toBe('Beta')
    expect(findProjectForWorkspace(projects, 'missing')).toBeUndefined()
  })
})

describe('resolveWorkspaceContext', () => {
  it('resolves project and workspace names from ids', () => {
    expect(
      resolveWorkspaceContext(projects, 'project-a', 'workspace-a2', null, null)
    ).toMatchObject({
      resolvedProjectName: 'Alpha',
      resolvedWorkspaceName: 'Alpha worktree'
    })
  })

  it('prefers explicit names when provided', () => {
    expect(
      resolveWorkspaceContext(projects, 'project-a', 'workspace-a2', 'Custom workspace', 'Custom project')
    ).toMatchObject({
      resolvedProjectName: 'Custom project',
      resolvedWorkspaceName: 'Custom workspace'
    })
  })
})
