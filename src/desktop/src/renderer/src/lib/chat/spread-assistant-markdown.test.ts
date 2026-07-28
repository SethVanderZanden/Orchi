import { describe, expect, it } from 'vitest'

import { spreadAssistantMarkdown } from './spread-assistant-markdown'

describe('spreadAssistantMarkdown', () => {
  it('turns soft line breaks into paragraph breaks', () => {
    expect(spreadAssistantMarkdown('First beat.\nSecond beat.\nThird beat.')).toBe(
      'First beat.\n\nSecond beat.\n\nThird beat.'
    )
  })

  it('leaves already-spaced paragraphs alone', () => {
    expect(spreadAssistantMarkdown('First beat.\n\nSecond beat.')).toBe(
      'First beat.\n\nSecond beat.'
    )
  })

  it('keeps consecutive list items tight', () => {
    expect(spreadAssistantMarkdown('Intro:\n- one\n- two\n- three')).toBe(
      'Intro:\n\n- one\n- two\n- three'
    )
  })

  it('preserves fenced code newlines', () => {
    const input = 'Before.\n```ts\nconst a = 1\nconst b = 2\n```\nAfter.'
    expect(spreadAssistantMarkdown(input)).toBe(
      'Before.\n\n```ts\nconst a = 1\nconst b = 2\n```\n\nAfter.'
    )
  })

  it('preserves markdown table rows', () => {
    const input = 'Stats:\n| A | B |\n| --- | --- |\n| 1 | 2 |'
    expect(spreadAssistantMarkdown(input)).toBe(
      'Stats:\n\n| A | B |\n| --- | --- |\n| 1 | 2 |'
    )
  })
})
