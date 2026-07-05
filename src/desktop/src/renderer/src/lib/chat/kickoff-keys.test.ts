import { describe, expect, it } from 'vitest'

import {
  isParentKickingOffAnyKeys,
  kickOffKey,
  removeKickoffKeysForParent
} from './kickoff-keys'

describe('kickOffKey', () => {
  it('combines parent chat id and plan id', () => {
    expect(kickOffKey('parent-1', 'plan-a')).toBe('parent-1:plan-a')
  })
})

describe('isParentKickingOffAnyKeys', () => {
  it('returns true when a key matches the parent prefix', () => {
    const keys = new Set([kickOffKey('parent-1', 'plan-a'), kickOffKey('other', 'plan-b')])

    expect(isParentKickingOffAnyKeys('parent-1', keys)).toBe(true)
  })

  it('returns false when no keys match the parent prefix', () => {
    const keys = new Set([kickOffKey('other', 'plan-a')])

    expect(isParentKickingOffAnyKeys('parent-1', keys)).toBe(false)
  })
})

describe('removeKickoffKeysForParent', () => {
  it('removes keys for the given parent and returns the same set when unchanged', () => {
    const keys = new Set([kickOffKey('parent-1', 'plan-a'), kickOffKey('other', 'plan-b')])

    const next = removeKickoffKeysForParent('parent-1', keys)

    expect(next).not.toBe(keys)
    expect(next.has(kickOffKey('parent-1', 'plan-a'))).toBe(false)
    expect(next.has(kickOffKey('other', 'plan-b'))).toBe(true)
  })

  it('returns the original set when nothing matches', () => {
    const keys = new Set([kickOffKey('other', 'plan-a')])

    expect(removeKickoffKeysForParent('parent-1', keys)).toBe(keys)
  })
})
