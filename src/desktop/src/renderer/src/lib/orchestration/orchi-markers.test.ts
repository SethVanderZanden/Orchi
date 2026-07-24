import { describe, expect, it } from 'vitest'

import {
  ORCHI_MARKERS,
  extractMarkdownTitle,
  getIdBlockParseConfig,
  getIdBlockStripConfig,
  getSequenceBlockParsePattern,
  getSequenceBlockStripConfig,
  mergeStripConfigs
} from './orchi-markers'
import { parseMarkedBlocks, stripMarkedBlocksForChatDisplay } from './parse-marked-blocks'

describe('orchi-markers', () => {
  it('builds matching parse and strip configs for id-bearing blocks', () => {
    const content = `Intro.

<!-- orchi-plan:auth-refactor -->
# Auth Refactor
<!-- /orchi-plan -->

Done.`

    const parsed = parseMarkedBlocks(content, getIdBlockParseConfig(ORCHI_MARKERS.plan))
    const stripped = stripMarkedBlocksForChatDisplay(
      content,
      getIdBlockStripConfig(ORCHI_MARKERS.plan)
    )

    expect(parsed).toHaveLength(1)
    expect(parsed[0]?.id).toBe('auth-refactor')
    expect(stripped).toBe('Intro.\n\nDone.')
  })

  it('builds sequence parse and strip configs', () => {
    const content = `<!-- orchi-plan-sequence -->
auth-refactor
ui-polish
<!-- /orchi-plan-sequence -->`

    const pattern = getSequenceBlockParsePattern()
    const match = [...content.matchAll(pattern)][0]
    const stripped = stripMarkedBlocksForChatDisplay(
      content,
      getSequenceBlockStripConfig(ORCHI_MARKERS.planSequence)
    )

    expect(match?.[1]?.trim()).toBe('auth-refactor\nui-polish')
    expect(stripped).toBe('')
  })

  it('merges strip configs for orchestration display', () => {
    const content = `Intro.

<!-- orchi-plan:auth-refactor -->
# Auth
<!-- /orchi-plan -->

<!-- orchi-plan-sequence -->
auth-refactor
<!-- /orchi-plan-sequence -->

Done.`

    const stripped = stripMarkedBlocksForChatDisplay(
      content,
      mergeStripConfigs(
        getIdBlockStripConfig(ORCHI_MARKERS.plan),
        getSequenceBlockStripConfig(ORCHI_MARKERS.planSequence)
      )
    )

    expect(stripped).toBe('Intro.\n\nDone.')
  })

  it('extracts markdown titles with fallback', () => {
    expect(extractMarkdownTitle('# Auth Refactor\n\nBody', 'Untitled plan')).toBe('Auth Refactor')
    expect(extractMarkdownTitle('No heading', 'Untitled plan')).toBe('Untitled plan')
  })
})
