import type {
  ModeRuntimeDefault,
  ModeRuntimeDefaultsListResponse,
  UpdateModeRuntimeDefaultRequest,
  UpdateModeRuntimeDefaultResponse
} from '@/lib/chat/types'
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

export async function listModeRuntimeDefaults(): Promise<ModeRuntimeDefaultsListResponse> {
  const response = await fetch(`${getApiBaseUrl()}/agents/mode-defaults`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as ModeRuntimeDefaultsListResponse
}

export async function updateModeRuntimeDefault(
  mode: string,
  request: UpdateModeRuntimeDefaultRequest
): Promise<UpdateModeRuntimeDefaultResponse> {
  const response = await fetch(
    `${getApiBaseUrl()}/agents/mode-defaults/${encodeURIComponent(mode)}`,
    {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    }
  )

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as UpdateModeRuntimeDefaultResponse
}

export function resolveModeRuntimeDefault(
  defaults: ModeRuntimeDefault[],
  mode: string
): ModeRuntimeDefault | null {
  const match = defaults.find((row) => row.mode.toLowerCase() === mode.toLowerCase())
  return match ?? null
}
