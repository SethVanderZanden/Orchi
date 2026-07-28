import { describe, expect, it } from 'vitest'

import { preferChatStatus } from './prefer-chat-status'

describe('preferChatStatus', () => {
  it('accepts the first status when current is missing', () => {
    expect(preferChatStatus(undefined, 'inProgress')).toBe('inProgress')
  })

  it('keeps readyForReview when incoming is a late inProgress', () => {
    expect(preferChatStatus('readyForReview', 'inProgress')).toBe('readyForReview')
  })

  it('allows inProgress after read so kickoff can leave Done', () => {
    expect(preferChatStatus('read', 'inProgress')).toBe('inProgress')
  })

  it('allows upgrades and same-value updates', () => {
    expect(preferChatStatus('inProgress', 'readyForReview')).toBe('readyForReview')
    expect(preferChatStatus('readyForReview', 'read')).toBe('read')
    expect(preferChatStatus('read', 'read')).toBe('read')
    expect(preferChatStatus('inProgress', 'read')).toBe('read')
  })
})
