import { app, shell, BrowserWindow, dialog, ipcMain, nativeTheme } from 'electron'
import { join } from 'path'
import { electronApp, optimizer, is } from '@electron-toolkit/utils'
import icon from '../../resources/icon.png?asset'
import { getApiBaseUrl, startApiHost, stopApiHost } from './api-host'

let isShuttingDown = false

async function shutdownApiSessions(): Promise<void> {
  try {
    await fetch(`${getApiBaseUrl()}/chats/shutdown`, { method: 'POST' })
  } catch {
    // Best-effort cleanup when the API is unavailable.
  }
}

function createWindow(): void {
  const mainWindow = new BrowserWindow({
    width: 900,
    height: 670,
    show: false,
    autoHideMenuBar: true,
    title: 'Orchi',
    ...(process.platform === 'linux' ? { icon } : {}),
    ...(process.platform === 'win32' ? { icon } : {}),
    webPreferences: {
      preload: join(__dirname, '../preload/index.js'),
      sandbox: false
    }
  })

  mainWindow.on('ready-to-show', () => {
    mainWindow.show()
  })

  mainWindow.webContents.setWindowOpenHandler((details) => {
    shell.openExternal(details.url)
    return { action: 'deny' }
  })

  if (is.dev && process.env['ELECTRON_RENDERER_URL']) {
    mainWindow.loadURL(process.env['ELECTRON_RENDERER_URL'])
  } else {
    mainWindow.loadFile(join(__dirname, '../renderer/index.html'))
  }
}

app.whenReady().then(async () => {
  nativeTheme.themeSource = 'system'
  electronApp.setAppUserModelId('com.orchi.app')

  app.on('browser-window-created', (_, window) => {
    optimizer.watchWindowShortcuts(window)
  })

  ipcMain.handle('dialog:openDirectory', async (event) => {
    const window = BrowserWindow.fromWebContents(event.sender)
    const result = window
      ? await dialog.showOpenDialog(window, { properties: ['openDirectory'] })
      : await dialog.showOpenDialog({ properties: ['openDirectory'] })

    if (result.canceled || result.filePaths.length === 0) {
      return null
    }

    return result.filePaths[0]
  })

  if (!is.dev) {
    try {
      await startApiHost()
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error)
      await dialog.showErrorBox('Orchi failed to start', message)
      app.quit()
      return
    }
  }

  createWindow()

  app.on('activate', function () {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
    }
  })
})

app.on('before-quit', (event) => {
  if (isShuttingDown) {
    return
  }

  event.preventDefault()
  isShuttingDown = true

  void shutdownApiSessions()
    .then(async () => {
      if (!is.dev) {
        await stopApiHost()
      }
    })
    .finally(() => {
      app.quit()
    })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})
