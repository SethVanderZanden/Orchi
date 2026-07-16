import { renderHook, waitFor } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'

import { useChatReadState } from '@/hooks/chat/use-chat-read-state'
import type { ChatThread } from '@/lib/chat/types'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
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
    messages: [],
    ...overrides
  }
}

describe('useChatReadState', () => {
  it('calls loadChat for default-mode chat with empty messages on selection', async () => {
    const loadChat = vi.fn().mockResolvedValue(undefined)
    const chat = createChat()

    renderHook(() =>
      useChatReadState({
        activeChatId: 'chat-1',
        getChat: () => chat,
        getChildChats: () => [],
        loadChat,
        isChatSending: () => false,
        isParentKickingOffAny: () => false
      })
    )

    await waitFor(() => {
      expect(loadChat).toHaveBeenCalledWith('chat-1')
    })
  })

  it('does not call loadChat when active chat already has messages', async () => {
    const loadChat = vi.fn().mockResolvedValue(undefined)
    const chat = createChat({
      messages: [
        {
          id: 'msg-1',
          role: 'user',
          content: 'Hello',
          createdAt: '2026-07-05T12:00:00.000Z',
          status: 'complete'
        }
      ]
    })

    renderHook(() =>
      useChatReadState({
        activeChatId: 'chat-1',
        getChat: () => chat,
        getChildChats: () => [],
        loadChat,
        isChatSending: () => false,
        isParentKickingOffAny: () => false
      })
    )

    await waitFor(() => {
      expect(loadChat).not.toHaveBeenCalled()
    })
  })

  it('loads orchestration children with empty messages', async () => {
    const loadChat = vi.fn().mockResolvedValue(undefined)
    const parent = createChat({ id: 'parent-1', mode: 'orchestration' })
    const child = createChat({ id: 'child-1', mode: 'implementation', parentChatId: 'parent-1' })

    renderHook(() =>
      useChatReadState({
        activeChatId: 'parent-1',
        getChat: (chatId) => {
          if (chatId === 'parent-1') {
            return parent
          }

          if (chatId === 'child-1') {
            return child
          }

          return undefined
        },
        getChildChats: () => [child],
        loadChat,
        isChatSending: () => false,
        isParentKickingOffAny: () => false
      })
    )

    await waitFor(() => {
      expect(loadChat).toHaveBeenCalledWith('parent-1')
      expect(loadChat).toHaveBeenCalledWith('child-1')
    })
  })
})
