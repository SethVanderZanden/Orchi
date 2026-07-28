import { beforeEach, describe, expect, it } from 'vitest'

import {
  canUseWorktreeToggle,
  clearWorktreeIntent,
  getWorktreeIntent,
  initializeWorktreeIntentForNewChat,
  isWorktreeIntentEnabled,
  migrateWorktreeIntent,
  resolveDefaultWorktreeIntent,
  setWorktreeIntent,
  setWorktreeIntentBranchName,
  setWorktreeIntentEnabled,
  shouldDefaultWorktreeForMode,
  syncWorktreeIntentWithMode,
  toggleWorktreeIntent
} from './worktree-intent'
import type { Project } from '@/lib/projects/types'

function createProject(overrides: Partial<Project> = {}): Project {
  return {
    id: 'project-1',
    name: 'Test',
    defaultBaseBranch: 'main',
    defaultWorktreeBranchPattern: 'orchi/{date}-{shortId}',
    gitHostProvider: 'github',
    useWorktreeOnKickoff: true,
    workspaces: [],
    createdAt: '2026-01-01T00:00:00.000Z',
    updatedAt: '2026-01-01T00:00:00.000Z',
    ...overrides
  }
}

describe('worktree-intent', () => {
  beforeEach(() => {
    clearWorktreeIntent('a')
    clearWorktreeIntent('b')
    clearWorktreeIntent('from')
    clearWorktreeIntent('to')
  })

  it('stores and clears intent', () => {
    setWorktreeIntent('a', { enabled: true, branchName: 'feature/x' })
    expect(getWorktreeIntent('a')).toEqual({ enabled: true, branchName: 'feature/x' })
    expect(isWorktreeIntentEnabled('a')).toBe(true)

    clearWorktreeIntent('a')
    expect(getWorktreeIntent('a')).toBeUndefined()
    expect(isWorktreeIntentEnabled('a')).toBe(false)
  })

  it('keeps branch name when disabled, clears when both off and empty', () => {
    setWorktreeIntent('a', { enabled: true, branchName: 'x' })
    setWorktreeIntent('a', { enabled: false, branchName: 'x' })
    expect(getWorktreeIntent('a')).toEqual({ enabled: false, branchName: 'x' })
    expect(isWorktreeIntentEnabled('a')).toBe(false)

    setWorktreeIntent('a', { enabled: false, branchName: '' })
    expect(getWorktreeIntent('a')).toBeUndefined()
  })

  it('toggles enabled and preserves branch name', () => {
    setWorktreeIntent('a', { enabled: true, branchName: 'my-branch' })
    toggleWorktreeIntent('a')
    expect(isWorktreeIntentEnabled('a')).toBe(false)

    toggleWorktreeIntent('a')
    expect(getWorktreeIntent('a')).toEqual({ enabled: true, branchName: 'my-branch' })
  })

  it('updates branch name only while enabled', () => {
    setWorktreeIntentBranchName('a', 'ignored')
    expect(getWorktreeIntent('a')).toBeUndefined()

    setWorktreeIntentEnabled('a', true)
    setWorktreeIntentBranchName('a', 'named')
    expect(getWorktreeIntent('a')?.branchName).toBe('named')
  })

  it('migrates intent to a new chat id', () => {
    setWorktreeIntent('from', { enabled: true, branchName: 'orchi/test' })
    migrateWorktreeIntent('from', 'to')
    expect(getWorktreeIntent('from')).toBeUndefined()
    expect(getWorktreeIntent('to')).toEqual({ enabled: true, branchName: 'orchi/test' })
  })

  it('allows the toggle only on empty chats', () => {
    expect(canUseWorktreeToggle(0)).toBe(true)
    expect(canUseWorktreeToggle(1)).toBe(false)
  })

  it('defaults worktree on when the project enables it', () => {
    expect(resolveDefaultWorktreeIntent(createProject(), 'orchestration')).toEqual({
      enabled: true,
      branchName: ''
    })
    expect(resolveDefaultWorktreeIntent(createProject(), 'default')).toEqual({
      enabled: true,
      branchName: ''
    })
    expect(resolveDefaultWorktreeIntent(createProject({ useWorktreeOnKickoff: false }), 'default')).toBeNull()
    expect(resolveDefaultWorktreeIntent(createProject(), 'review')).toBeNull()
  })

  it('initializes intent for new chats from project defaults', () => {
    initializeWorktreeIntentForNewChat('a', createProject(), 'orchestration')
    expect(getWorktreeIntent('a')).toEqual({ enabled: true, branchName: '' })

    clearWorktreeIntent('a')
    initializeWorktreeIntentForNewChat('a', createProject({ useWorktreeOnKickoff: false }), 'default')
    expect(getWorktreeIntent('a')).toBeUndefined()
  })

  it('syncs intent when mode changes on an empty chat', () => {
    setWorktreeIntent('a', { enabled: true, branchName: 'custom' })
    syncWorktreeIntentWithMode('a', createProject(), 'review')
    expect(getWorktreeIntent('a')).toBeUndefined()

    setWorktreeIntent('a', { enabled: true, branchName: 'custom' })
    syncWorktreeIntentWithMode('a', createProject(), 'orchestration')
    expect(getWorktreeIntent('a')).toEqual({ enabled: true, branchName: 'custom' })
  })

  it('skips review mode for default worktree', () => {
    expect(shouldDefaultWorktreeForMode('review')).toBe(false)
    expect(shouldDefaultWorktreeForMode('orchestration')).toBe(true)
  })
})
