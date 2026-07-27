import { contextBridge, ipcRenderer } from 'electron'
import { electronAPI } from '@electron-toolkit/preload'

const api = {
  openDirectory: (): Promise<string | null> => ipcRenderer.invoke('dialog:openDirectory'),
  openInEditor: (
    folderPath: string,
    editor: 'vscode' | 'cursor',
    location?: { relativePath: string; line: number; column?: number }
  ): Promise<{ ok: true } | { ok: false; error: string }> =>
    ipcRenderer.invoke('shell:openInEditor', folderPath, editor, location),
  getLogPath: (): Promise<string> => ipcRenderer.invoke('logs:getPath'),
  openLogFolder: (): Promise<{ logFile: string; logDirectory: string }> =>
    ipcRenderer.invoke('logs:openFolder')
}

if (process.contextIsolated) {
  try {
    contextBridge.exposeInMainWorld('electron', electronAPI)
    contextBridge.exposeInMainWorld('api', api)
  } catch (error) {
    // Preload runs before renderer logging is available.
    console.error(error)
  }
} else {
  // @ts-ignore (define in dts)
  window.electron = electronAPI
  // @ts-ignore (define in dts)
  window.api = api
}
