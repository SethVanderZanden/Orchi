import { describe, expect, it } from 'vitest'
import { QueryClient } from '@tanstack/react-query'

import { createOrchestrationEventHandlers, mergeOrchestrationChildren } from './orchestration-cache'
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
    contextSizeId: null,
    reasoningEffortId: null,
    approvalPolicyId: null,
    parentChatId: null,
    planFilePath: null,
    status: 'read',
    lastReadAt: null,
    messages: []
  }
}

function createChildChat(): ChatThread {
  return {
    id: 'child-id',
    title: 'Review',
    preview: 'Working',
    updatedAt: '2026-07-04T12:00:00.000Z',
    agentId: 'cursor',
    projectId: 'project-1',
    workspaceId: 'workspace-1',
    workspacePath: '/workspace',
    mode: 'review',
    modelId: null,
    contextSizeId: null,
    reasoningEffortId: null,
    approvalPolicyId: null,
    parentChatId: 'parent-id',
    planFilePath: '.orchi/review-auth-refactor.md',
    status: 'inProgress',
    lastReadAt: null,
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

describe('createOrchestrationEventHandlers', () => {
  it('marks streaming assistant complete when agent_done messageId does not match local id', () => {
    const queryClient = new QueryClient()
    const parentChat = createParentChat()
    const childChat = createChildChat()

    queryClient.setQueryData(chatKeys.detail(childChat.id), {
      ...childChat,
      messages: [
        {
          id: 'local-streaming-id',
          role: 'assistant',
          content: '# Review plan\n\n| A | B |\n|---|---|\n| 1 | 2 |',
          createdAt: '2026-07-04T12:00:00.000Z',
          status: 'streaming'
        }
      ]
    })

    const handlers = createOrchestrationEventHandlers(parentChat, queryClient, () => childChat)
    handlers.onAgentDone?.({
      childChatId: childChat.id,
      messageId: 'server-message-id',
      succeeded: true
    })

    const detail = queryClient.getQueryData<ChatThread>(chatKeys.detail(childChat.id))
    expect(detail?.messages[0]).toMatchObject({
      id: 'server-message-id',
      status: 'complete',
      content: expect.stringContaining('# Review plan')
    })
  })
})
