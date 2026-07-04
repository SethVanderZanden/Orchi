import { beforeEach, describe, expect, it, vi } from 'vitest'

import { getLastReadUpdatedAt, isChatUnread, markChatRead } from './chat-read-state'

describe('chat-read-state', () => {
  const storage = new Map<string, string>()

  beforeEach(() => {
    storage.clear()
    vi.stubGlobal('localStorage', {
      getItem: (key: string) => storage.get(key) ?? null,
      setItem: (key: string, value: string) => {
        storage.set(key, value)
      },
      removeItem: (key: string) => {
        storage.delete(key)
      },
      clear: () => {
        storage.clear()
      }
    })
  })

  it('marks a chat as read with the latest updatedAt', () => {
    markChatRead('chat-1', '2026-07-04T12:00:00.000Z')

    expect(getLastReadUpdatedAt('chat-1')).toBe('2026-07-04T12:00:00.000Z')
  })

  it('treats chats as unread when updatedAt is newer than last read', () => {
    markChatRead('chat-1', '2026-07-04T12:00:00.000Z')

    expect(
      isChatUnread(
        { id: 'chat-1', updatedAt: '2026-07-04T13:00:00.000Z' },
        undefined
      )
    ).toBe(true)
  })

  it('does not mark the active chat as unread', () => {
    markChatRead('chat-1', '2026-07-04T12:00:00.000Z')

    expect(
      isChatUnread(
        { id: 'chat-1', updatedAt: '2026-07-04T13:00:00.000Z' },
        'chat-1'
      )
    ).toBe(false)
  })

  it('does not mark chats without a read timestamp as unread', () => {
    expect(
      isChatUnread(
        { id: 'chat-1', updatedAt: '2026-07-04T13:00:00.000Z' },
        undefined
      )
    ).toBe(false)
  })
})
