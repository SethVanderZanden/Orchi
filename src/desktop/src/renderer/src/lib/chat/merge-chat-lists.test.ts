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
    parentChatId: overrides.parentChatId ?? null,
    planFilePath: overrides.planFilePath ?? null,
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
    const cached = [makeChat({ id: 'a', title: 'Old title', updatedAt: '2026-01-01T00:00:00.000Z' })]
    const incoming = [
      makeChat({ id: 'a', title: 'New title', updatedAt: '2026-01-02T00:00:00.000Z' })
    ]

    const merged = mergeChatLists(cached, incoming)

    expect(merged).toHaveLength(1)
    expect(merged[0].title).toBe('New title')
    expect(merged[0].updatedAt).toBe('2026-01-02T00:00:00.000Z')
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
