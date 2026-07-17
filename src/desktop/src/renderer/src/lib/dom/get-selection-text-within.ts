/** Returns the current selection text if it lies within `container`, otherwise empty. */
export function getSelectionTextWithin(container: Node): string {
  const selection = window.getSelection()
  if (!selection || selection.rangeCount === 0 || selection.isCollapsed) {
    return ''
  }

  const range = selection.getRangeAt(0)
  if (!container.contains(range.commonAncestorContainer)) {
    return ''
  }

  return selection
    .toString()
    .replace(/\u00a0/g, ' ')
    .trim()
}
