import { describe, expect, it } from 'vitest'

import { createLocalDraftChat } from './create-local-draft'
import { isLocalChat } from './chat-persistence'

describe('createLocalDraftChat', () => {
  it('creates a local id and default mode', () => {
    const draft = createLocalDraftChat({
      workspaceId: 'workspace-1',
      workspacePath: '/tmp/project',
      projectId: 'project-1'
    })

    expect(isLocalChat(draft.id)).toBe(true)
    expect(draft.mode).toBe('default')
    expect(draft.messages).toEqual([])
    expect(draft.workspaceId).toBe('workspace-1')
    expect(draft.workspacePath).toBe('/tmp/project')
  })
})
