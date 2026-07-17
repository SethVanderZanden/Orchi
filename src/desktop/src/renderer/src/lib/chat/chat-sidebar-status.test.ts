import { describe, expect, it } from 'vitest'

import { getChatSidebarStatus, mapChatStatusToSidebarVariant } from './chat-sidebar-status'
import type { ChatThread } from '@/lib/chat/types'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Test chat',
    preview: 'Hello',
    updatedAt: '2026-07-04T12:00:00.000Z',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
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

describe('mapChatStatusToSidebarVariant', () => {
  it('maps server statuses to sidebar variants', () => {
    expect(mapChatStatusToSidebarVariant('read')).toBe('standard')
    expect(mapChatStatusToSidebarVariant('inProgress')).toBe('active')
    expect(mapChatStatusToSidebarVariant('readyForReview')).toBe('attention')
  })
})

describe('getChatSidebarStatus', () => {
  it('returns active when the chat is sending', () => {
    const status = getChatSidebarStatus({
      chat: createChat({ status: 'read' }),
      isSending: true,
      isParentKickingOff: false
    })

    expect(status).toBe('active')
  })

  it('returns active when parent kickoff is in progress', () => {
    const status = getChatSidebarStatus({
      chat: createChat({ status: 'read' }),
      isSending: false,
      isParentKickingOff: true
    })

    expect(status).toBe('active')
  })

  it('uses server status when idle', () => {
    expect(
      getChatSidebarStatus({
        chat: createChat({ status: 'readyForReview' }),
        isSending: false,
        isParentKickingOff: false
      })
    ).toBe('attention')

    expect(
      getChatSidebarStatus({
        chat: createChat({ status: 'inProgress' }),
        isSending: false,
        isParentKickingOff: false
      })
    ).toBe('active')

    expect(
      getChatSidebarStatus({
        chat: createChat({ status: 'read' }),
        isSending: false,
        isParentKickingOff: false
      })
    ).toBe('standard')
  })

  it('prioritizes ephemeral sending over server readyForReview', () => {
    const status = getChatSidebarStatus({
      chat: createChat({ status: 'readyForReview' }),
      isSending: true,
      isParentKickingOff: false
    })

    expect(status).toBe('active')
  })
})
