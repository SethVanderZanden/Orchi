/**
 * Opens up dense agent narration for scanning.
 *
 * Soft line breaks become paragraph breaks so each status beat gets air,
 * while fenced code, tables, and consecutive list items stay intact.
 */
export function spreadAssistantMarkdown(markdown: string): string {
  if (!markdown.includes('\n')) {
    return markdown
  }

  const segments = markdown.split(/(```[\s\S]*?```)/g)

  return segments
    .map((segment) => {
      if (segment.startsWith('```')) {
        return segment
      }

      return spreadProseSegment(segment)
    })
    .join('')
}

function spreadProseSegment(segment: string): string {
  const lines = segment.split('\n')
  const out: string[] = []

  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index]
    const next = lines[index + 1]
    out.push(line)

    if (next === undefined) {
      continue
    }

    if (line.trim() === '' || next.trim() === '') {
      continue
    }

    if (isListItem(line) && isListItem(next)) {
      continue
    }

    if (isTableRow(line) && isTableRow(next)) {
      continue
    }

    out.push('')
  }

  return out.join('\n')
}

function isListItem(line: string): boolean {
  return /^\s*([-*+]|\d+\.)\s/.test(line)
}

function isTableRow(line: string): boolean {
  return /^\s*\|/.test(line)
}
