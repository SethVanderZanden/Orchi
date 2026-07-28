import { describe, expect, it } from 'vitest'

import { resolveChatFromSource } from '@/lib/chat/resolve-chat-from-source'
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

describe('resolveChatFromSource', () => {
  it('keeps the source chat workspace so parallel agents share a worktree', () => {
    expect(resolveChatFromSource(chat(), [project()])).toEqual({
      workspace: {
        workspaceId: 'ws-worktree',
        workspacePath: '/tmp/worktrees/feature-a',
        projectId: 'project-1'
      },
      draftOptions: {
        mode: 'default',
        agentId: 'cursor',
        modelId: 'gpt-4',
        contextSizeId: 'large',
        reasoningEffortId: 'high',
        approvalPolicyId: 'auto'
      }
    })
  })

  it('returns null when the source chat has no workspace to inherit', () => {
    expect(
      resolveChatFromSource(
        chat({ workspaceId: null, workspacePath: '', projectId: null }),
        []
      )
    ).toBeNull()
  })
})
