import { beforeEach, describe, expect, it } from 'vitest'

import {
  getComposerDraft,
  hasComposerDraft,
  migrateComposerDraft,
  setComposerDraft,
  takeComposerDraft
} from './composer-drafts'

describe('composer-drafts', () => {
  beforeEach(() => {
    // Clear any leftover drafts between tests via take.
    takeComposerDraft('a')
    takeComposerDraft('b')
    takeComposerDraft('from')
    takeComposerDraft('to')
  })

  it('reads a draft without consuming it', () => {
    setComposerDraft('a', 'hello')
    expect(getComposerDraft('a')).toBe('hello')
    expect(getComposerDraft('a')).toBe('hello')
  })

  it('stores and consumes a draft once', () => {
    setComposerDraft('a', '  hello  ')
    expect(takeComposerDraft('a')).toBe('  hello')
    expect(takeComposerDraft('a')).toBeUndefined()
  })

  it('ignores blank drafts', () => {
    setComposerDraft('a', '   ')
    expect(takeComposerDraft('a')).toBeUndefined()
  })

  it('tracks whether a draft exists', () => {
    expect(hasComposerDraft('a')).toBe(false)
    setComposerDraft('a', 'hello')
    expect(hasComposerDraft('a')).toBe(true)
    takeComposerDraft('a')
    expect(hasComposerDraft('a')).toBe(false)
  })

  it('migrates a draft to a new chat id', () => {
    setComposerDraft('from', 'selected text')
    migrateComposerDraft('from', 'to')
    expect(takeComposerDraft('from')).toBeUndefined()
    expect(takeComposerDraft('to')).toBe('selected text')
  })
})
