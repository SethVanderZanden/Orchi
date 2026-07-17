export type PostMessageBehavior = 'stayOnChat' | 'goToBoard' | 'openNewChat'

export type UserPreferences = {
  postMessageBehavior: PostMessageBehavior
  enabledAgentIds: string[]
  updatedAt: string
}

export type UpdateUserPreferencesRequest = {
  postMessageBehavior?: PostMessageBehavior
  enabledAgentIds?: string[]
}
