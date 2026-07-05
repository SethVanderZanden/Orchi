export type OrchestrationPlanResponse = {
  planId: string
  title: string
  contentMarkdown: string
}

export type OrchestrationChildResponse = {
  planId: string
  chatId: string
  mode: string
  planFilePath: string | null
}

export type OrchestrationStateResponse = {
  status: string
  currentStep: number | null
  totalSteps: number | null
  currentPlanId: string | null
  sequencePlanIds: string[]
  plans: OrchestrationPlanResponse[]
  children: OrchestrationChildResponse[]
}

export type OrchestrationWorkflowProgress = {
  active: boolean
  currentStep: number
  totalSteps: number
  status: string
}

export function workflowProgressFromState(
  state: OrchestrationStateResponse
): OrchestrationWorkflowProgress | null {
  if (state.totalSteps === null || state.totalSteps === 0 || state.currentStep === null) {
    if (state.status === 'running') {
      return {
        active: true,
        currentStep: state.currentStep ?? 1,
        totalSteps: state.totalSteps ?? 1,
        status: state.status
      }
    }

    return null
  }

  return {
    active: state.status === 'running',
    currentStep: state.currentStep,
    totalSteps: state.totalSteps,
    status: state.status
  }
}
