import { beforeEach, describe, expect, it } from 'vitest'

import {
  DEFAULT_CHAT_MODE,
  getDefaultChatMode,
  getDefaultChatModeLabel,
  isDefaultChatMode,
  setDefaultChatMode
} from './default-chat-mode'

describe('default chat mode', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('defaults to orchestration', () => {
    expect(getDefaultChatMode()).toBe(DEFAULT_CHAT_MODE)
    expect(DEFAULT_CHAT_MODE).toBe('orchestration')
  })

  it('persists and reads the preference', () => {
    setDefaultChatMode('default')
    expect(getDefaultChatMode()).toBe('default')
  })

  it('ignores unknown mode ids', () => {
    expect(isDefaultChatMode('orchestration')).toBe(true)
    expect(isDefaultChatMode('default')).toBe(true)
    expect(isDefaultChatMode('review')).toBe(true)
    expect(isDefaultChatMode('implementation')).toBe(false)
    expect(isDefaultChatMode('vim')).toBe(false)

    localStorage.setItem('orchi.defaultChatMode', 'implementation')
    expect(getDefaultChatMode()).toBe('orchestration')
  })

  it('builds labels', () => {
    expect(getDefaultChatModeLabel('orchestration')).toBe('Orchestration')
    expect(getDefaultChatModeLabel('default')).toBe('Default')
  })
})
