import { describe, expect, it } from 'vitest'

import {
  planNewChatTab,
  resolveWorkspaceForNewTab
} from '@/lib/chat-tabs/resolve-workspace-for-new-tab'
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
    workspaceId: 'ws-1',
    workspacePath: 'E:/proj',
    mode: 'default',
    modelId: null,
    contextSizeId: null,
    reasoningEffortId: null,
    approvalPolicyId: null,
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
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    workspaces: [
      {
        id: 'ws-default',
        projectId: 'project-1',
        path: 'E:/orchi',
        name: 'main',
        isDefault: true,
        kind: 'primary',
        createdAt: new Date().toISOString()
      }
    ],
    ...overrides
  }
}

describe('resolveWorkspaceForNewTab', () => {
  it('inherits workspace from the active chat', () => {
    expect(resolveWorkspaceForNewTab(chat(), [project()])).toEqual({
      workspaceId: 'ws-1',
      workspacePath: 'E:/proj',
      projectId: 'project-1'
    })
  })

  it('falls back to the first project default workspace', () => {
    expect(resolveWorkspaceForNewTab(undefined, [project()])).toEqual({
      workspaceId: 'ws-default',
      workspacePath: 'E:/orchi',
      projectId: 'project-1'
    })
  })

  it('returns null when there is no workspace to inherit', () => {
    expect(resolveWorkspaceForNewTab(undefined, [])).toBeNull()
    expect(resolveWorkspaceForNewTab(chat({ workspaceId: null, projectId: null }), [])).toBeNull()
  })
})

describe('planNewChatTab', () => {
  it('plans create when a project workspace is available', () => {
    expect(planNewChatTab(undefined, [project()])).toEqual({
      kind: 'create',
      workspace: {
        workspaceId: 'ws-default',
        workspacePath: 'E:/orchi',
        projectId: 'project-1'
      }
    })
  })

  it('plans create from the active chat workspace', () => {
    expect(planNewChatTab(chat(), [])).toEqual({
      kind: 'create',
      workspace: {
        workspaceId: 'ws-1',
        workspacePath: 'E:/proj',
        projectId: 'project-1'
      }
    })
  })

  it('plans needsProject when there is no project or inheritable workspace', () => {
    // Fresh DB / empty projects — New Chat must not silently no-op.
    expect(planNewChatTab(undefined, [])).toEqual({ kind: 'needsProject' })
    expect(planNewChatTab(chat({ workspaceId: null, projectId: null }), [])).toEqual({
      kind: 'needsProject'
    })
  })

  it('plans needsProject when the only project has no workspaces', () => {
    expect(planNewChatTab(undefined, [project({ workspaces: [] })])).toEqual({
      kind: 'needsProject'
    })
  })
})
