import { describe, expect, it } from 'vitest'

import { getPlanReviewVisibility } from '@/lib/orchestration/plan-review-visibility'
import type { ChatThread } from '@/lib/chat/types'

function createReviewChild(messages: ChatThread['messages'] = []): ChatThread {
  return {
    id: 'review-child-id',
    title: 'Auth refactor review',
    preview: 'Review',
    updatedAt: '2026-07-04T12:00:00.000Z',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
    workspacePath: '/workspace',
    mode: 'review',
    modelId: null,
    parentChatId: 'parent-id',
    planFilePath: '.orchi/review-auth-refactor.md',
    status: 'read',
    lastReadAt: null,
    messages
  }
}

describe('getPlanReviewVisibility', () => {
  it('shows reviewing while the review child is streaming', () => {
    const reviewChild = createReviewChild([
      {
        id: 'm-1',
        role: 'assistant',
        content: 'Reviewing changes',
        createdAt: '2026-07-04T12:00:00.000Z',
        status: 'streaming'
      }
    ])

    expect(getPlanReviewVisibility(reviewChild, false)).toEqual({
      reviewing: true,
      reviewStarted: false
    })
  })

  it('shows review started when a review child exists without parsed review output', () => {
    expect(getPlanReviewVisibility(createReviewChild(), false)).toEqual({
      reviewing: false,
      reviewStarted: true
    })
  })

  it('shows neither state when review output is ready', () => {
    expect(getPlanReviewVisibility(createReviewChild(), true)).toEqual({
      reviewing: false,
      reviewStarted: false
    })
  })
})
