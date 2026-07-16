import { describe, expect, it } from 'vitest'
import { QueryClient } from '@tanstack/react-query'

import { mergeOrchestrationChildren } from './orchestration-cache'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

function createParentChat(): ChatThread {
  return {
    id: 'parent-id',
    title: 'Orchestration',
    preview: 'Plans ready',
    updatedAt: '2026-07-04T12:00:00.000Z',
    agentId: 'cursor',
    projectId: 'project-1',
    workspaceId: 'workspace-1',
    workspacePath: '/workspace',
    mode: 'orchestration',
    modelId: null,
    parentChatId: null,
    planFilePath: null,
    messages: []
  }
}

describe('mergeOrchestrationChildren', () => {
  it('adds orchestration children to the chat list only', () => {
    const queryClient = new QueryClient()
    const parentChat = createParentChat()

    const newIds = mergeOrchestrationChildren(
      parentChat,
      [
        {
          planId: 'auth-refactor',
          chatId: 'impl-child-id',
          mode: 'implementation',
          planFilePath: '.orchi/plan-auth-refactor.md'
        },
        {
          planId: 'auth-refactor',
          chatId: 'review-child-id',
          mode: 'review',
          planFilePath: '.orchi/review-auth-refactor.md'
        }
      ],
      queryClient
    )

    expect(newIds).toEqual(['impl-child-id', 'review-child-id'])

    const list = queryClient.getQueryData<ChatThread[]>(chatKeys.lists())
    expect(list).toHaveLength(2)
    expect(list?.map((chat) => chat.id).sort()).toEqual(['impl-child-id', 'review-child-id'].sort())

    expect(queryClient.getQueryData(chatKeys.detail('review-child-id'))).toBeUndefined()
    expect(list?.find((chat) => chat.id === 'review-child-id')).toMatchObject({
      id: 'review-child-id',
      mode: 'review',
      parentChatId: 'parent-id',
      planFilePath: '.orchi/review-auth-refactor.md',
      title: 'Auth refactor review'
    })
  })

  it('skips children already present in the chat list', () => {
    const queryClient = new QueryClient()
    const parentChat = createParentChat()

    queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), [
      {
        ...createParentChat(),
        id: 'review-child-id',
        parentChatId: 'parent-id',
        mode: 'review',
        planFilePath: '.orchi/review-auth-refactor.md'
      }
    ])

    const newIds = mergeOrchestrationChildren(
      parentChat,
      [
        {
          planId: 'auth-refactor',
          chatId: 'review-child-id',
          mode: 'review',
          planFilePath: '.orchi/review-auth-refactor.md'
        }
      ],
      queryClient
    )

    expect(newIds).toEqual([])
    expect(queryClient.getQueryData<ChatThread[]>(chatKeys.lists())).toHaveLength(1)
  })
})
