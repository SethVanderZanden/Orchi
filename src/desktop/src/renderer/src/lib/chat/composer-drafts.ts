const draftsByChatId = new Map<string, string>()

type ComposerDraftListener = () => void

const listeners = new Set<ComposerDraftListener>()

function notifyComposerDraftChange(): void {
  for (const listener of listeners) {
    listener()
  }
}

export function subscribeComposerDrafts(listener: ComposerDraftListener): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function getComposerDraft(chatId: string): string | undefined {
  return draftsByChatId.get(chatId)
}

export function hasComposerDraft(chatId: string): boolean {
  return draftsByChatId.has(chatId)
}

export function clearComposerDraft(chatId: string): void {
  if (!draftsByChatId.delete(chatId)) {
    return
  }

  notifyComposerDraftChange()
}

export function setComposerDraft(chatId: string, draft: string): void {
  const trimmed = draft.trim()
  if (!trimmed) {
    clearComposerDraft(chatId)
    return
  }

  const next = draft.trimEnd()
  if (draftsByChatId.get(chatId) === next) {
    return
  }

  draftsByChatId.set(chatId, next)
  notifyComposerDraftChange()
}

/** Reads and clears a one-shot composer draft for a chat. */
export function takeComposerDraft(chatId: string): string | undefined {
  const draft = draftsByChatId.get(chatId)
  if (draft === undefined) {
    return undefined
  }

  clearComposerDraft(chatId)
  return draft
}

export function migrateComposerDraft(fromId: string, toId: string): void {
  const draft = draftsByChatId.get(fromId)
  if (draft === undefined) {
    return
  }

  draftsByChatId.delete(fromId)
  draftsByChatId.set(toId, draft)
  notifyComposerDraftChange()
}
