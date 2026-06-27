export type WeatherForecast = {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string | null
}

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

export async function fetchWeatherForecast(): Promise<WeatherForecast[]> {
  const baseUrl = getApiBaseUrl()
  const url = baseUrl ? `${baseUrl}/WeatherForecast` : '/WeatherForecast'
  const res = await fetch(url)

  if (!res.ok) {
    throw new Error(`API error: ${res.status}`)
  }

  return res.json()
}
