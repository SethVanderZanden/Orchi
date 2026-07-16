import { describe, expect, it } from 'vitest'

import { workflowProgressFromState, workflowProgressFromWorkflowEvent } from './orchestration-state'

describe('workflowProgressFromWorkflowEvent', () => {
  it('returns active progress when workflow is running without step totals', () => {
    expect(
      workflowProgressFromWorkflowEvent({
        status: 'running',
        currentStep: null,
        totalSteps: null,
        planId: null
      })
    ).toEqual({
      active: true,
      currentStep: 1,
      totalSteps: 1,
      status: 'running'
    })
  })

  it('returns null when workflow is idle without step totals', () => {
    expect(
      workflowProgressFromWorkflowEvent({
        status: 'idle',
        currentStep: null,
        totalSteps: null,
        planId: null
      })
    ).toBeNull()
  })

  it('returns step progress for sequential runs', () => {
    expect(
      workflowProgressFromWorkflowEvent({
        status: 'running',
        currentStep: 2,
        totalSteps: 3,
        planId: 'auth-refactor'
      })
    ).toEqual({
      active: true,
      currentStep: 2,
      totalSteps: 3,
      status: 'running'
    })
  })

  it('marks completed sequential runs as inactive', () => {
    expect(
      workflowProgressFromWorkflowEvent({
        status: 'completed',
        currentStep: 3,
        totalSteps: 3,
        planId: 'auth-refactor'
      })
    ).toEqual({
      active: false,
      currentStep: 3,
      totalSteps: 3,
      status: 'completed'
    })
  })
})

describe('workflowProgressFromState', () => {
  it('matches workflow event mapping for kick-off-all snapshots', () => {
    const state = {
      status: 'running',
      currentStep: 1,
      totalSteps: 2,
      currentPlanId: 'auth-refactor',
      sequencePlanIds: ['auth-refactor', 'api-layer'],
      plans: [],
      children: []
    }

    expect(workflowProgressFromState(state)).toEqual(
      workflowProgressFromWorkflowEvent({
        status: state.status,
        currentStep: state.currentStep,
        totalSteps: state.totalSteps,
        planId: state.currentPlanId
      })
    )
  })
})
