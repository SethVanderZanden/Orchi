export type MarkedBlock = {
  id: string
  body: string
}

export type MarkedBlockParseConfig = {
  completeBlockPattern: RegExp
  openMarkerPattern: RegExp
}

/**
 * Extracts marked blocks (plan / review-plan) from assistant output.
 * Supports complete blocks (open + close markers) and incomplete blocks
 * (open marker without a close tag, e.g. while streaming).
 */
export function parseMarkedBlocks(
  content: string,
  { completeBlockPattern, openMarkerPattern }: MarkedBlockParseConfig
): MarkedBlock[] {
  const blocks = new Map<string, string>()

  for (const match of content.matchAll(completeBlockPattern)) {
    const id = match[1]
    const body = match[2]?.trim()
    if (id && body) {
      blocks.set(id, body)
    }
  }

  const opens: Array<{ id: string; markerStart: number; contentStart: number }> = []
  for (const match of content.matchAll(openMarkerPattern)) {
    if (match.index === undefined || !match[1]) {
      continue
    }

    opens.push({
      id: match[1],
      markerStart: match.index,
      contentStart: match.index + match[0].length
    })
  }

  for (let index = 0; index < opens.length; index += 1) {
    const open = opens[index]
    if (blocks.has(open.id)) {
      continue
    }

    const sliceEnd = index + 1 < opens.length ? opens[index + 1].markerStart : content.length
    const body = content.slice(open.contentStart, sliceEnd).trim()
    if (!body) {
      continue
    }

    blocks.set(open.id, body)
  }

  return [...blocks.entries()].map(([id, body]) => ({ id, body }))
}
