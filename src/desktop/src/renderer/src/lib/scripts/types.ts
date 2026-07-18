export type ScriptEvent = 'agentStart' | 'agentFinish'

export type ScriptOnError = 'continue' | 'abortTurn'

export type ScriptBinding = {
  id: string
  scriptId: string
  event: ScriptEvent
  modeFilter: string | null
  order: number
  enabled: boolean
  onError: ScriptOnError
}

export type Script = {
  id: string
  name: string
  projectId: string | null
  stepsJson: string
  createdAt: string
  updatedAt: string
  bindings: ScriptBinding[]
}

export type ScriptBindingRequest = {
  event: ScriptEvent
  modeFilter?: string | null
  order?: number
  enabled?: boolean
  onError?: ScriptOnError
}

export type CreateScriptRequest = {
  name: string
  projectId?: string | null
  stepsJson: string
  bindings?: ScriptBindingRequest[]
}

export type UpdateScriptRequest = {
  name: string
  stepsJson: string
  bindings?: ScriptBindingRequest[]
}

export type GitHostReadiness = {
  providerId: string
  status: 'ready' | 'missingCli' | 'notAuthenticated' | 'repoNotDetected' | string
  message: string
  requiredCli: string | null
}

export type GitHostProviderInfo = {
  providerId: string
  displayName: string
  requiredCli: string
  configureHint: string
}
