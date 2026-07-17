import { afterEach, describe, expect, it, vi } from 'vitest'

import type { ChatMessage } from '@/lib/chat/types'

import { createMessageStreamHandlers } from './message-stream-handlers'

function makeMessage(content = ''): ChatMessage {
  return {
    id: 'assistant-1',
    role: 'assistant',
    content,
    createdAt: '2026-01-01T00:00:00.000Z',
    status: 'processing'
  }
}

describe('createMessageStreamHandlers', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
    vi.restoreAllMocks()
  })

  it('batches tokens into a single update per animation frame', () => {
    const rafCallbacks: FrameRequestCallback[] = []
    vi.stubGlobal(
      'requestAnimationFrame',
      vi.fn((callback: FrameRequestCallback) => {
        rafCallbacks.push(callback)
        return rafCallbacks.length
      })
    )
    vi.stubGlobal('cancelAnimationFrame', vi.fn())

    const updateAssistantMessage = vi.fn(
      (_chatId: string, _messageId: string, updater: (message: ChatMessage) => ChatMessage) => {
        updater(makeMessage(''))
      }
    )

    const handlers = createMessageStreamHandlers({
      isActiveTurn: () => true,
      assistantMessageId: 'assistant-1',
      chatId: 'chat-1',
      updateAssistantMessage,
      appendMarker: vi.fn(),
      clearMarkers: vi.fn(),
      notifyAgentActivity: vi.fn()
    })

    handlers.onToken?.('Hel')
    handlers.onToken?.('lo')
    expect(updateAssistantMessage).not.toHaveBeenCalled()

    rafCallbacks[0](0)

    expect(updateAssistantMessage).toHaveBeenCalledTimes(1)
    const updater = updateAssistantMessage.mock.calls[0][2] as (message: ChatMessage) => ChatMessage
    expect(updater(makeMessage('')).content).toBe('Hello')
  })

  it('flushes pending tokens before onDone', () => {
    vi.stubGlobal(
      'requestAnimationFrame',
      vi.fn((callback: FrameRequestCallback) => {
        void callback
        return 1
      })
    )
    vi.stubGlobal('cancelAnimationFrame', vi.fn())

    const updateAssistantMessage = vi.fn(
      (_chatId: string, _messageId: string, updater: (message: ChatMessage) => ChatMessage) => {
        updater(makeMessage('Hi'))
      }
    )

    const handlers = createMessageStreamHandlers({
      isActiveTurn: () => true,
      assistantMessageId: 'assistant-1',
      chatId: 'chat-1',
      updateAssistantMessage,
      appendMarker: vi.fn(),
      clearMarkers: vi.fn(),
      notifyAgentActivity: vi.fn()
    })

    handlers.onToken?.(' there')
    handlers.onDone?.('server-id')

    expect(updateAssistantMessage).toHaveBeenCalledTimes(2)
    const tokenUpdater = updateAssistantMessage.mock.calls[0][2] as (
      message: ChatMessage
    ) => ChatMessage
    expect(tokenUpdater(makeMessage('Hi')).content).toBe('Hi there')
    expect(updateAssistantMessage.mock.calls[1][2](makeMessage('Hi there')).status).toBe('complete')
  })
})
