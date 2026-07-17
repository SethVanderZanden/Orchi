import '@fontsource-variable/dm-sans'
import '@fontsource/jetbrains-mono/400.css'
import '@fontsource/jetbrains-mono/500.css'
import './assets/main.css'

import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider, createHashHistory, createRouter } from '@tanstack/react-router'

import { routeTree } from './routeTree.gen'

const isFileProtocol = typeof window !== 'undefined' && window.location.protocol === 'file:'

const router = createRouter({
  routeTree,
  history: isFileProtocol ? createHashHistory() : undefined,
  origin: isFileProtocol ? 'http://localhost' : undefined
})

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router
  }
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>
)
