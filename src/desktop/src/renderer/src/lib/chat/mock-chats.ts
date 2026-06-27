import type { ChatThread } from './types'

export const MOCK_CHATS: ChatThread[] = [
  {
    id: '1',
    title: 'Pipeline review workflow',
    preview: 'Can you outline a PR review loop with multiple agents?',
    updatedAt: new Date(Date.now() - 1000 * 60 * 12),
    messages: [
      {
        id: '1-1',
        role: 'user',
        content: 'Can you outline a PR review loop with multiple agents?',
        createdAt: new Date(Date.now() - 1000 * 60 * 15)
      },
      {
        id: '1-2',
        role: 'assistant',
        content:
          'A simple loop: assign a reviewer agent, run checks, collect comments, apply fixes in a worktree, then re-run review until merge-ready.',
        createdAt: new Date(Date.now() - 1000 * 60 * 12)
      }
    ]
  },
  {
    id: '2',
    title: 'Worktree naming',
    preview: 'What convention should we use for agent worktrees?',
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 3),
    messages: [
      {
        id: '2-1',
        role: 'user',
        content: 'What convention should we use for agent worktrees?',
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 3)
      },
      {
        id: '2-2',
        role: 'assistant',
        content:
          'Use `{agent}/{task-id}` under a shared worktrees root so paths stay predictable and easy to clean up.',
        createdAt: new Date(Date.now() - 1000 * 60 * 60 * 3 + 1000 * 30)
      }
    ]
  },
  {
    id: '3',
    title: 'New chat',
    preview: 'Start a conversation with Orchi',
    updatedAt: new Date(Date.now() - 1000 * 60 * 60 * 26),
    messages: []
  }
]
