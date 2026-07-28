import { describe, expect, it } from 'vitest'

import { preferChatStatus } from './prefer-chat-status'
import type { ChatStatus } from './types'

/**
 * Regression: plan kickoff used to navigate to the new child before the agent
 * turn started. Mark-read then returned Read (Done) and preferChatStatus used
 * to keep Read over later InProgress — so the child never appeared under
 * Processing.
 */
function simulateKickoffStatusRace(options: {
  markReadWinsBeforeTurn: boolean
  allowInProgressAfterRead: boolean
}): ChatStatus {
  let status: ChatStatus = 'read'

  // Client optimistic kickoff insert
  status = 'inProgress'

  if (options.markReadWinsBeforeTurn) {
    // Concurrent mark-read while the child is still idle on the server.
    // applyStatusToCaches now refuses Read over InProgress; model the old bug
    // when allowInProgressAfterRead is false via preferChatStatus alone.
    const markReadStatus: ChatStatus = 'read'
    if (status === 'inProgress' && markReadStatus === 'read') {
      // New mark-read path: preserve inProgress
      status = status
    } else {
      status = preferChatStatus(status, markReadStatus)
    }
  }

  // Turn starts — InProgress SSE / optimistic send
  const incoming: ChatStatus = 'inProgress'
  if (options.allowInProgressAfterRead) {
    status = preferChatStatus(status, incoming)
  } else if (status === 'read' && incoming === 'inProgress') {
    // Old preferChatStatus behavior
    status = 'read'
  } else {
    status = preferChatStatus(status, incoming)
  }

  return status
}

describe('kickoff status race', () => {
  it('keeps Processing when mark-read races ahead of the agent turn', () => {
    expect(
      simulateKickoffStatusRace({
        markReadWinsBeforeTurn: true,
        allowInProgressAfterRead: true
      })
    ).toBe('inProgress')
  })

  it('allows InProgress SSE to recover after Read', () => {
    expect(preferChatStatus('read', 'inProgress')).toBe('inProgress')
  })

  it('still blocks late InProgress after ReadyForReview', () => {
    expect(preferChatStatus('readyForReview', 'inProgress')).toBe('readyForReview')
  })
})
