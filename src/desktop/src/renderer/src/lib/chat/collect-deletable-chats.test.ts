import { describe, expect, it } from 'vitest'

import { collectDeletableChats } from '@/lib/chat/collect-deletable-chats'
import type { ChatThread } from '@/lib/chat/types'

function chat(id: string): ChatThread {
  return {
    id,
    title: `Chat ${id}`,
    preview: '',
    status: 'read',
    mode: 'default',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
    workspacePath: '',
    modelId: null,
    contextSizeId: null,
    reasoningEffortId: null,
    approvalPolicyId: null,
    parentChatId: null,
    planFilePath: null,
    lastReadAt: null,
    messages: [],
    updatedAt: '2026-01-01T00:00:00.000Z'
  }
}

describe('collectDeletableChats', () => {
  it('returns all chats when none are busy', () => {
    const chats = [chat('a'), chat('b')]

    expect(
      collectDeletableChats({
        chats,
        isChatSending: () => false,
        isParentKickingOffAny: () => false
      })
    ).toEqual({
      deletable: chats,
      skippedSendingCount: 0
    })
  })

  it('skips chats that are sending or kicking off', () => {
    const chats = [chat('a'), chat('b'), chat('c')]

    expect(
      collectDeletableChats({
        chats,
        isChatSending: (id) => id === 'a',
        isParentKickingOffAny: (id) => id === 'c'
      })
    ).toEqual({
      deletable: [chat('b')],
      skippedSendingCount: 2
    })
  })
})
