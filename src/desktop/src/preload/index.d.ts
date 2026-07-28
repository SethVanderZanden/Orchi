import { ElectronAPI } from '@electron-toolkit/preload'

export type EditorId = 'vscode' | 'cursor'

export type OpenInEditorResult = { ok: true } | { ok: false; error: string }

export type OpenLogFolderResult = { logFile: string; logDirectory: string }

export type EditorGotoLocation = {
  relativePath: string
  line: number
  column?: number
}

export interface OrchiApi {
  openDirectory: () => Promise<string | null>
  openInEditor: (
    folderPath: string,
    editor: EditorId,
    location?: EditorGotoLocation
  ) => Promise<OpenInEditorResult>
  getLogPath: () => Promise<string>
  openLogFolder: () => Promise<OpenLogFolderResult>
}

declare global {
  interface Window {
    electron: ElectronAPI
    api: OrchiApi
  }
}
