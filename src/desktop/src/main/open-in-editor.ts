import { spawn } from 'child_process'
import { existsSync } from 'fs'
import { homedir } from 'os'
import path from 'path'
import { shell } from 'electron'

export type EditorId = 'vscode' | 'cursor'

export type OpenInEditorResult = { ok: true } | { ok: false; error: string }

export type OpenInEditorDeps = {
  openExternal: (url: string) => Promise<void>
  spawnDetached: (command: string, args: string[]) => boolean
  fileExists: (filePath: string) => boolean
}

function toFileUriPath(folderPath: string): string {
  const normalized = path.resolve(folderPath)
  if (process.platform === 'win32') {
    return normalized.replace(/\\/g, '/')
  }

  return normalized
}

export function getEditorProtocolUrl(folderPath: string, editor: EditorId): string {
  const protocol = editor === 'vscode' ? 'vscode' : 'cursor'
  return `${protocol}://file/${encodeURI(toFileUriPath(folderPath))}`
}

export function getEditorCliCommand(editor: EditorId): string {
  return editor === 'vscode' ? 'code' : 'cursor'
}

export function getKnownEditorInstallPaths(editor: EditorId): string[] {
  if (process.platform === 'win32') {
    const localAppData = process.env.LOCALAPPDATA ?? path.join(homedir(), 'AppData', 'Local')
    if (editor === 'vscode') {
      return [
        path.join(localAppData, 'Programs', 'Microsoft VS Code', 'bin', 'code.cmd'),
        path.join(localAppData, 'Programs', 'Microsoft VS Code', 'Code.exe')
      ]
    }

    return [path.join(localAppData, 'Programs', 'cursor', 'Cursor.exe')]
  }

  if (process.platform === 'darwin') {
    if (editor === 'vscode') {
      return [
        '/usr/local/bin/code',
        '/Applications/Visual Studio Code.app/Contents/Resources/app/bin/code'
      ]
    }

    return ['/usr/local/bin/cursor', '/Applications/Cursor.app/Contents/MacOS/Cursor']
  }

  if (editor === 'vscode') {
    return ['/usr/bin/code', '/snap/bin/code']
  }

  return ['/usr/local/bin/cursor', '/usr/bin/cursor']
}

function spawnDetachedDefault(command: string, args: string[]): boolean {
  try {
    const child = spawn(command, args, { detached: true, stdio: 'ignore', shell: true })
    child.unref()
    return true
  } catch {
    return false
  }
}

const defaultDeps: OpenInEditorDeps = {
  openExternal: (url) => shell.openExternal(url),
  spawnDetached: spawnDetachedDefault,
  fileExists: existsSync
}

export async function openInEditor(
  folderPath: string,
  editor: EditorId,
  deps: OpenInEditorDeps = defaultDeps
): Promise<OpenInEditorResult> {
  if (!folderPath.trim()) {
    return { ok: false, error: 'Workspace path is empty.' }
  }

  const resolvedPath = path.resolve(folderPath)
  if (!deps.fileExists(resolvedPath)) {
    return { ok: false, error: `Path does not exist: ${resolvedPath}` }
  }

  try {
    await deps.openExternal(getEditorProtocolUrl(resolvedPath, editor))
    return { ok: true }
  } catch {
    // Fall through to CLI and install-path strategies.
  }

  const cli = getEditorCliCommand(editor)
  if (deps.spawnDetached(cli, [resolvedPath])) {
    return { ok: true }
  }

  for (const installPath of getKnownEditorInstallPaths(editor)) {
    if (!deps.fileExists(installPath)) {
      continue
    }

    if (deps.spawnDetached(installPath, [resolvedPath])) {
      return { ok: true }
    }

    if (editor === 'cursor' && installPath.endsWith('Cursor.exe')) {
      const folderUri = `file:///${toFileUriPath(resolvedPath)}`
      if (deps.spawnDetached(installPath, ['--folder-uri', folderUri])) {
        return { ok: true }
      }
    }
  }

  const label = editor === 'vscode' ? 'VS Code' : 'Cursor'
  return { ok: false, error: `Could not open ${label}. Install it or add it to PATH.` }
}
