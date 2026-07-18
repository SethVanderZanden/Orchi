import { describe, expect, it } from 'vitest'

import type { ChatThread } from '@/lib/chat/types'

import {
  filterProjectGroups,
  getDefaultWorkspace,
  groupChatsByProject,
  groupContainsChat,
  ORPHAN_GROUP_ID,
  resolveWorkspaceIdForNewChat
} from './group-chats'
import type { Project } from './types'

function makeChat(overrides: Partial<ChatThread> & Pick<ChatThread, 'id'>): ChatThread {
  return {
    id: overrides.id,
    title: overrides.title ?? overrides.id,
    preview: overrides.preview ?? '',
    updatedAt: overrides.updatedAt ?? '2026-01-01T00:00:00.000Z',
    agentId: 'cursor',
    projectId: overrides.projectId ?? null,
    workspaceId: overrides.workspaceId ?? null,
    workspacePath: overrides.workspacePath ?? 'E:\\Projects\\Orchi',
    mode: overrides.mode ?? 'default',
    modelId: overrides.modelId ?? null,
    contextSizeId: overrides.contextSizeId ?? null,
    reasoningEffortId: overrides.reasoningEffortId ?? null,
    approvalPolicyId: overrides.approvalPolicyId ?? null,
    parentChatId: overrides.parentChatId ?? null,
    planFilePath: overrides.planFilePath ?? null,
    status: overrides.status ?? 'read',
    lastReadAt: overrides.lastReadAt ?? null,
    messages: overrides.messages ?? []
  }
}

function makeProject(overrides: Partial<Project> & Pick<Project, 'id' | 'name'>): Project {
  return {
    id: overrides.id,
    name: overrides.name,
    defaultBaseBranch: overrides.defaultBaseBranch ?? 'main',
    defaultWorktreeBranchPattern:
      overrides.defaultWorktreeBranchPattern ?? 'orchi/{date}-{shortId}',
    gitHostProvider: overrides.gitHostProvider ?? 'github',
    useWorktreeOnKickoff: overrides.useWorktreeOnKickoff ?? true,
    createdAt: overrides.createdAt ?? '2026-01-01T00:00:00.000Z',
    updatedAt: overrides.updatedAt ?? '2026-01-01T00:00:00.000Z',
    workspaces: overrides.workspaces ?? [
      {
        id: 'ws-default',
        projectId: overrides.id,
        path: 'E:\\Projects\\Orchi',
        name: 'Orchi',
        isDefault: true,
        branch: null,
        baseBranch: null,
        kind: 'primary',
        createdAt: '2026-01-01T00:00:00.000Z'
      }
    ]
  }
}

describe('getDefaultWorkspace', () => {
  it('returns the default workspace when marked', () => {
    const project = makeProject({
      id: 'p1',
      name: 'Orchi',
      workspaces: [
        {
          id: 'ws-main',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi',
          name: 'main',
          isDefault: true,
          kind: 'primary',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        },
        {
          id: 'ws-feature',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi-feature',
          name: 'feature',
          isDefault: false,
          kind: 'worktree',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        }
      ]
    })

    expect(getDefaultWorkspace(project)?.id).toBe('ws-main')
  })

  it('falls back to the first workspace when none is default', () => {
    const project = makeProject({
      id: 'p1',
      name: 'Orchi',
      workspaces: [
        {
          id: 'ws-first',
          projectId: 'p1',
          path: 'E:\\Projects\\First',
          name: 'first',
          isDefault: false,
          kind: 'primary',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        }
      ]
    })

    expect(getDefaultWorkspace(project)?.id).toBe('ws-first')
  })
})

describe('groupChatsByProject', () => {
  it('flattens chats under a project with a single workspace', () => {
    const project = makeProject({ id: 'p1', name: 'Orchi' })
    const chats = [
      makeChat({ id: 'c1', projectId: 'p1', workspaceId: 'ws-default' }),
      makeChat({ id: 'c2', projectId: 'p1', workspaceId: 'ws-default' })
    ]

    const groups = groupChatsByProject([project], chats)

    expect(groups).toHaveLength(1)
    expect(groups[0]?.isFlat).toBe(true)
    expect(groups[0]?.chatNodes).toHaveLength(2)
    expect(groups[0]?.workspaceGroups).toEqual([])
  })

  it('creates workspace sub-groups when a project has multiple workspaces', () => {
    const project = makeProject({
      id: 'p1',
      name: 'Orchi',
      workspaces: [
        {
          id: 'ws-main',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi',
          name: 'main',
          isDefault: true,
          kind: 'primary',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        },
        {
          id: 'ws-feature',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi-feature',
          name: 'feature-auth',
          isDefault: false,
          kind: 'worktree',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        }
      ]
    })

    const chats = [
      makeChat({
        id: 'c-main',
        projectId: 'p1',
        workspaceId: 'ws-main',
        workspacePath: 'E:\\Projects\\Orchi'
      }),
      makeChat({
        id: 'c-feature',
        projectId: 'p1',
        workspaceId: 'ws-feature',
        workspacePath: 'E:\\Projects\\Orchi-feature'
      })
    ]

    const groups = groupChatsByProject([project], chats)

    expect(groups).toHaveLength(1)
    expect(groups[0]?.isFlat).toBe(false)
    expect(groups[0]?.workspaceGroups).toHaveLength(2)
    expect(groups[0]?.workspaceGroups[0]?.chatNodes).toHaveLength(1)
    expect(groups[0]?.workspaceGroups[1]?.chatNodes).toHaveLength(1)
  })

  it('matches chats by workspace path when ids are null', () => {
    const project = makeProject({ id: 'p1', name: 'Orchi' })
    const chats = [makeChat({ id: 'c1', workspacePath: 'E:\\Projects\\Orchi' })]

    const groups = groupChatsByProject([project], chats)

    expect(groups).toHaveLength(1)
    expect(groups[0]?.chatNodes).toHaveLength(1)
  })

  it('places unmatched chats in the Other group', () => {
    const project = makeProject({ id: 'p1', name: 'Orchi' })
    const chats = [makeChat({ id: 'orphan', workspacePath: 'E:\\Projects\\Unknown' })]

    const groups = groupChatsByProject([project], chats)

    expect(groups).toHaveLength(2)
    const orphanGroup = groups.find((group) => group.id === ORPHAN_GROUP_ID)
    expect(orphanGroup?.isOrphan).toBe(true)
    expect(orphanGroup?.chatNodes).toHaveLength(1)
  })

  it('treats chats with unknown project ids as orphans', () => {
    const project = makeProject({ id: 'p1', name: 'Orchi' })
    const chats = [
      makeChat({
        id: 'missing-project',
        projectId: 'missing',
        workspaceId: 'missing-ws',
        workspacePath: 'E:\\Projects\\Orchi'
      })
    ]

    const groups = groupChatsByProject([project], chats)
    const orphanGroup = groups.find((group) => group.id === ORPHAN_GROUP_ID)

    expect(orphanGroup?.chatNodes).toHaveLength(1)
  })
})

describe('filterProjectGroups', () => {
  it('filters flat project chats by search query', () => {
    const project = makeProject({ id: 'p1', name: 'Orchi' })
    const chats = [
      makeChat({ id: 'c1', title: 'Auth refactor', projectId: 'p1', workspaceId: 'ws-default' }),
      makeChat({ id: 'c2', title: 'UI polish', projectId: 'p1', workspaceId: 'ws-default' })
    ]

    const groups = filterProjectGroups(groupChatsByProject([project], chats), 'auth')

    expect(groups).toHaveLength(1)
    expect(groups[0]?.chatNodes).toHaveLength(1)
    expect(groups[0]?.chatNodes[0]?.chat.id).toBe('c1')
  })
})

describe('groupContainsChat', () => {
  it('finds chats nested under workspace sub-groups', () => {
    const project = makeProject({
      id: 'p1',
      name: 'Orchi',
      workspaces: [
        {
          id: 'ws-main',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi',
          name: 'main',
          isDefault: true,
          kind: 'primary',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        },
        {
          id: 'ws-feature',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi-feature',
          name: 'feature-auth',
          isDefault: false,
          kind: 'worktree',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        }
      ]
    })

    const chats = [
      makeChat({
        id: 'c-feature',
        projectId: 'p1',
        workspaceId: 'ws-feature',
        workspacePath: 'E:\\Projects\\Orchi-feature'
      })
    ]

    const groups = groupChatsByProject([project], chats)
    expect(groupContainsChat(groups[0]!, 'c-feature')).toBe(true)
  })

  it('finds review chats nested under implementation children', () => {
    const project = makeProject({ id: 'p1', name: 'Orchi' })
    const chats = [
      makeChat({
        id: 'parent',
        projectId: 'p1',
        workspaceId: 'ws-default'
      }),
      makeChat({
        id: 'impl',
        projectId: 'p1',
        workspaceId: 'ws-default',
        parentChatId: 'parent',
        planFilePath: '.orchi/plan-auth-refactor.md'
      }),
      makeChat({
        id: 'review',
        mode: 'review',
        projectId: 'p1',
        workspaceId: 'ws-default',
        parentChatId: 'parent',
        planFilePath: '.orchi/review-auth-refactor.md'
      })
    ]

    const groups = groupChatsByProject([project], chats)
    expect(groupContainsChat(groups[0]!, 'review')).toBe(true)
  })
})

describe('resolveWorkspaceIdForNewChat', () => {
  it('returns the default workspace for flat projects', () => {
    const group = groupChatsByProject([makeProject({ id: 'p1', name: 'Orchi' })], [])[0]!

    expect(resolveWorkspaceIdForNewChat(group)).toBe('ws-default')
  })

  it('returns the selected workspace sub-group id', () => {
    const project = makeProject({
      id: 'p1',
      name: 'Orchi',
      workspaces: [
        {
          id: 'ws-main',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi',
          name: 'main',
          isDefault: true,
          kind: 'primary',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        },
        {
          id: 'ws-feature',
          projectId: 'p1',
          path: 'E:\\Projects\\Orchi-feature',
          name: 'feature-auth',
          isDefault: false,
          kind: 'worktree',
          branch: null,
          baseBranch: null,
          createdAt: '2026-01-01T00:00:00.000Z'
        }
      ]
    })

    const group = groupChatsByProject([project], [])[0]!

    expect(resolveWorkspaceIdForNewChat(group, 'ws-feature')).toBe('ws-feature')
  })
})
