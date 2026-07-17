import { describe, expect, it } from 'vitest'

import type { ChatThread } from '@/lib/chat/types'
import {
  filterBoardChats,
  getDateCutoff,
  matchesBoardDateRange,
  matchesBoardProjectFilter
} from '@/lib/kanban/board-filters'

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

const now = new Date('2026-07-17T12:00:00.000Z')

describe('getDateCutoff', () => {
  it('returns null for all', () => {
    expect(getDateCutoff('all', now)).toBeNull()
  })

  it('returns expected cutoffs', () => {
    expect(getDateCutoff('lastHour', now)?.toISOString()).toBe('2026-07-17T11:00:00.000Z')
    expect(getDateCutoff('last9Hours', now)?.toISOString()).toBe('2026-07-17T03:00:00.000Z')
    expect(getDateCutoff('last24Hours', now)?.toISOString()).toBe('2026-07-16T12:00:00.000Z')
    expect(getDateCutoff('last3Days', now)?.toISOString()).toBe('2026-07-14T12:00:00.000Z')
    expect(getDateCutoff('last7Days', now)?.toISOString()).toBe('2026-07-10T12:00:00.000Z')
  })
})

describe('matchesBoardProjectFilter', () => {
  it('matches all projects', () => {
    expect(matchesBoardProjectFilter(createChat({ projectId: 'p1' }), 'all')).toBe(true)
    expect(matchesBoardProjectFilter(createChat({ projectId: null }), 'all')).toBe(true)
  })

  it('matches a specific project', () => {
    expect(matchesBoardProjectFilter(createChat({ projectId: 'p1' }), 'p1')).toBe(true)
    expect(matchesBoardProjectFilter(createChat({ projectId: 'p2' }), 'p1')).toBe(false)
  })
})

describe('matchesBoardDateRange', () => {
  it('includes every chat when range is all', () => {
    expect(
      matchesBoardDateRange(createChat({ updatedAt: '2020-01-01T00:00:00.000Z' }), 'all', now)
    ).toBe(true)
  })

  it('filters by updatedAt for bounded ranges', () => {
    expect(
      matchesBoardDateRange(createChat({ updatedAt: '2026-07-17T11:30:00.000Z' }), 'lastHour', now)
    ).toBe(true)
    expect(
      matchesBoardDateRange(createChat({ updatedAt: '2026-07-17T10:59:59.999Z' }), 'lastHour', now)
    ).toBe(false)
  })
})

describe('filterBoardChats', () => {
  it('applies project and date filters together', () => {
    const chats = [
      createChat({ id: 'recent-p1', projectId: 'p1', updatedAt: '2026-07-17T11:00:00.000Z' }),
      createChat({ id: 'old-p1', projectId: 'p1', updatedAt: '2026-07-10T11:00:00.000Z' }),
      createChat({ id: 'recent-p2', projectId: 'p2', updatedAt: '2026-07-17T11:00:00.000Z' })
    ]

    const filtered = filterBoardChats(chats, { projectFilter: 'p1', dateRange: 'last24Hours' }, now)

    expect(filtered.map((chat) => chat.id)).toEqual(['recent-p1'])
  })
})
