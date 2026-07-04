import { describe, expect, it } from 'vitest'

import { isLocalChat, isPersistedChat } from './chat-persistence'

describe('isLocalChat', () => {
  it('returns true for sentinel id 0', () => {
    expect(isLocalChat('0')).toBe(true)
  })

  it('returns true for local: prefixed ids', () => {
    expect(isLocalChat('local:draft-1')).toBe(true)
    expect(isLocalChat('local:')).toBe(true)
  })

  it('returns false for persisted GUID ids', () => {
    expect(isLocalChat('a1b2c3d4-e5f6-7890-abcd-ef1234567890')).toBe(false)
    expect(isLocalChat('optimistic-child')).toBe(false)
  })
})

describe('isPersistedChat', () => {
  it('returns false for local chat ids', () => {
    expect(isPersistedChat('0')).toBe(false)
    expect(isPersistedChat('local:draft-1')).toBe(false)
  })

  it('returns true for server-backed chat ids', () => {
    expect(isPersistedChat('a1b2c3d4-e5f6-7890-abcd-ef1234567890')).toBe(true)
    expect(isPersistedChat('optimistic-child')).toBe(true)
  })
})
