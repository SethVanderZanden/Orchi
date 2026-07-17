import { beforeEach, describe, expect, it } from 'vitest'

import { setDefaultChatMode } from '@/lib/preferences/default-chat-mode'

import { createLocalDraftChat } from './create-local-draft'
import { isLocalChat } from './chat-persistence'

describe('createLocalDraftChat', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('creates a local id and preferred default mode', () => {
    const draft = createLocalDraftChat({
      workspaceId: 'workspace-1',
      workspacePath: '/tmp/project',
      projectId: 'project-1'
    })

    expect(isLocalChat(draft.id)).toBe(true)
    expect(draft.mode).toBe('orchestration')
    expect(draft.messages).toEqual([])
    expect(draft.workspaceId).toBe('workspace-1')
    expect(draft.workspacePath).toBe('/tmp/project')
  })

  it('uses the saved preference when no mode is provided', () => {
    setDefaultChatMode('default')

    const draft = createLocalDraftChat({
      workspaceId: 'workspace-1',
      workspacePath: '/tmp/project',
      projectId: 'project-1'
    })

    expect(draft.mode).toBe('default')
  })

  it('respects an explicit mode override', () => {
    setDefaultChatMode('orchestration')

    const draft = createLocalDraftChat({
      workspaceId: 'workspace-1',
      workspacePath: '/tmp/project',
      projectId: 'project-1',
      mode: 'review'
    })

    expect(draft.mode).toBe('review')
  })
})
