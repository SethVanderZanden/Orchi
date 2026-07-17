import { describe, expect, it } from 'vitest'
import { QueryClient } from '@tanstack/react-query'

import { appendUserAndAssistantMessages } from '@/lib/chat/message-updates'
import { resolveDetailCache } from '@/lib/chat/resolve-detail-cache'
import type { ChatThread } from '@/lib/chat/types'
import { chatKeys } from '@/lib/query-keys'

function createSummary(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Test chat',
    preview: 'Preview',
    updatedAt: '2026-07-05T12:00:00.000Z',
    agentId: 'cursor',
    projectId: 'project-1',
    workspaceId: 'workspace-1',
    workspacePath: '/workspace',
    mode: 'default',
    modelId: null,
    parentChatId: null,
    planFilePath: null,
    status: 'read',
    lastReadAt: null,
    messages: [],
    ...overrides
  }
}

describe('resolveDetailCache', () => {
  it('returns list summary when detail cache is empty', () => {
    const queryClient = new QueryClient()
    const summary = createSummary()
    queryClient.setQueryData(chatKeys.lists(), [summary])

    const getChat = (chatId: string): ChatThread | undefined =>
      queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((chat) => chat.id === chatId)

    expect(resolveDetailCache(queryClient, 'chat-1', getChat)).toEqual(summary)
  })

  it('prefers detail cache over list summary', () => {
    const queryClient = new QueryClient()
    const summary = createSummary()
    const detail = createSummary({
      messages: [
        {
          id: 'msg-1',
          role: 'user',
          content: 'Existing message',
          createdAt: '2026-07-05T12:00:00.000Z',
          status: 'complete'
        }
      ]
    })

    queryClient.setQueryData(chatKeys.lists(), [summary])
    queryClient.setQueryData(chatKeys.detail('chat-1'), detail)

    const getChat = (chatId: string): ChatThread | undefined =>
      queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((chat) => chat.id === chatId)

    expect(resolveDetailCache(queryClient, 'chat-1', getChat)?.messages).toHaveLength(1)
  })
})

describe('send cache upsert', () => {
  it('appends user message when detail cache is empty but list summary exists', () => {
    const queryClient = new QueryClient()
    const summary = createSummary()
    queryClient.setQueryData(chatKeys.lists(), [summary])

    const getChat = (chatId: string): ChatThread | undefined =>
      queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((chat) => chat.id === chatId)

    const assistantMessageId = 'assistant-1'
    const content = 'Hello Orchi'

    queryClient.setQueryData<ChatThread>(chatKeys.detail('chat-1'), (current) => {
      const base = current ?? resolveDetailCache(queryClient, 'chat-1', getChat)
      if (!base) {
        return current
      }

      return appendUserAndAssistantMessages(base, content, assistantMessageId)
    })

    const detail = queryClient.getQueryData<ChatThread>(chatKeys.detail('chat-1'))
    expect(detail?.messages).toHaveLength(2)
    expect(detail?.messages[0]).toMatchObject({ role: 'user', content })
    expect(detail?.messages[1]).toMatchObject({
      id: assistantMessageId,
      role: 'assistant',
      status: 'processing'
    })
  })
})
