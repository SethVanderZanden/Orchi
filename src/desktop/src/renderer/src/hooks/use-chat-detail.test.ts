import { describe, expect, it } from 'vitest'

import { getChatDetailQueryOptions } from '@/hooks/use-chat-detail'
import { chatKeys } from '@/lib/query-keys'

describe('getChatDetailQueryOptions', () => {
  it('always refetches on mount and does not use list summary as placeholder', () => {
    const options = getChatDetailQueryOptions('chat-1')

    expect(options.queryKey).toEqual(chatKeys.detail('chat-1'))
    expect(options.staleTime).toBe(0)
    expect(options.refetchOnMount).toBe('always')
    expect(typeof options.placeholderData).toBe('function')
    if (typeof options.placeholderData === 'function') {
      expect(options.placeholderData(undefined, undefined as never)).toBeUndefined()
    }

    const cachedDetail = {
      id: 'chat-1',
      title: 'Cached',
      preview: 'Preview',
      updatedAt: '2026-07-05T12:00:00.000Z',
      agentId: 'cursor',
      projectId: null,
      workspaceId: null,
      workspacePath: '',
      mode: 'default' as const,
      modelId: null,
      contextSizeId: null,
      reasoningEffortId: null,
      approvalPolicyId: null,
      parentChatId: null,
      planFilePath: null,
      status: 'read' as const,
      lastReadAt: null,
      messages: []
    }

    if (typeof options.placeholderData === 'function') {
      expect(options.placeholderData(cachedDetail, undefined as never)).toBe(cachedDetail)
    }
  })

  it('disables query for local draft chats', () => {
    expect(getChatDetailQueryOptions('local:draft-1').enabled).toBe(false)
    expect(getChatDetailQueryOptions('550e8400-e29b-41d4-a716-446655440000').enabled).toBe(true)
  })
})
