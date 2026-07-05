import { QueryClient } from '@tanstack/react-query'
import { describe, expect, it, beforeEach } from 'vitest'

import { createLocalDraftChat } from './create-local-draft'
import { promoteLocalChat, __resetPromotionLocksForTests } from './promote-local-chat'
import { chatKeys } from '@/lib/query-keys'

describe('promoteLocalChat', () => {
  beforeEach(() => {
    __resetPromotionLocksForTests()
  })

  it('returns the same id for persisted chats', async () => {
    const queryClient = new QueryClient()
    const id = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890'

    await expect(promoteLocalChat(queryClient, id)).resolves.toBe(id)
  })

  it('migrates cache from local id to persisted id', async () => {
    const queryClient = new QueryClient()
    const draft = createLocalDraftChat({
      workspaceId: '00000000-0000-0000-0000-000000000001',
      workspacePath: '/tmp/project',
      projectId: 'project-1',
      mode: 'orchestration'
    })

    queryClient.setQueryData(chatKeys.lists(), [draft])
    queryClient.setQueryData(chatKeys.detail(draft.id), draft)

    const originalFetch = globalThis.fetch
    globalThis.fetch = async () =>
      new Response(
        JSON.stringify({
          id: '11111111-1111-1111-1111-111111111111',
          agentId: 'cursor',
          projectId: 'project-1',
          workspaceId: draft.workspaceId,
          workspacePath: draft.workspacePath,
          mode: 'orchestration',
          modelId: null,
          parentChatId: null,
          planFilePath: null
        }),
        { status: 201, headers: { 'Content-Type': 'application/json' } }
      )

    try {
      const persistedId = await promoteLocalChat(queryClient, draft.id)
      expect(persistedId).toBe('11111111-1111-1111-1111-111111111111')

      const list = queryClient.getQueryData<(typeof draft)[]>(chatKeys.lists()) ?? []
      expect(list.some((chat) => chat.id === draft.id)).toBe(false)
      expect(list.some((chat) => chat.id === persistedId)).toBe(true)
      expect(queryClient.getQueryData(chatKeys.detail(draft.id))).toBeUndefined()
      expect(queryClient.getQueryData(chatKeys.detail(persistedId))).toMatchObject({
        mode: 'orchestration'
      })
    } finally {
      globalThis.fetch = originalFetch
    }
  })
})
