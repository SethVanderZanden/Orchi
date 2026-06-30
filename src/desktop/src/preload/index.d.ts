import { ElectronAPI } from '@electron-toolkit/preload'

export interface OrchiApi {
  openDirectory: () => Promise<string | null>
}

declare global {
  interface Window {
    electron: ElectronAPI
    api: OrchiApi
  }
}
