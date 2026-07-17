export const chatKeys = {
  all: ['chats'] as const,
  lists: () => [...chatKeys.all, 'list'] as const,
  search: (q: string, limit?: number) => [...chatKeys.all, 'search', { q, limit }] as const,
  detail: (chatId: string) => [...chatKeys.all, 'detail', chatId] as const
}

export const projectKeys = {
  all: ['projects'] as const,
  lists: () => [...projectKeys.all, 'list'] as const,
  detail: (projectId: string) => [...projectKeys.all, 'detail', projectId] as const
}

export const agentKeys = {
  all: ['agents'] as const,
  list: () => [...agentKeys.all, 'list'] as const,
  modes: () => [...agentKeys.all, 'modes'] as const,
  /** Prefix for all model queries for an agent (any includeDisabled flag). Use for invalidation. */
  modelsForAgent: (agentId: string) => [...agentKeys.all, 'models', agentId] as const,
  models: (agentId: string, includeDisabled = false) =>
    [...agentKeys.all, 'models', agentId, { includeDisabled }] as const,
  contextSizesForAgent: (agentId: string) => [...agentKeys.all, 'context-sizes', agentId] as const,
  contextSizes: (agentId: string, includeDisabled = false) =>
    [...agentKeys.all, 'context-sizes', agentId, { includeDisabled }] as const,
  cliOptionsForAgent: (agentId: string) => [...agentKeys.all, 'cli-options', agentId] as const,
  cliOptions: (agentId: string, kind: string, includeDisabled = false) =>
    [...agentKeys.all, 'cli-options', agentId, kind, { includeDisabled }] as const,
  modeDefaults: () => [...agentKeys.all, 'mode-defaults'] as const
}

export const selectionActionKeys = {
  all: ['selection-actions'] as const,
  lists: () => [...selectionActionKeys.all, 'list'] as const
}

export const userPreferenceKeys = {
  all: ['user-preferences'] as const,
  detail: () => [...userPreferenceKeys.all, 'detail'] as const
}
