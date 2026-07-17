import { describe, expect, it } from 'vitest'

import type { ChatThread } from '@/lib/chat/types'

import { mergeChatLists } from './merge-chat-lists'

function makeChat(overrides: Partial<ChatThread> & Pick<ChatThread, 'id'>): ChatThread {
  return {
    id: overrides.id,
    title: overrides.title ?? overrides.id,
    preview: overrides.preview ?? '',
    updatedAt: overrides.updatedAt ?? '2026-01-01T00:00:00.000Z',
    agentId: 'cursor',
    projectId: overrides.projectId ?? null,
    workspaceId: overrides.workspaceId ?? null,
    workspacePath: '/workspace',
    mode: 'default',
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

describe('mergeChatLists', () => {
  it('retains cache-only entries not present in incoming', () => {
    const cached = [makeChat({ id: 'optimistic-child', title: 'Kickoff child' })]
    const incoming: ChatThread[] = []

    expect(mergeChatLists(cached, incoming)).toEqual(cached)
  })

  it('lets incoming entries overwrite cached entries with the same id', () => {
    const cached = [
      makeChat({ id: 'a', title: 'Old title', updatedAt: '2026-01-01T00:00:00.000Z' })
    ]
    const incoming = [
      makeChat({ id: 'a', title: 'New title', updatedAt: '2026-01-02T00:00:00.000Z' })
    ]

    const merged = mergeChatLists(cached, incoming)

    expect(merged).toHaveLength(1)
    expect(merged[0].title).toBe('New title')
    expect(merged[0].updatedAt).toBe('2026-01-02T00:00:00.000Z')
  })

  it('preserves cached messages when incoming summaries have empty messages', () => {
    const cached = [
      makeChat({
        id: 'a',
        title: 'Cached title',
        updatedAt: '2026-01-03T00:00:00.000Z',
        messages: [
          {
            id: 'm1',
            role: 'user',
            content: 'Hello',
            createdAt: '2026-01-03T00:00:00.000Z',
            status: 'complete'
          }
        ]
      })
    ]
    const incoming = [
      makeChat({
        id: 'a',
        title: 'Fresh title',
        updatedAt: '2026-01-02T00:00:00.000Z',
        messages: []
      })
    ]

    const merged = mergeChatLists(cached, incoming)

    expect(merged[0].title).toBe('Fresh title')
    expect(merged[0].messages).toHaveLength(1)
    expect(merged[0].messages[0]?.content).toBe('Hello')
  })

  it('sorts merged results by updatedAt descending', () => {
    const cached = [
      makeChat({ id: 'old', updatedAt: '2026-01-01T00:00:00.000Z' }),
      makeChat({ id: 'newest', updatedAt: '2026-01-03T00:00:00.000Z' })
    ]
    const incoming = [makeChat({ id: 'middle', updatedAt: '2026-01-02T00:00:00.000Z' })]

    expect(mergeChatLists(cached, incoming).map((chat) => chat.id)).toEqual([
      'newest',
      'middle',
      'old'
    ])
  })
})
