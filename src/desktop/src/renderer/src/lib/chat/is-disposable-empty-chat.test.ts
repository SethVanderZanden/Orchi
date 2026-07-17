import { describe, expect, it } from 'vitest'

import type { ChatThread } from '@/lib/chat/types'
import { setComposerDraft, takeComposerDraft } from '@/lib/chat/composer-drafts'
import { isDisposableEmptyChat } from '@/lib/chat/is-disposable-empty-chat'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Chat',
    preview: '',
    updatedAt: new Date().toISOString(),
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

describe('isDisposableEmptyChat', () => {
  it('returns true for chats with no messages and no draft', () => {
    expect(isDisposableEmptyChat(createChat(), 'chat-1')).toBe(true)
  })

  it('returns false when the chat has messages', () => {
    expect(
      isDisposableEmptyChat(
        createChat({
          messages: [
            {
              id: 'm1',
              role: 'user',
              content: 'hello',
              createdAt: new Date().toISOString(),
              status: 'complete'
            }
          ]
        }),
        'chat-1'
      )
    ).toBe(false)
  })

  it('returns false when a composer draft exists', () => {
    setComposerDraft('chat-1', 'draft text')
    expect(isDisposableEmptyChat(createChat(), 'chat-1')).toBe(false)
    takeComposerDraft('chat-1')
  })

  it('returns false when the chat is missing', () => {
    expect(isDisposableEmptyChat(undefined, 'chat-1')).toBe(false)
  })
})
