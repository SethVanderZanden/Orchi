import { describe, expect, it } from 'vitest'

import { parseOpenEditorCommand } from './parse-open-editor-command'
import {
  decodeOpenEditorLinkHref,
  isOpenEditorLinkHref,
  transformOpenEditorBlocksForDisplay
} from './transform-open-editor-blocks'

describe('parseOpenEditorCommand', () => {
  it('parses a basic code command', () => {
    expect(
      parseOpenEditorCommand(
        'code /workspace/project -g src/MyProject/Services/UserService.cs:87'
      )
    ).toEqual({
      workspacePath: '/workspace/project',
      relativePath: 'src/MyProject/Services/UserService.cs',
      line: 87
    })
  })

  it('parses cursor with column', () => {
    expect(
      parseOpenEditorCommand('cursor /workspace/project -g src/file.ts:12:5')
    ).toEqual({
      workspacePath: '/workspace/project',
      relativePath: 'src/file.ts',
      line: 12,
      column: 5
    })
  })

  it('parses quoted workspace paths', () => {
    expect(
      parseOpenEditorCommand('code "/workspace/my project" -g src/file.ts:1')
    ).toEqual({
      workspacePath: '/workspace/my project',
      relativePath: 'src/file.ts',
      line: 1
    })
  })

  it('returns null for invalid commands', () => {
    expect(parseOpenEditorCommand('open /workspace/file.ts')).toBeNull()
    expect(parseOpenEditorCommand('code /workspace -g file')).toBeNull()
  })
})

describe('transformOpenEditorBlocksForDisplay', () => {
  it('replaces open-editor blocks with markdown links', () => {
    const content = `See <orchi-open-editor>code /workspace/project -g src/file.ts:42</orchi-open-editor> for details.`

    expect(transformOpenEditorBlocksForDisplay(content)).toBe(
      'See [src/file.ts:42](orchi-open-editor:code%20%2Fworkspace%2Fproject%20-g%20src%2Ffile.ts%3A42) for details.'
    )
  })

  it('leaves invalid commands as plain text', () => {
    const content = '<orchi-open-editor>not a command</orchi-open-editor>'

    expect(transformOpenEditorBlocksForDisplay(content)).toBe('not a command')
  })
})

describe('open editor link helpers', () => {
  it('detects and decodes open-editor hrefs', () => {
    const href =
      'orchi-open-editor:code%20%2Fworkspace%20-g%20src%2Ffile.ts%3A10'

    expect(isOpenEditorLinkHref(href)).toBe(true)
    expect(decodeOpenEditorLinkHref(href)).toBe('code /workspace -g src/file.ts:10')
  })
})
