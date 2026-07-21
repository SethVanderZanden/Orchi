import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'

import { agentKeys, userPreferenceKeys } from '@/lib/query-keys'
import {
  DEFAULT_AUTO_KICK_OFF_REVIEW,
  DEFAULT_POST_MESSAGE_BEHAVIOR,
  getUserPreferences,
  updateUserPreferences
} from '@/lib/user-preferences/api'
import { needsAgentSetup } from '@/lib/user-preferences/enabled-agents'
import type {
  AgentSetupPreferences,
  PostMessageBehavior,
  UserPreferences
} from '@/lib/user-preferences/types'

export function useUserPreferences(): {
  preferences: UserPreferences | undefined
  postMessageBehavior: PostMessageBehavior
  enabledAgentIds: string[]
  autoKickOffReview: boolean
  needsAgentSetup: boolean
  isLoading: boolean
  setPostMessageBehavior: (behavior: PostMessageBehavior) => void
  setEnabledAgentIds: (
    agentIds: string[],
    setup?: AgentSetupPreferences
  ) => Promise<UserPreferences>
  setAutoKickOffReview: (enabled: boolean) => void
  isUpdating: boolean
} {
  const queryClient = useQueryClient()

  const query = useQuery({
    queryKey: userPreferenceKeys.detail(),
    queryFn: getUserPreferences,
    staleTime: 5 * 60 * 1000
  })

  const mutation = useMutation({
    mutationFn: updateUserPreferences,
    onSuccess: async (updated) => {
      queryClient.setQueryData(userPreferenceKeys.detail(), updated)
      await queryClient.invalidateQueries({ queryKey: agentKeys.modeDefaults() })
    }
  })

  const enabledAgentIds = query.data?.enabledAgentIds ?? []

  return {
    preferences: query.data,
    postMessageBehavior: query.data?.postMessageBehavior ?? DEFAULT_POST_MESSAGE_BEHAVIOR,
    enabledAgentIds,
    autoKickOffReview: query.data?.autoKickOffReview ?? DEFAULT_AUTO_KICK_OFF_REVIEW,
    needsAgentSetup: needsAgentSetup(query.data?.enabledAgentIds),
    isLoading: query.isLoading,
    setPostMessageBehavior: (behavior) => {
      mutation.mutate({ postMessageBehavior: behavior })
    },
    setEnabledAgentIds: (agentIds, setup) =>
      mutation.mutateAsync({
        enabledAgentIds: agentIds,
        codexApprovalPolicyId: setup?.codexApprovalPolicyId ?? undefined,
        codexReasoningEffortId: setup?.codexReasoningEffortId ?? undefined
      }),
    setAutoKickOffReview: (enabled) => {
      mutation.mutate({ autoKickOffReview: enabled })
    },
    isUpdating: mutation.isPending
  }
}
