export type PostMessageBehavior = 'stayOnChat' | 'goToBoard' | 'openNewChat'

export type AgentSetupPreferences = {
  codexApprovalPolicyId?: string | null
  codexReasoningEffortId?: string | null
}

export type UserPreferences = {
  postMessageBehavior: PostMessageBehavior
  enabledAgentIds: string[]
  autoKickOffReview: boolean
  updatedAt: string
}

export type UpdateUserPreferencesRequest = {
  postMessageBehavior?: PostMessageBehavior
  enabledAgentIds?: string[]
  autoKickOffReview?: boolean
  codexApprovalPolicyId?: string | null
  codexReasoningEffortId?: string | null
}
