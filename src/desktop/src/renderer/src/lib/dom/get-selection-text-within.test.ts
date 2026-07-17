import { afterEach, describe, expect, it, vi } from 'vitest'

import { getSelectionTextWithin } from './get-selection-text-within'

describe('getSelectionTextWithin', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('returns empty when there is no selection', () => {
    vi.stubGlobal('getSelection', () => null)
    expect(getSelectionTextWithin(document.body)).toBe('')
  })

  it('returns empty when selection is collapsed', () => {
    vi.stubGlobal('getSelection', () => ({
      rangeCount: 1,
      isCollapsed: true,
      getRangeAt: () => ({ commonAncestorContainer: document.body })
    }))
    expect(getSelectionTextWithin(document.body)).toBe('')
  })

  it('returns empty when selection is outside the container', () => {
    const container = document.createElement('div')
    const outside = document.createElement('span')
    vi.stubGlobal('getSelection', () => ({
      rangeCount: 1,
      isCollapsed: false,
      getRangeAt: () => ({ commonAncestorContainer: outside }),
      toString: () => 'hello'
    }))
    expect(getSelectionTextWithin(container)).toBe('')
  })

  it('returns trimmed selection text inside the container', () => {
    const container = document.createElement('div')
    const child = document.createElement('span')
    container.appendChild(child)
    vi.stubGlobal('getSelection', () => ({
      rangeCount: 1,
      isCollapsed: false,
      getRangeAt: () => ({ commonAncestorContainer: child }),
      toString: () => '  hello\u00a0world  '
    }))
    expect(getSelectionTextWithin(container)).toBe('hello world')
  })
})
