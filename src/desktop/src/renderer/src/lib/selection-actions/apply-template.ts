import { SELECTED_TEXT_PLACEHOLDER } from '@/lib/selection-actions/types'

export function containsSelectedTextPlaceholder(template: string): boolean {
  return /\{\{\s*selected text\s*\}\}/i.test(template)
}

/** Replaces `{{selected text}}` (any spacing/case) with the given selection. */
export function applySelectionTemplate(template: string, selectedText: string): string {
  return template.replace(/\{\{\s*selected text\s*\}\}/gi, selectedText)
}

export function defaultSelectionTemplate(): string {
  return `Explain {{selected text}} clearly with a short example.`
}

export { SELECTED_TEXT_PLACEHOLDER }
