import { resolve } from 'path'
import { defineConfig } from 'electron-vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { tanstackRouter } from '@tanstack/router-plugin/vite'

export default defineConfig(({ command }) => ({
  main: {
    resolve: {
      alias: {
        // Rollup cannot always resolve package.json export subpaths when bundling.
        'electron-log/main': resolve('node_modules/electron-log/main.js')
      }
    },
    build: {
      // Dev keeps deps external (fast rebuilds, uses node_modules). Production bundles
      // electron-log so packaged apps do not depend on node_modules layout.
      externalizeDeps:
        command === 'build'
          ? {
              exclude: ['electron-log']
            }
          : true
    }
  },
  preload: {},
  renderer: {
    resolve: {
      alias: {
        '@': resolve('src/renderer/src')
      }
    },
    worker: {
      format: 'es'
    },
    server: {
      proxy: {
        '/chats': {
          target: 'http://localhost:5265',
          changeOrigin: true
        },
        '/projects': {
          target: 'http://localhost:5265',
          changeOrigin: true
        },
        '/agents': {
          target: 'http://localhost:5265',
          changeOrigin: true
        },
        '/workspaces': {
          target: 'http://localhost:5265',
          changeOrigin: true
        },
        '/selection-actions': {
          target: 'http://localhost:5265',
          changeOrigin: true
        },
        '/user-preferences': {
          target: 'http://localhost:5265',
          changeOrigin: true
        }
      }
    },
    plugins: [
      tanstackRouter({
        target: 'react',
        routesDirectory: resolve('src/renderer/src/routes'),
        generatedRouteTree: resolve('src/renderer/src/routeTree.gen.ts'),
        autoCodeSplitting: true
      }),
      react(),
      tailwindcss()
    ]
  }
}))
