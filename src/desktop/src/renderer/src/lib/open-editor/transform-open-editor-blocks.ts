import {
  formatOpenEditorLinkLabel,
  parseOpenEditorCommand
} from '@/lib/open-editor/parse-open-editor-command'

export const OPEN_EDITOR_LINK_PREFIX = 'orchi-open-editor:'

const OPEN_EDITOR_BLOCK_PATTERN =
  /<orchi-open-editor>\s*([\s\S]*?)\s*<\/orchi-open-editor>/gi

export function transformOpenEditorBlocksForDisplay(content: string): string {
  return content.replace(OPEN_EDITOR_BLOCK_PATTERN, (_match, command: string) => {
    const parsed = parseOpenEditorCommand(command)
    if (!parsed) {
      return command.trim()
    }

    const label = formatOpenEditorLinkLabel(parsed)
    const href = `${OPEN_EDITOR_LINK_PREFIX}${encodeURIComponent(command.trim())}`
    return `[${label}](${href})`
  })
}

export function isOpenEditorLinkHref(href: string | undefined): href is string {
  return typeof href === 'string' && href.startsWith(OPEN_EDITOR_LINK_PREFIX)
}

export function decodeOpenEditorLinkHref(href: string): string {
  return decodeURIComponent(href.slice(OPEN_EDITOR_LINK_PREFIX.length))
}
