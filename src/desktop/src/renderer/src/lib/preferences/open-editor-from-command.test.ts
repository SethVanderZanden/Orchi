import { beforeEach, describe, expect, it, vi } from 'vitest'

import { openEditorFromCommand } from '@/lib/preferences/open-editor-from-command'

describe('openEditorFromCommand', () => {
  beforeEach(() => {
    window.api = {
      ...window.api,
      openInEditor: vi.fn().mockResolvedValue({ ok: true })
    }
  })

  it('opens using the workspace path from the command when no chat workspace is provided', async () => {
    const error = await openEditorFromCommand('code /workspace/primary -g src/a.ts:3')

    expect(error).toBeNull()
    expect(window.api.openInEditor).toHaveBeenCalledWith(
      '/workspace/primary',
      expect.any(String),
      {
        relativePath: 'src/a.ts',
        line: 3
      }
    )
  })

  it('prefers the chat workspace over the embedded command workspace', async () => {
    const error = await openEditorFromCommand(
      'code /workspace/primary -g src/a.ts:3:2',
      '/workspace/worktree'
    )

    expect(error).toBeNull()
    expect(window.api.openInEditor).toHaveBeenCalledWith(
      '/workspace/worktree',
      expect.any(String),
      {
        relativePath: 'src/a.ts',
        line: 3,
        column: 2
      }
    )
  })
})
