const COMPLETE_PLAN_BLOCK_PATTERN =
  /<!--\s*orchi-plan:[a-z0-9]+(?:-[a-z0-9]+)*\s*-->\s*[\s\S]*?<!--\s*\/orchi-plan\s*-->/gi

const COMPLETE_PLAN_SEQUENCE_PATTERN =
  /<!--\s*orchi-plan-sequence\s*-->\s*[\s\S]*?<!--\s*\/orchi-plan-sequence\s*-->/gi

const COMPLETE_REVIEW_PLAN_BLOCK_PATTERN =
  /<!--\s*orchi-review-plan:[a-z0-9]+(?:-[a-z0-9]+)*\s*-->\s*[\s\S]*?<!--\s*\/orchi-review-plan\s*-->/gi

const OPEN_PLAN_MARKER_PATTERN = /<!--\s*orchi-plan:[a-z0-9]+(?:-[a-z0-9]+)*\s*-->/i
const OPEN_PLAN_SEQUENCE_MARKER_PATTERN = /<!--\s*orchi-plan-sequence\s*-->/i
const OPEN_REVIEW_PLAN_MARKER_PATTERN = /<!--\s*orchi-review-plan:[a-z0-9]+(?:-[a-z0-9]+)*\s*-->/i

function stripMarkedBlocksForChatDisplay(
  content: string,
  options: {
    completePatterns: RegExp[]
    openMarkerPatterns: RegExp[]
  }
): string {
  let stripped = content
  for (const pattern of options.completePatterns) {
    stripped = stripped.replace(pattern, '')
  }

  const truncateAt = options.openMarkerPatterns
    .map((pattern) => stripped.search(pattern))
    .filter((index) => index >= 0)
    .reduce((min, index) => Math.min(min, index), Number.POSITIVE_INFINITY)

  if (Number.isFinite(truncateAt)) {
    stripped = stripped.slice(0, truncateAt)
  }

  return stripped.replace(/\n{3,}/g, '\n\n').trim()
}

/**
 * Removes orchestration plan / sequence blocks from assistant message text for chat display.
 * Plan content still lives in the stored message for parsing and kickoff; only the bubble hides it.
 * Incomplete open markers (mid-stream) are truncated so plan markdown never flashes in chat.
 */
export function stripPlanBlocksForChatDisplay(content: string): string {
  return stripMarkedBlocksForChatDisplay(content, {
    completePatterns: [COMPLETE_PLAN_BLOCK_PATTERN, COMPLETE_PLAN_SEQUENCE_PATTERN],
    openMarkerPatterns: [OPEN_PLAN_MARKER_PATTERN, OPEN_PLAN_SEQUENCE_MARKER_PATTERN]
  })
}

/**
 * Removes review plan blocks from assistant message text for chat display.
 * Incomplete open markers (mid-stream) are truncated so review markdown never flashes in chat.
 */
export function stripReviewPlanBlocksForChatDisplay(content: string): string {
  return stripMarkedBlocksForChatDisplay(content, {
    completePatterns: [COMPLETE_REVIEW_PLAN_BLOCK_PATTERN],
    openMarkerPatterns: [OPEN_REVIEW_PLAN_MARKER_PATTERN]
  })
}
