import { describe, expect, it } from 'vitest'

import { mergeChatDetail } from '@/lib/chat/merge-chat-detail'
import type { ChatMessage, ChatThread } from '@/lib/chat/types'

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

function createMessage(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: 'msg-1',
    role: 'user',
    content: 'Hello',
    createdAt: '2026-07-05T12:00:00.000Z',
    status: 'complete',
    ...overrides
  }
}

describe('mergeChatDetail', () => {
  it('returns incoming when existing is undefined', () => {
    const incoming = createSummary({
      messages: [createMessage()]
    })

    expect(mergeChatDetail(undefined, incoming)).toEqual(incoming)
  })

  it('preserves streaming tokens when stale refetch has fewer messages', () => {
    const existing = createSummary({
      messages: [
        createMessage({ id: 'user-1', role: 'user', content: 'Hi' }),
        createMessage({
          id: 'assistant-1',
          role: 'assistant',
          content: 'Partial reply',
          status: 'streaming'
        })
      ]
    })
    const incoming = createSummary({
      messages: [createMessage({ id: 'user-1', role: 'user', content: 'Hi' })]
    })

    const merged = mergeChatDetail(existing, incoming)

    expect(merged.messages).toHaveLength(2)
    expect(merged.messages[1]).toMatchObject({
      id: 'assistant-1',
      content: 'Partial reply',
      status: 'streaming'
    })
  })

  it('accepts server reconcile after stream completes', () => {
    const existing = createSummary({
      messages: [
        createMessage({ id: 'user-1', role: 'user', content: 'Hi' }),
        createMessage({
          id: 'client-assistant',
          role: 'assistant',
          content: 'Full reply',
          status: 'complete'
        })
      ]
    })
    const incoming = createSummary({
      messages: [
        createMessage({ id: 'user-1', role: 'user', content: 'Hi' }),
        createMessage({
          id: 'server-assistant',
          role: 'assistant',
          content: 'Full reply',
          status: 'complete'
        })
      ]
    })

    const merged = mergeChatDetail(existing, incoming)

    expect(merged.messages).toHaveLength(2)
    expect(merged.messages[1]?.id).toBe('server-assistant')
  })

  it('merges metadata from incoming while preserving client-ahead messages', () => {
    const existing = createSummary({
      title: 'Old title',
      messages: [
        createMessage({ id: 'user-1' }),
        createMessage({ id: 'assistant-1', role: 'assistant', status: 'processing', content: '' })
      ]
    })
    const incoming = createSummary({
      title: 'New title',
      updatedAt: '2026-07-05T13:00:00.000Z',
      messages: []
    })

    const merged = mergeChatDetail(existing, incoming)

    expect(merged.title).toBe('New title')
    expect(merged.messages).toHaveLength(2)
  })
})
