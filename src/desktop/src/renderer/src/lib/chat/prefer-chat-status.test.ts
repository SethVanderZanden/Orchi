import { describe, expect, it } from 'vitest'

import { preferChatStatus, mergeChatStatus } from './prefer-chat-status'

describe('preferChatStatus', () => {
  it('accepts the first status when current is missing', () => {
    expect(preferChatStatus(undefined, 'inProgress')).toBe('inProgress')
  })

  it('keeps readyForReview / read when incoming is a late inProgress', () => {
    expect(preferChatStatus('readyForReview', 'inProgress')).toBe('readyForReview')
    expect(preferChatStatus('read', 'inProgress')).toBe('read')
  })

  it('allows upgrades and same-value updates', () => {
    expect(preferChatStatus('inProgress', 'readyForReview')).toBe('readyForReview')
    expect(preferChatStatus('readyForReview', 'read')).toBe('read')
    expect(preferChatStatus('read', 'read')).toBe('read')
  })
})

describe('mergeChatStatus', () => {
  it('keeps optimistic inProgress when server list still has read', () => {
    expect(mergeChatStatus('inProgress', 'read')).toBe('inProgress')
  })

  it('delegates other merges to preferChatStatus', () => {
    expect(mergeChatStatus('readyForReview', 'inProgress')).toBe('readyForReview')
    expect(mergeChatStatus('read', 'readyForReview')).toBe('readyForReview')
  })
})
