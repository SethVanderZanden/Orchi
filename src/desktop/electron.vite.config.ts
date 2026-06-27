import { resolve } from 'path'
import { defineConfig } from 'electron-vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import { tanstackRouter } from '@tanstack/router-plugin/vite'

export default defineConfig({
  main: {},
  preload: {},
  renderer: {
    resolve: {
      alias: {
        '@': resolve('src/renderer/src'),
        '@renderer': resolve('src/renderer/src')
      }
    },
    server: {
      proxy: {
        '/WeatherForecast': {
          target: 'http://localhost:5265',
          changeOrigin: true
        },
        '/chats': {
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
})
