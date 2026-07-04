import { describe, expect, it, vi } from 'vitest'

import {
  getEditorCliCommand,
  getEditorProtocolUrl,
  getKnownEditorInstallPaths,
  openInEditor,
  type OpenInEditorDeps
} from './open-in-editor'

describe('getEditorProtocolUrl', () => {
  it('builds vscode protocol url', () => {
    const url = getEditorProtocolUrl('C:\\Projects\\Orchi', 'vscode')

    expect(url.startsWith('vscode://file/')).toBe(true)
    expect(url).toContain('Orchi')
  })

  it('builds cursor protocol url', () => {
    const url = getEditorProtocolUrl('/home/user/project', 'cursor')

    expect(url.startsWith('cursor://file/')).toBe(true)
    expect(url).toContain('project')
  })
})

describe('getEditorCliCommand', () => {
  it('returns code for vscode', () => {
    expect(getEditorCliCommand('vscode')).toBe('code')
  })

  it('returns cursor for cursor', () => {
    expect(getEditorCliCommand('cursor')).toBe('cursor')
  })
})

describe('getKnownEditorInstallPaths', () => {
  it('returns non-empty paths for each editor', () => {
    expect(getKnownEditorInstallPaths('vscode').length).toBeGreaterThan(0)
    expect(getKnownEditorInstallPaths('cursor').length).toBeGreaterThan(0)
  })
})

describe('openInEditor', () => {
  const folderPath = 'C:\\Projects\\Orchi'

  function createDeps(overrides: Partial<OpenInEditorDeps> = {}): OpenInEditorDeps {
    return {
      openExternal: vi.fn().mockResolvedValue(undefined),
      spawnDetached: vi.fn().mockReturnValue(false),
      fileExists: vi.fn().mockReturnValue(true),
      ...overrides
    }
  }

  it('returns error when path is empty', async () => {
    const result = await openInEditor('  ', 'vscode', createDeps())

    expect(result).toEqual({ ok: false, error: 'Workspace path is empty.' })
  })

  it('returns error when path does not exist', async () => {
    const result = await openInEditor(folderPath, 'vscode', createDeps({ fileExists: () => false }))

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.error).toContain('Path does not exist')
    }
  })

  it('opens via protocol when openExternal succeeds', async () => {
    const deps = createDeps()
    const result = await openInEditor(folderPath, 'vscode', deps)

    expect(result).toEqual({ ok: true })
    expect(deps.openExternal).toHaveBeenCalledWith(getEditorProtocolUrl(folderPath, 'vscode'))
    expect(deps.spawnDetached).not.toHaveBeenCalled()
  })

  it('falls back to cli when protocol fails', async () => {
    const deps = createDeps({
      openExternal: vi.fn().mockRejectedValue(new Error('protocol failed')),
      spawnDetached: vi.fn().mockReturnValue(true)
    })

    const result = await openInEditor(folderPath, 'cursor', deps)

    expect(result).toEqual({ ok: true })
    expect(deps.spawnDetached).toHaveBeenCalledWith('cursor', [folderPath])
  })

  it('falls back to known install path when cli fails', async () => {
    const installPath = getKnownEditorInstallPaths('vscode')[0]
    const deps = createDeps({
      openExternal: vi.fn().mockRejectedValue(new Error('protocol failed')),
      spawnDetached: vi.fn().mockImplementation((command: string) => command === installPath),
      fileExists: vi.fn().mockImplementation((path: string) => path === folderPath || path === installPath)
    })

    const result = await openInEditor(folderPath, 'vscode', deps)

    expect(result).toEqual({ ok: true })
    expect(deps.spawnDetached).toHaveBeenCalledWith(installPath, [folderPath])
  })

  it('returns error when all strategies fail', async () => {
    const deps = createDeps({
      openExternal: vi.fn().mockRejectedValue(new Error('protocol failed')),
      spawnDetached: vi.fn().mockReturnValue(false),
      fileExists: vi.fn().mockImplementation((path: string) => path === folderPath)
    })

    const result = await openInEditor(folderPath, 'vscode', deps)

    expect(result).toEqual({
      ok: false,
      error: 'Could not open VS Code. Install it or add it to PATH.'
    })
  })
})
