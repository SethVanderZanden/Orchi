const COMPLETE_PLAN_BLOCK_PATTERN =
  /<!--\s*orchi-plan:[a-z0-9]+(?:-[a-z0-9]+)*\s*-->\s*[\s\S]*?<!--\s*\/orchi-plan\s*-->/gi

const COMPLETE_PLAN_SEQUENCE_PATTERN =
  /<!--\s*orchi-plan-sequence\s*-->\s*[\s\S]*?<!--\s*\/orchi-plan-sequence\s*-->/gi

const OPEN_PLAN_MARKER_PATTERN = /<!--\s*orchi-plan:[a-z0-9]+(?:-[a-z0-9]+)*\s*-->/i
const OPEN_PLAN_SEQUENCE_MARKER_PATTERN = /<!--\s*orchi-plan-sequence\s*-->/i

/**
 * Removes orchestration plan / sequence blocks from assistant message text for chat display.
 * Plan content still lives in the stored message for parsing and kickoff; only the bubble hides it.
 * Incomplete open markers (mid-stream) are truncated so plan markdown never flashes in chat.
 */
export function stripPlanBlocksForChatDisplay(content: string): string {
  let stripped = content
    .replace(COMPLETE_PLAN_BLOCK_PATTERN, '')
    .replace(COMPLETE_PLAN_SEQUENCE_PATTERN, '')

  const openPlanIndex = stripped.search(OPEN_PLAN_MARKER_PATTERN)
  const openSequenceIndex = stripped.search(OPEN_PLAN_SEQUENCE_MARKER_PATTERN)

  const truncateAt = [openPlanIndex, openSequenceIndex]
    .filter((index) => index >= 0)
    .reduce((min, index) => Math.min(min, index), Number.POSITIVE_INFINITY)

  if (Number.isFinite(truncateAt)) {
    stripped = stripped.slice(0, truncateAt)
  }

  return stripped.replace(/\n{3,}/g, '\n\n').trim()
}
