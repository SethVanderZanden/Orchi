import { describe, expect, it } from 'vitest'

import type { Project } from '@/lib/projects/types'

function findWorkspace(
  projects: Project[],
  workspaceId: string | null
): Project['workspaces'][number] | undefined {
  if (!workspaceId) {
    return undefined
  }

  for (const project of projects) {
    const workspace = project.workspaces.find((entry) => entry.id === workspaceId)
    if (workspace) {
      return workspace
    }
  }

  return undefined
}

describe('chat workspace context helpers', () => {
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

  it('finds workspaces across projects', () => {
    expect(findWorkspace(projects, 'workspace-a2')?.name).toBe('Alpha worktree')
    expect(findWorkspace(projects, 'workspace-b1')?.path).toBe('/beta')
    expect(findWorkspace(projects, 'missing')).toBeUndefined()
    expect(findWorkspace(projects, null)).toBeUndefined()
  })
})
