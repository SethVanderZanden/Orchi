import { describe, expect, it } from 'vitest'

import {
  isSourceChatOnWorktree,
  resolveChatFromSource,
  resolveWorkspaceForChatFromSource
} from '@/lib/chat/resolve-chat-from-source'
import type { ChatThread } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'

function chat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Chat',
    preview: '',
    updatedAt: new Date().toISOString(),
    agentId: 'cursor',
    projectId: 'project-1',
    workspaceId: 'ws-worktree',
    workspacePath: '/tmp/worktrees/feature-a',
    mode: 'default',
    modelId: 'gpt-4',
    contextSizeId: 'large',
    reasoningEffortId: 'high',
    approvalPolicyId: 'auto',
    parentChatId: null,
    planFilePath: null,
    status: 'read',
    lastReadAt: null,
    messages: [],
    ...overrides
  }
}

function project(overrides: Partial<Project> = {}): Project {
  return {
    id: 'project-1',
    name: 'Orchi',
    defaultBaseBranch: 'main',
    defaultWorktreeBranchPattern: 'orchi/{date}-{shortId}',
    gitHostProvider: 'github',
    useWorktreeOnKickoff: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    workspaces: [
      {
        id: 'ws-primary',
        projectId: 'project-1',
        path: '/tmp/orchi',
        name: 'main',
        isDefault: true,
        kind: 'primary',
        branch: null,
        baseBranch: null,
        createdAt: new Date().toISOString()
      },
      {
        id: 'ws-worktree',
        projectId: 'project-1',
        path: '/tmp/worktrees/feature-a',
        name: 'feature-a',
        isDefault: false,
        kind: 'worktree',
        branch: 'feature-a',
        baseBranch: 'main',
        createdAt: new Date().toISOString()
      }
    ],
    ...overrides
  }
}

describe('resolveWorkspaceForChatFromSource', () => {
  it('uses the project primary workspace instead of the source worktree', () => {
    expect(resolveWorkspaceForChatFromSource(chat(), [project()])).toEqual({
      workspaceId: 'ws-primary',
      workspacePath: '/tmp/orchi',
      projectId: 'project-1'
    })
  })

  it('falls back to the source workspace when the project is unknown', () => {
    expect(resolveWorkspaceForChatFromSource(chat({ projectId: null }), [])).toEqual({
      workspaceId: 'ws-worktree',
      workspacePath: '/tmp/worktrees/feature-a',
      projectId: null
    })
  })

  it('returns null when no workspace can be resolved', () => {
    expect(
      resolveWorkspaceForChatFromSource(
        chat({ workspaceId: null, workspacePath: '', projectId: null }),
        []
      )
    ).toBeNull()
  })
})

describe('isSourceChatOnWorktree', () => {
  it('detects when the source chat is on a worktree workspace', () => {
    expect(isSourceChatOnWorktree(chat(), [project()])).toBe(true)
    expect(
      isSourceChatOnWorktree(chat({ workspaceId: 'ws-primary', workspacePath: '/tmp/orchi' }), [
        project()
      ])
    ).toBe(false)
  })
})

describe('resolveChatFromSource', () => {
  it('copies runtime settings and enables worktree when a project exists', () => {
    expect(resolveChatFromSource(chat(), [project()])).toEqual({
      workspace: {
        workspaceId: 'ws-primary',
        workspacePath: '/tmp/orchi',
        projectId: 'project-1'
      },
      draftOptions: {
        mode: 'default',
        agentId: 'cursor',
        modelId: 'gpt-4',
        contextSizeId: 'large',
        reasoningEffortId: 'high',
        approvalPolicyId: 'auto'
      },
      enableWorktree: true
    })
  })

  it('does not enable worktree when the source chat has no project', () => {
    expect(resolveChatFromSource(chat({ projectId: null }), [])).toEqual({
      workspace: {
        workspaceId: 'ws-worktree',
        workspacePath: '/tmp/worktrees/feature-a',
        projectId: null
      },
      draftOptions: {
        mode: 'default',
        agentId: 'cursor',
        modelId: 'gpt-4',
        contextSizeId: 'large',
        reasoningEffortId: 'high',
        approvalPolicyId: 'auto'
      },
      enableWorktree: false
    })
  })
})
