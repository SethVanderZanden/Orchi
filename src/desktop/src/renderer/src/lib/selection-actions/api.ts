import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'
import type {
  CreateSelectionActionRequest,
  SelectionAction,
  UpdateSelectionActionRequest
} from '@/lib/selection-actions/types'

export async function listSelectionActions(): Promise<SelectionAction[]> {
  const response = await fetch(`${getApiBaseUrl()}/selection-actions`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as SelectionAction[]
}

export async function createSelectionAction(
  request: CreateSelectionActionRequest
): Promise<SelectionAction> {
  const response = await fetch(`${getApiBaseUrl()}/selection-actions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as SelectionAction
}

export async function updateSelectionAction(
  id: string,
  request: UpdateSelectionActionRequest
): Promise<SelectionAction> {
  const response = await fetch(`${getApiBaseUrl()}/selection-actions/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as SelectionAction
}

export async function deleteSelectionAction(id: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/selection-actions/${encodeURIComponent(id)}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}
