import { describe, expect, it } from 'vitest'

import { getChatMessageDisplayState } from './message-display'
import type { ChatMarker, ChatMessage } from '@/lib/chat/types'

function createMessage(overrides: Partial<ChatMessage> = {}): ChatMessage {
  return {
    id: 'msg-1',
    role: 'assistant',
    content: 'Hello',
    createdAt: new Date().toISOString(),
    status: 'complete',
    ...overrides
  }
}

const toolMarker: ChatMarker = {
  id: 'marker-1',
  variant: 'tool',
  content: 'Reading file'
}

describe('getChatMessageDisplayState', () => {
  it('passes through non-orchestration assistant content', () => {
    const state = getChatMessageDisplayState({
      message: createMessage({ content: 'Normal reply' }),
      mode: 'default',
      rowMarkers: []
    })

    expect(state).toEqual({
      displayContent: 'Normal reply',
      showPlaceholder: false,
      showActivity: false,
      shouldRender: true
    })
  })

  it('strips review plan blocks from display content', () => {
    const content = `Intro.

<!-- orchi-review-plan:auth-refactor -->
# Auth Refactor Review
<!-- /orchi-review-plan -->

Done.`

    const state = getChatMessageDisplayState({
      message: createMessage({ content }),
      mode: 'review',
      rowMarkers: []
    })

    expect(state.displayContent).toBe('Intro.\n\nDone.')
    expect(state.shouldRender).toBe(true)
  })

  it('strips orchestration plan blocks from display content', () => {
    const content = `Intro.

<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->

Done.`

    const state = getChatMessageDisplayState({
      message: createMessage({ content }),
      mode: 'orchestration',
      rowMarkers: []
    })

    expect(state.displayContent).toBe('Intro.\n\nDone.')
    expect(state.shouldRender).toBe(true)
  })

  it('skips completed plan-only orchestration bubbles', () => {
    const content = `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->`

    const state = getChatMessageDisplayState({
      message: createMessage({ content, status: 'complete' }),
      mode: 'orchestration',
      rowMarkers: []
    })

    expect(state.displayContent).toBe('')
    expect(state.showPlaceholder).toBe(false)
    expect(state.showActivity).toBe(false)
    expect(state.shouldRender).toBe(false)
  })

  it('keeps a placeholder while an empty orchestration turn is streaming', () => {
    const content = `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
Still streaming`

    const state = getChatMessageDisplayState({
      message: createMessage({ content, status: 'streaming' }),
      mode: 'orchestration',
      rowMarkers: []
    })

    expect(state.displayContent).toBe('')
    expect(state.showPlaceholder).toBe(true)
    expect(state.shouldRender).toBe(true)
  })

  it('keeps activity rows even when display content is empty', () => {
    const content = `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->`

    const state = getChatMessageDisplayState({
      message: createMessage({ content, status: 'streaming' }),
      mode: 'orchestration',
      rowMarkers: [toolMarker]
    })

    expect(state.displayContent).toBe('')
    expect(state.showPlaceholder).toBe(false)
    expect(state.showActivity).toBe(true)
    expect(state.shouldRender).toBe(true)
  })

  it('always renders error messages even when stripped content is empty', () => {
    const content = `<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->`

    const state = getChatMessageDisplayState({
      message: createMessage({ content, status: 'error' }),
      mode: 'orchestration',
      rowMarkers: []
    })

    expect(state.displayContent).toBe('')
    expect(state.shouldRender).toBe(true)
  })

  it('always renders user messages unchanged', () => {
    const state = getChatMessageDisplayState({
      message: createMessage({
        role: 'user',
        content: 'Please plan the auth work',
        status: 'complete'
      }),
      mode: 'orchestration',
      rowMarkers: []
    })

    expect(state).toEqual({
      displayContent: 'Please plan the auth work',
      showPlaceholder: false,
      showActivity: false,
      shouldRender: true
    })
  })
})
