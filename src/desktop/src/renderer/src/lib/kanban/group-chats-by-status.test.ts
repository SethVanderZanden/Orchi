import { describe, expect, it } from 'vitest'

import { groupChatsByStatus } from './group-chats-by-status'
import type { ChatThread } from '@/lib/chat/types'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Chat',
    preview: '',
    updatedAt: '2026-07-16T12:00:00.000Z',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
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

describe('groupChatsByStatus', () => {
  it('returns three columns in board order', () => {
    const columns = groupChatsByStatus([])
    expect(columns.map((c) => c.id)).toEqual(['processing', 'readyToRead', 'done'])
    expect(columns.map((c) => c.title)).toEqual(['Processing', 'Ready to Read', 'Done'])
  })

  it('buckets chats by status and sorts newest first', () => {
    const chats = [
      createChat({ id: 'a', status: 'inProgress', updatedAt: '2026-07-16T10:00:00.000Z' }),
      createChat({ id: 'b', status: 'readyForReview', updatedAt: '2026-07-16T11:00:00.000Z' }),
      createChat({ id: 'c', status: 'read', updatedAt: '2026-07-16T09:00:00.000Z' }),
      createChat({ id: 'd', status: 'inProgress', updatedAt: '2026-07-16T12:00:00.000Z' })
    ]

    const columns = groupChatsByStatus(chats)

    expect(columns[0]?.chats.map((c) => c.id)).toEqual(['d', 'a'])
    expect(columns[1]?.chats.map((c) => c.id)).toEqual(['b'])
    expect(columns[2]?.chats.map((c) => c.id)).toEqual(['c'])
  })

  it('uses resolveStatus when provided', () => {
    const chats = [createChat({ id: 'sending', status: 'read' })]

    const columns = groupChatsByStatus(chats, {
      resolveStatus: (chat) => (chat.id === 'sending' ? 'inProgress' : chat.status)
    })

    expect(columns[0]?.chats.map((c) => c.id)).toEqual(['sending'])
    expect(columns[2]?.chats).toEqual([])
  })
})
