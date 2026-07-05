export function getApiBaseUrl(): string {
  const configured = import.meta.env.VITE_API_BASE_URL
  if (configured) {
    return configured.replace(/\/$/, '')
  }

  // Dev: relative URL hits the Vite proxy in electron.vite.config.ts
  if (import.meta.env.DEV) {
    return ''
  }

  return 'http://localhost:5265'
}
