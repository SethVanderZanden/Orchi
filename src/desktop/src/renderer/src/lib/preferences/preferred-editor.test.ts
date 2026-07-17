import { describe, expect, it, beforeEach } from 'vitest'

import {
  getAlternateEditor,
  getEditorLabel,
  getOpenInEditorLabel,
  getPreferredEditor,
  isEditorId,
  setPreferredEditor
} from '@/lib/preferences/preferred-editor'

describe('preferred editor', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('defaults to vscode', () => {
    expect(getPreferredEditor()).toBe('vscode')
  })

  it('persists and reads the preference', () => {
    setPreferredEditor('cursor')
    expect(getPreferredEditor()).toBe('cursor')
  })

  it('validates editor ids', () => {
    expect(isEditorId('vscode')).toBe(true)
    expect(isEditorId('cursor')).toBe(true)
    expect(isEditorId('vim')).toBe(false)
  })

  it('builds labels', () => {
    expect(getEditorLabel('vscode')).toBe('VS Code')
    expect(getOpenInEditorLabel('cursor')).toBe('Open in Cursor')
    expect(getAlternateEditor('vscode')).toBe('cursor')
  })
})
