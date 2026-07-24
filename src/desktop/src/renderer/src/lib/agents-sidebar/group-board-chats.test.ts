import { describe, expect, it } from 'vitest'

import { groupBoardChats } from './group-board-chats'
import type { ChatThread } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'

function createChat(overrides: Partial<ChatThread> = {}): ChatThread {
  return {
    id: 'chat-1',
    title: 'Chat',
    preview: '',
    updatedAt: '2026-07-16T12:00:00.000Z',
    agentId: 'cursor',
    projectId: null,
    workspaceId: null,
    workspacePath: 'E:/proj',
    mode: 'default',
    modelId: null,
    contextSizeId: null,
    reasoningEffortId: null,
    approvalPolicyId: null,
    parentChatId: null,
    planFilePath: null,
    status: 'read',
    lastReadAt: null,
    messages: [],
    ...overrides
  }
}

function createProject(id: string, name: string): Project {
  return {
    id,
    name,
    defaultBaseBranch: 'main',
    defaultWorktreeBranchPattern: 'orchi/{slug}',
    gitHostProvider: 'github',
    useWorktreeOnKickoff: false,
    workspaces: [
      {
        id: `${id}-ws`,
        projectId: id,
        name: 'main',
        path: `E:/${name}`,
        kind: 'primary',
        isDefault: true,
        branch: null,
        baseBranch: null,
        createdAt: '2026-07-16T12:00:00.000Z'
      }
    ],
    createdAt: '2026-07-16T12:00:00.000Z',
    updatedAt: '2026-07-16T12:00:00.000Z'
  }
}

describe('groupBoardChats', () => {
  it('groups by state into vertical status sections', () => {
    const chats = [
      createChat({ id: 'a', status: 'inProgress', projectId: 'p1' }),
      createChat({ id: 'b', status: 'readyForReview', projectId: 'p1' }),
      createChat({ id: 'c', status: 'read', projectId: 'p2' })
    ]

    const view = groupBoardChats(chats, {
      grouping: 'state',
      projects: [createProject('p1', 'Alpha'), createProject('p2', 'Beta')]
    })

    expect(view.mode).toBe('state')
    if (view.mode !== 'state') {
      return
    }

    expect(view.sections.map((section) => section.title)).toEqual([
      'Processing',
      'Ready to Review',
      'Done'
    ])
    expect(view.sections[0]?.chats.map((chat) => chat.id)).toEqual(['a'])
    expect(view.sections[1]?.chats.map((chat) => chat.id)).toEqual(['b'])
    expect(view.sections[2]?.chats.map((chat) => chat.id)).toEqual(['c'])
  })

  it('groups by project first, then by state under each project', () => {
    const chats = [
      createChat({
        id: 'alpha-done',
        status: 'read',
        projectId: 'p1',
        updatedAt: '2026-07-16T09:00:00.000Z'
      }),
      createChat({
        id: 'alpha-busy',
        status: 'inProgress',
        projectId: 'p1',
        updatedAt: '2026-07-16T11:00:00.000Z'
      }),
      createChat({
        id: 'beta-ready',
        status: 'readyForReview',
        projectId: 'p2',
        updatedAt: '2026-07-16T10:00:00.000Z'
      }),
      createChat({
        id: 'orphan',
        status: 'read',
        projectId: null,
        updatedAt: '2026-07-16T08:00:00.000Z'
      })
    ]

    const view = groupBoardChats(chats, {
      grouping: 'project',
      projects: [createProject('p2', 'Beta'), createProject('p1', 'Alpha')]
    })

    expect(view.mode).toBe('project')
    if (view.mode !== 'project') {
      return
    }

    expect(view.projects.map((project) => project.name)).toEqual(['Alpha', 'Beta', 'Other'])
    expect(view.projects[0]?.sections[0]?.chats.map((chat) => chat.id)).toEqual(['alpha-busy'])
    expect(view.projects[0]?.sections[2]?.chats.map((chat) => chat.id)).toEqual(['alpha-done'])
    expect(view.projects[1]?.sections[1]?.chats.map((chat) => chat.id)).toEqual(['beta-ready'])
    expect(view.projects[2]?.sections[2]?.chats.map((chat) => chat.id)).toEqual(['orphan'])
  })

  it('omits empty projects when grouping by project', () => {
    const chats = [createChat({ id: 'only', status: 'inProgress', projectId: 'p1' })]

    const view = groupBoardChats(chats, {
      grouping: 'project',
      projects: [createProject('p1', 'Alpha'), createProject('p2', 'Empty')]
    })

    expect(view.mode).toBe('project')
    if (view.mode !== 'project') {
      return
    }

    expect(view.projects.map((project) => project.id)).toEqual(['p1'])
  })
})
