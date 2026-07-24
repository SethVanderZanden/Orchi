import type { MarkedBlockParseConfig, MarkedBlockStripConfig } from '@/lib/orchestration/parse-marked-blocks'

const PLAN_ID = '[a-z0-9]+(?:-[a-z0-9]+)*'

export const ORCHI_MARKERS = {
  plan: {
    tag: 'orchi-plan',
    defaultTitle: 'Untitled plan'
  },
  reviewPlan: {
    tag: 'orchi-review-plan',
    defaultTitle: 'Untitled review plan'
  },
  planSequence: {
    tag: 'orchi-plan-sequence'
  }
} as const

export type OrchiIdMarker = typeof ORCHI_MARKERS.plan | typeof ORCHI_MARKERS.reviewPlan
export type OrchiSequenceMarker = typeof ORCHI_MARKERS.planSequence

function createIdBlockParseConfig(tag: string): MarkedBlockParseConfig {
  return {
    completeBlockPattern: new RegExp(
      `<!--\\s*${tag}:(${PLAN_ID})\\s*-->\\s*([\\s\\S]*?)<!--\\s*\\/${tag}\\s*-->`,
      'gi'
    ),
    openMarkerPattern: new RegExp(`<!--\\s*${tag}:(${PLAN_ID})\\s*-->`, 'gi')
  }
}

function createIdBlockStripPatterns(tag: string): MarkedBlockStripConfig {
  return {
    completePatterns: [
      new RegExp(
        `<!--\\s*${tag}:${PLAN_ID}\\s*-->\\s*[\\s\\S]*?<!--\\s*\\/${tag}\\s*-->`,
        'gi'
      )
    ],
    openMarkerPatterns: [new RegExp(`<!--\\s*${tag}:${PLAN_ID}\\s*-->`, 'i')]
  }
}

function createSequenceBlockParsePattern(tag: string): RegExp {
  return new RegExp(
    `<!--\\s*${tag}\\s*-->\\s*([\\s\\S]*?)<!--\\s*\\/${tag}\\s*-->`,
    'gi'
  )
}

function createSequenceBlockStripPatterns(tag: string): MarkedBlockStripConfig {
  return {
    completePatterns: [
      new RegExp(`<!--\\s*${tag}\\s*-->\\s*[\\s\\S]*?<!--\\s*\\/${tag}\\s*-->`, 'gi')
    ],
    openMarkerPatterns: [new RegExp(`<!--\\s*${tag}\\s*-->`, 'i')]
  }
}

export function getIdBlockParseConfig(marker: OrchiIdMarker): MarkedBlockParseConfig {
  return createIdBlockParseConfig(marker.tag)
}

export function getIdBlockStripConfig(marker: OrchiIdMarker): MarkedBlockStripConfig {
  return createIdBlockStripPatterns(marker.tag)
}

export function getSequenceBlockParsePattern(
  marker: OrchiSequenceMarker = ORCHI_MARKERS.planSequence
): RegExp {
  return createSequenceBlockParsePattern(marker.tag)
}

export function getSequenceBlockStripConfig(
  marker: OrchiSequenceMarker = ORCHI_MARKERS.planSequence
): MarkedBlockStripConfig {
  return createSequenceBlockStripPatterns(marker.tag)
}

export function mergeStripConfigs(...configs: MarkedBlockStripConfig[]): MarkedBlockStripConfig {
  return {
    completePatterns: configs.flatMap((config) => config.completePatterns),
    openMarkerPatterns: configs.flatMap((config) => config.openMarkerPatterns)
  }
}

export function extractMarkdownTitle(content: string, fallback: string): string {
  const headingMatch = content.match(/^#\s+(.+)$/m)
  return headingMatch?.[1]?.trim() ?? fallback
}

export const PLAN_ID_PATTERN = new RegExp(`^${PLAN_ID}$`)
