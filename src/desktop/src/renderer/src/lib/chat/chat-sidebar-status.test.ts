import { describe, expect, it } from 'vitest'

import { getChatSidebarStatus } from './chat-sidebar-status'
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
    messages: [],
    ...overrides
  }
}

describe('getChatSidebarStatus', () => {
  it('returns active when the chat is sending', () => {
    const status = getChatSidebarStatus({
      chat: createChat(),
      isSending: true,
      isParentKickingOff: false,
      getChat: () => undefined,
      getChildChats: () => []
    })

    expect(status).toBe('active')
  })

  it('returns active when cached messages are streaming', () => {
    const status = getChatSidebarStatus({
      chat: createChat(),
      isSending: false,
      isParentKickingOff: false,
      getChat: () =>
        createChat({
          messages: [
            {
              id: 'm-1',
              role: 'assistant',
              content: 'Working',
              createdAt: '2026-07-04T12:00:00.000Z',
              status: 'streaming'
            }
          ]
        }),
      getChildChats: () => []
    })

    expect(status).toBe('active')
  })

  it('returns attention when the last message failed', () => {
    const status = getChatSidebarStatus({
      chat: createChat(),
      isSending: false,
      isParentKickingOff: false,
      getChat: () =>
        createChat({
          messages: [
            {
              id: 'm-1',
              role: 'assistant',
              content: 'Failed',
              createdAt: '2026-07-04T12:00:00.000Z',
              status: 'error'
            }
          ]
        }),
      getChildChats: () => []
    })

    expect(status).toBe('attention')
  })

  it('prioritizes active over attention', () => {
    const status = getChatSidebarStatus({
      chat: createChat({ updatedAt: '2026-07-04T13:00:00.000Z' }),
      activeChatId: 'other-chat',
      isSending: true,
      isParentKickingOff: false,
      getChat: () =>
        createChat({
          messages: [
            {
              id: 'm-1',
              role: 'assistant',
              content: 'Failed',
              createdAt: '2026-07-04T12:00:00.000Z',
              status: 'error'
            }
          ]
        }),
      getChildChats: () => []
    })

    expect(status).toBe('active')
  })

  it('returns standard for idle chats', () => {
    const status = getChatSidebarStatus({
      chat: createChat(),
      isSending: false,
      isParentKickingOff: false,
      getChat: () => createChat(),
      getChildChats: () => []
    })

    expect(status).toBe('standard')
  })
})
