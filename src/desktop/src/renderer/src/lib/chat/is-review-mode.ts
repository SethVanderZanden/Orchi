import type { AgentMode } from '@/lib/chat/types'

/** Work-conducted review (`review`) or PR/branch review (`branch-review`). */
export function isReviewFamilyMode(mode: AgentMode | null | undefined): boolean {
  if (!mode) {
    return false
  }

  const normalized = mode.toLowerCase()
  return normalized === 'review' || normalized === 'branch-review'
}
