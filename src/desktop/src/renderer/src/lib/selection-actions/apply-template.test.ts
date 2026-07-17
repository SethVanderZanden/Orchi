import { describe, expect, it } from 'vitest'

import { applySelectionTemplate, containsSelectedTextPlaceholder } from './apply-template'

describe('applySelectionTemplate', () => {
  it('replaces the selected text placeholder', () => {
    expect(applySelectionTemplate('Please define "{{selected text}}" simply.', 'middleware')).toBe(
      'Please define "middleware" simply.'
    )
  })

  it('is case and whitespace insensitive for the placeholder', () => {
    expect(applySelectionTemplate('Define {{ Selected Text }} now.', 'DI')).toBe('Define DI now.')
  })

  it('detects missing placeholders', () => {
    expect(containsSelectedTextPlaceholder('no placeholder')).toBe(false)
    expect(containsSelectedTextPlaceholder('has {{selected text}}')).toBe(true)
  })
})
