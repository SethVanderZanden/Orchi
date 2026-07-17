import { describe, expect, it } from 'vitest'

import { resolveModeRuntimeDefault } from '@/lib/chat/mode-runtime-defaults-api'
import type { ModeRuntimeDefault } from '@/lib/chat/types'

describe('resolveModeRuntimeDefault', () => {
  const defaults: ModeRuntimeDefault[] = [
    {
      mode: 'default',
      label: 'Agent',
      agentId: 'cursor',
      modelId: null,
      contextSizeId: null,
      reasoningEffortId: null,
      approvalPolicyId: null
    },
    {
      mode: 'orchestration',
      label: 'Orchestration',
      agentId: 'codex',
      modelId: 'gpt-5.4',
      contextSizeId: 'medium',
      reasoningEffortId: null,
      approvalPolicyId: null
    }
  ]

  it('resolves by mode id case-insensitively', () => {
    expect(resolveModeRuntimeDefault(defaults, 'Orchestration')).toEqual(defaults[1])
  })

  it('returns null when mode is missing', () => {
    expect(resolveModeRuntimeDefault(defaults, 'review')).toBeNull()
  })
})
