import { describe, expect, it } from 'vitest'

import { buildChatFinderGroups } from '@/lib/chat-finder/build-chat-finder-groups'
import type { ChatThread } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'

function chat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'c1',
    title: 'Chat',
    preview: '',
    updatedAt: new Date().toISOString(),
    agentId: 'cursor',
    projectId: 'p1',
    workspaceId: 'w1',
    workspacePath: 'E:/a',
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

function project(id: string, name: string): Project {
  return {
    id,
    name,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    workspaces: []
  }
}

describe('buildChatFinderGroups', () => {
  it('builds Recent then per-project groups', () => {
    const chats = [
      chat({ id: '1', projectId: 'p2', title: 'Second' }),
      chat({ id: '2', projectId: 'p1', title: 'First' }),
      chat({ id: '3', projectId: null, title: 'Orphan' })
    ]
    const groups = buildChatFinderGroups(chats, [project('p1', 'Alpha'), project('p2', 'Beta')])

    expect(groups.map((group) => group.heading)).toEqual(['Recent', 'Alpha', 'Beta', 'Other'])
    expect(groups[0]?.chats).toHaveLength(3)
    expect(groups.find((group) => group.heading === 'Alpha')?.chats.map((c) => c.id)).toEqual(['2'])
  })
})
