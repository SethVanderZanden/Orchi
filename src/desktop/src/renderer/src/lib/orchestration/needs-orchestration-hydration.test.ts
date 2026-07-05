import { describe, expect, it } from 'vitest'

import { createLocalDraftChat } from '@/lib/chat/create-local-draft'
import { needsOrchestrationHydration } from './needs-orchestration-hydration'

describe('needsOrchestrationHydration', () => {
  it('returns false for local draft orchestration chats', () => {
    const draft = createLocalDraftChat({
      workspaceId: 'workspace-1',
      workspacePath: '/tmp/project',
      projectId: 'project-1',
      mode: 'orchestration'
    })

    expect(needsOrchestrationHydration(draft, 0, false)).toBe(false)
  })

  it('returns true for persisted orchestration chats with messages', () => {
    expect(
      needsOrchestrationHydration(
        {
          ...createLocalDraftChat({
            workspaceId: 'workspace-1',
            workspacePath: '/tmp/project',
            projectId: 'project-1',
            mode: 'orchestration'
          }),
          id: '11111111-1111-1111-1111-111111111111',
          messages: [
            {
              id: 'message-1',
              role: 'assistant',
              content: 'plan',
              createdAt: '2026-07-05T00:00:00.000Z',
              status: 'complete'
            }
          ]
        },
        0,
        false
      )
    ).toBe(true)
  })
})
