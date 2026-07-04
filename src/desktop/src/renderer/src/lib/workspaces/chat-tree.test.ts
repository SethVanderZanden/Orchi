import { describe, expect, it } from 'vitest'

import type { ChatThread } from '@/lib/chat/types'

import {
  buildChatTree,
  findChildForPlan,
  findReviewChildForPlan,
  formatPlanIdAsTitle,
  filterChatTreeNodes,
  isImplementationChildChat,
  planIdFromPlanFilePath
} from './chat-tree'

function makeChat(overrides: Partial<ChatThread> & Pick<ChatThread, 'id'>): ChatThread {
  return {
    id: overrides.id,
    title: overrides.title ?? overrides.id,
    preview: overrides.preview ?? '',
    updatedAt: overrides.updatedAt ?? '2026-01-01T00:00:00.000Z',
    agentId: 'cursor',
    projectId: overrides.projectId ?? null,
    workspaceId: overrides.workspaceId ?? null,
    workspacePath: overrides.workspacePath ?? '/workspace',
    mode: 'default',
    parentChatId: overrides.parentChatId ?? null,
    planFilePath: overrides.planFilePath ?? null,
    messages: overrides.messages ?? []
  }
}

describe('planIdFromPlanFilePath', () => {
  it('extracts plan id from .orchi plan paths', () => {
    expect(planIdFromPlanFilePath('.orchi/plan-auth-refactor.md')).toBe('auth-refactor')
    expect(planIdFromPlanFilePath('.orchi\\plan-ui-polish.md')).toBe('ui-polish')
  })

  it('returns null for non-plan paths', () => {
    expect(planIdFromPlanFilePath(null)).toBeNull()
    expect(planIdFromPlanFilePath('README.md')).toBeNull()
  })
})

describe('formatPlanIdAsTitle', () => {
  it('formats kebab-case plan ids', () => {
    expect(formatPlanIdAsTitle('auth-refactor')).toBe('Auth refactor')
  })
})

describe('buildChatTree', () => {
  it('nests children under their parent and excludes them from roots', () => {
    const parent = makeChat({ id: 'parent', title: 'Orchestration', updatedAt: '2026-01-03T00:00:00.000Z' })
    const childA = makeChat({
      id: 'child-a',
      title: 'Auth refactor',
      parentChatId: 'parent',
      updatedAt: '2026-01-02T00:00:00.000Z'
    })
    const childB = makeChat({
      id: 'child-b',
      title: 'Token refresh',
      parentChatId: 'parent',
      updatedAt: '2026-01-01T00:00:00.000Z'
    })
    const root = makeChat({ id: 'root', title: 'New chat', updatedAt: '2026-01-04T00:00:00.000Z' })

    const tree = buildChatTree([parent, childA, childB, root])

    expect(tree.map((node) => node.chat.id)).toEqual(['root', 'parent'])
    expect(tree.find((node) => node.chat.id === 'parent')?.children.map((child) => child.id)).toEqual([
      'child-a',
      'child-b'
    ])
  })

  it('promotes orphan children to roots when parent is missing', () => {
    const orphan = makeChat({
      id: 'orphan',
      title: 'Orphan agent',
      parentChatId: 'missing-parent'
    })

    const tree = buildChatTree([orphan])

    expect(tree).toHaveLength(1)
    expect(tree[0]?.chat.id).toBe('orphan')
    expect(tree[0]?.children).toEqual([])
  })
})

describe('findChildForPlan', () => {
  it('matches child chats by plan file path', () => {
    const children = [
      makeChat({
        id: 'child-a',
        planFilePath: '.orchi/plan-auth-refactor.md'
      }),
      makeChat({
        id: 'child-b',
        planFilePath: '.orchi/plan-ui-polish.md'
      })
    ]

    expect(findChildForPlan('auth-refactor', children)?.id).toBe('child-a')
    expect(findChildForPlan('missing', children)).toBeUndefined()
  })
})

describe('findReviewChildForPlan', () => {
  it('matches review child chats by review file path', () => {
    const children = [
      makeChat({
        id: 'review-child',
        mode: 'review',
        planFilePath: '.orchi/review-auth-refactor.md'
      })
    ]

    expect(findReviewChildForPlan('auth-refactor', children)?.id).toBe('review-child')
  })
})

describe('isImplementationChildChat', () => {
  it('identifies default-mode plan children', () => {
    expect(
      isImplementationChildChat(
        makeChat({
          id: 'child',
          parentChatId: 'parent',
          planFilePath: '.orchi/plan-auth-refactor.md'
        })
      )
    ).toBe(true)

    expect(
      isImplementationChildChat(
        makeChat({
          id: 'child',
          mode: 'implementation',
          parentChatId: 'parent',
          planFilePath: '.orchi/plan-auth-refactor.md'
        })
      )
    ).toBe(true)

    expect(
      isImplementationChildChat(
        makeChat({
          id: 'review',
          mode: 'review',
          parentChatId: 'parent',
          planFilePath: '.orchi/review-auth-refactor.md'
        })
      )
    ).toBe(false)
  })
})

describe('filterChatTreeNodes', () => {
  it('includes parent when a child matches search', () => {
    const nodes = buildChatTree([
      makeChat({ id: 'parent', title: 'Orchestration' }),
      makeChat({
        id: 'child',
        title: 'Auth refactor',
        preview: 'Implement auth',
        parentChatId: 'parent'
      })
    ])

    const filtered = filterChatTreeNodes(nodes, 'auth')

    expect(filtered).toHaveLength(1)
    expect(filtered[0]?.chat.id).toBe('parent')
    expect(filtered[0]?.children.map((child) => child.id)).toEqual(['child'])
  })
})
