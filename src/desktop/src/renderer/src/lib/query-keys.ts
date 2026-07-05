export const chatKeys = {
  all: ['chats'] as const,
  lists: () => [...chatKeys.all, 'list'] as const,
  detail: (chatId: string) => [...chatKeys.all, 'detail', chatId] as const
}

export const projectKeys = {
  all: ['projects'] as const,
  lists: () => [...projectKeys.all, 'list'] as const,
  detail: (projectId: string) => [...projectKeys.all, 'detail', projectId] as const
}

export const agentKeys = {
  all: ['agents'] as const,
  modes: () => [...agentKeys.all, 'modes'] as const,
  /** Prefix for all model queries for an agent (any includeDisabled flag). Use for invalidation. */
  modelsForAgent: (agentId: string) => [...agentKeys.all, 'models', agentId] as const,
  models: (agentId: string, includeDisabled = false) =>
    [...agentKeys.all, 'models', agentId, { includeDisabled }] as const,
  modeModelDefaults: (agentId: string) =>
    [...agentKeys.all, 'mode-model-defaults', agentId] as const
}
