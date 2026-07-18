import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

import type {
  CreateScriptRequest,
  GitHostProviderInfo,
  GitHostReadiness,
  Script,
  UpdateScriptRequest
} from './types'

export async function listScripts(projectId?: string | null): Promise<Script[]> {
  const params = new URLSearchParams()
  if (projectId) {
    params.set('projectId', projectId)
  }

  const query = params.toString()
  const response = await fetch(`${getApiBaseUrl()}/scripts${query ? `?${query}` : ''}`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as Script[]
}

export async function createScript(request: CreateScriptRequest): Promise<Script> {
  const response = await fetch(`${getApiBaseUrl()}/scripts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as Script
}

export async function updateScript(id: string, request: UpdateScriptRequest): Promise<Script> {
  const response = await fetch(`${getApiBaseUrl()}/scripts/${id}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as Script
}

export async function deleteScript(id: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/scripts/${id}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}

export async function applyOrchestrationGitDefaults(projectId?: string | null): Promise<Script[]> {
  const response = await fetch(`${getApiBaseUrl()}/scripts/templates/orchestration-git-defaults`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ projectId: projectId ?? null })
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as Script[]
}

export async function listGitHostProviders(): Promise<GitHostProviderInfo[]> {
  const response = await fetch(`${getApiBaseUrl()}/git/hosting/providers`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as GitHostProviderInfo[]
}

export async function getGitHostReadiness(options?: {
  projectId?: string
  workspacePath?: string
  provider?: string
}): Promise<GitHostReadiness> {
  const params = new URLSearchParams()
  if (options?.projectId) {
    params.set('projectId', options.projectId)
  }
  if (options?.workspacePath) {
    params.set('workspacePath', options.workspacePath)
  }
  if (options?.provider) {
    params.set('provider', options.provider)
  }

  const query = params.toString()
  const response = await fetch(
    `${getApiBaseUrl()}/git/hosting/readiness${query ? `?${query}` : ''}`
  )
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as GitHostReadiness
}
