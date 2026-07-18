import { beforeEach, describe, expect, it } from 'vitest'

import {
  canUseWorktreeToggle,
  clearWorktreeIntent,
  getWorktreeIntent,
  isWorktreeIntentEnabled,
  migrateWorktreeIntent,
  setWorktreeIntent,
  setWorktreeIntentBranchName,
  setWorktreeIntentEnabled,
  toggleWorktreeIntent
} from './worktree-intent'

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
})
