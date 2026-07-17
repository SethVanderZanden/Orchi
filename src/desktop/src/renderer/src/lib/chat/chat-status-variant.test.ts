import { describe, expect, it } from 'vitest'

import { getChatStatusVariant, mapChatStatusToVariant } from './chat-status-variant'
import type { ChatThread } from '@/lib/chat/types'

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

describe('mapChatStatusToVariant', () => {
  it('maps statuses', () => {
    expect(mapChatStatusToVariant('inProgress')).toBe('active')
    expect(mapChatStatusToVariant('readyForReview')).toBe('attention')
    expect(mapChatStatusToVariant('read')).toBe('standard')
  })
})

describe('getChatStatusVariant', () => {
  it('returns active while sending', () => {
    const status = getChatStatusVariant({
      chat: createChat({ status: 'read' }),
      isSending: true,
      isParentKickingOff: false
    })
    expect(status).toBe('active')
  })

  it('returns active while parent is kicking off', () => {
    const status = getChatStatusVariant({
      chat: createChat({ status: 'read' }),
      isSending: false,
      isParentKickingOff: true
    })
    expect(status).toBe('active')
  })

  it('maps chat status when idle', () => {
    expect(
      getChatStatusVariant({
        chat: createChat({ status: 'readyForReview' }),
        isSending: false,
        isParentKickingOff: false
      })
    ).toBe('attention')

    expect(
      getChatStatusVariant({
        chat: createChat({ status: 'inProgress' }),
        isSending: false,
        isParentKickingOff: false
      })
    ).toBe('active')

    expect(
      getChatStatusVariant({
        chat: createChat({ status: 'read' }),
        isSending: false,
        isParentKickingOff: false
      })
    ).toBe('standard')
  })

  it('prefers sending over readyForReview', () => {
    const status = getChatStatusVariant({
      chat: createChat({ status: 'readyForReview' }),
      isSending: true,
      isParentKickingOff: false
    })
    expect(status).toBe('active')
  })

  it('shows standard while viewing readyForReview', () => {
    expect(
      getChatStatusVariant({
        chat: createChat({ status: 'readyForReview' }),
        isSending: false,
        isParentKickingOff: false,
        isViewing: true
      })
    ).toBe('standard')
  })

  it('keeps amber while viewing if still inProgress', () => {
    expect(
      getChatStatusVariant({
        chat: createChat({ status: 'inProgress' }),
        isSending: false,
        isParentKickingOff: false,
        isViewing: true
      })
    ).toBe('active')
  })

  it('keeps amber while viewing if still sending', () => {
    expect(
      getChatStatusVariant({
        chat: createChat({ status: 'inProgress' }),
        isSending: true,
        isParentKickingOff: false,
        isViewing: true
      })
    ).toBe('active')
  })
})
