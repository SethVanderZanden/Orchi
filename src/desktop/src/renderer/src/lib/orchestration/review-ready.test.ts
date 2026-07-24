import { describe, expect, it } from 'vitest'

import {
  buildReviewPlansByPlanId,
  hasReviewReadyPlan,
  listReviewChildIdsNeedingReload
} from './review-ready'
import type { ChatThread } from '@/lib/chat/types'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'parent-chat',
    title: 'Parent chat',
    preview: 'Plan ready',
    updatedAt: '2026-07-04T12:00:00.000Z',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
    workspacePath: '/workspace',
    mode: 'orchestration',
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

describe('review-ready', () => {
  it('detects review-ready plans from review child messages', () => {
    const parent = createChat({
      messages: [
        {
          id: 'm-1',
          role: 'assistant',
          content: `
<!-- orchi-plan:auth-refactor -->
# Auth Refactor
Implement auth changes.
<!-- /orchi-plan -->
`,
          createdAt: '2026-07-04T12:00:00.000Z',
          status: 'complete'
        }
      ]
    })

    const reviewChild: ChatThread = {
      ...createChat({
        id: 'review-child',
        parentChatId: 'parent-chat',
        mode: 'review',
        planFilePath: 'plans/review-auth-refactor.md'
      }),
      messages: [
        {
          id: 'm-2',
          role: 'assistant',
          content: `
<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review
Review complete.
<!-- /orchi-review-plan -->
`,
          createdAt: '2026-07-04T12:05:00.000Z',
          status: 'complete'
        }
      ]
    }

    const reviewPlansByPlanId = buildReviewPlansByPlanId(parent, [reviewChild], (chatId) =>
      chatId === reviewChild.id ? reviewChild : undefined
    )

    expect(reviewPlansByPlanId['auth-refactor']).toMatchObject({
      planId: 'auth-refactor',
      title: 'Auth Refactor Review'
    })
    expect(
      hasReviewReadyPlan(parent, [reviewChild], (chatId) =>
        chatId === reviewChild.id ? reviewChild : undefined
      )
    ).toBe(true)
  })

  it('returns false for non-orchestration chats', () => {
    const chat = createChat({ mode: 'default' })

    expect(hasReviewReadyPlan(chat, [], () => undefined)).toBe(false)
  })

  it('lists review children that finished without parseable review output', () => {
    const parent = createChat({
      messages: [
        {
          id: 'm-1',
          role: 'assistant',
          content: `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
Implement auth changes.
<!-- /orchi-plan -->`,
          createdAt: '2026-07-04T12:00:00.000Z',
          status: 'complete'
        }
      ]
    })

    const reviewChild: ChatThread = {
      ...createChat({
        id: 'review-child',
        parentChatId: 'parent-chat',
        mode: 'review',
        planFilePath: '.orchi/review-auth-refactor.md'
      }),
      messages: [
        {
          id: 'm-user',
          role: 'user',
          content: 'Begin review.',
          createdAt: '2026-07-04T12:04:00.000Z',
          status: 'complete'
        }
      ]
    }

    expect(
      listReviewChildIdsNeedingReload(parent, [reviewChild], (chatId) =>
        chatId === reviewChild.id ? reviewChild : undefined
      )
    ).toEqual(['review-child'])
  })
})
