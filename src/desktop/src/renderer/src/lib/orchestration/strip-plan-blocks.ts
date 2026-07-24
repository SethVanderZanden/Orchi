import {
  ORCHI_MARKERS,
  getIdBlockStripConfig,
  getSequenceBlockStripConfig,
  mergeStripConfigs
} from '@/lib/orchestration/orchi-markers'
import { stripMarkedBlocksForChatDisplay } from '@/lib/orchestration/parse-marked-blocks'

const ORCHESTRATION_STRIP_CONFIG = mergeStripConfigs(
  getIdBlockStripConfig(ORCHI_MARKERS.plan),
  getSequenceBlockStripConfig(ORCHI_MARKERS.planSequence)
)

const REVIEW_STRIP_CONFIG = getIdBlockStripConfig(ORCHI_MARKERS.reviewPlan)

/**
 * Removes orchestration plan / sequence blocks from assistant message text for chat display.
 * Plan content still lives in the stored message for parsing and kickoff; only the bubble hides it.
 */
export function stripPlanBlocksForChatDisplay(content: string): string {
  return stripMarkedBlocksForChatDisplay(content, ORCHESTRATION_STRIP_CONFIG)
}

/**
 * Removes review plan blocks from assistant message text for chat display.
 */
export function stripReviewPlanBlocksForChatDisplay(content: string): string {
  return stripMarkedBlocksForChatDisplay(content, REVIEW_STRIP_CONFIG)
}
