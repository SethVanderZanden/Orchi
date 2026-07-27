import { app, shell, BrowserWindow, dialog, ipcMain, nativeTheme, screen } from 'electron'
import { join } from 'path'
import { electronApp, optimizer, is } from '@electron-toolkit/utils'
import icon from '../../resources/icon.png?asset'
import { getApiBaseUrl, startApiHost, stopApiHost } from './api-host'
import { getLogDirectory, getLogFilePath, log, setupLogging } from './logging'
import { openInEditor, type EditorId } from './open-in-editor'
import { BeforeQuitState, createDefaultShutdownDeps, handleBeforeQuit } from './shutdown'
import { attachWindowRecovery } from './window-recovery'
import { getDefaultWindowSize } from './window-size'

setupLogging()

const shutdownState = { current: BeforeQuitState.NotStarted }

function createWindow(): void {
  const { width, height } = getDefaultWindowSize(screen.getPrimaryDisplay().workAreaSize)

  const mainWindow = new BrowserWindow({
    width,
    height,
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

  attachWindowRecovery(mainWindow)

  mainWindow.on('ready-to-show', () => {
    mainWindow.show()
  })

  mainWindow.webContents.setWindowOpenHandler((details) => {
    shell.openExternal(details.url)
    return { action: 'deny' }
  })

  if (is.dev && process.env['ELECTRON_RENDERER_URL']) {
    void mainWindow.loadURL(process.env['ELECTRON_RENDERER_URL']).catch((error) => {
      log.error('Failed to load renderer URL', error)
    })
  } else {
    void mainWindow.loadFile(join(__dirname, '../renderer/index.html')).catch((error) => {
      log.error('Failed to load renderer file', error)
    })
  }
}

app.whenReady().then(async () => {
  nativeTheme.themeSource = 'system'
  electronApp.setAppUserModelId('com.orchi.app')

  app.on('browser-window-created', (_, window) => {
    optimizer.watchWindowShortcuts(window)
  })

  ipcMain.handle(
    'shell:openInEditor',
    async (
      _event,
      folderPath: string,
      editor: EditorId,
      location?: { relativePath: string; line: number; column?: number }
    ) => {
      return openInEditor(folderPath, editor, undefined, { location })
    }
  )

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

  ipcMain.handle('logs:getPath', () => getLogFilePath())

  ipcMain.handle('logs:openFolder', async () => {
    const logFile = getLogFilePath()
    shell.showItemInFolder(logFile)
    return { logFile, logDirectory: getLogDirectory() }
  })

  if (!is.dev) {
    try {
      await startApiHost()
    } catch (error) {
      const message = error instanceof Error ? error.message : String(error)
      log.error('API host failed to start', error)
      await dialog.showErrorBox('Orchi failed to start', message)
      app.quit()
      return
    }
  }

  createWindow()
  log.info('Main window created')

  app.on('activate', function () {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow()
    }
  })
})

app.on('before-quit', (event) => {
  handleBeforeQuit(
    event,
    shutdownState,
    createDefaultShutdownDeps({
      isDev: is.dev,
      getApiBaseUrl,
      stopApiHost
    }),
    () => app.quit()
  )
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit()
  }
})
