import { ElectronAPI } from '@electron-toolkit/preload'

export type EditorId = 'vscode' | 'cursor'

export type OpenInEditorResult = { ok: true } | { ok: false; error: string }

export interface OrchiApi {
  openDirectory: () => Promise<string | null>
  openInEditor: (folderPath: string, editor: EditorId) => Promise<OpenInEditorResult>
}

declare global {
  interface Window {
    electron: ElectronAPI
    api: OrchiApi
  }
}
