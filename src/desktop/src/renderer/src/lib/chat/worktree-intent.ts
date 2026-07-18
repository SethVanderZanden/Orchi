export type WorktreeIntent = {
  enabled: boolean
  branchName: string
}

const intentsByChatId = new Map<string, WorktreeIntent>()

type WorktreeIntentListener = () => void

const listeners = new Set<WorktreeIntentListener>()

function notifyWorktreeIntentChange(): void {
  for (const listener of listeners) {
    listener()
  }
}

export function subscribeWorktreeIntents(listener: WorktreeIntentListener): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function getWorktreeIntent(chatId: string): WorktreeIntent | undefined {
  return intentsByChatId.get(chatId)
}

export function isWorktreeIntentEnabled(chatId: string): boolean {
  return intentsByChatId.get(chatId)?.enabled === true
}

export function clearWorktreeIntent(chatId: string): void {
  if (!intentsByChatId.delete(chatId)) {
    return
  }

  notifyWorktreeIntentChange()
}

export function setWorktreeIntent(chatId: string, intent: WorktreeIntent): void {
  if (!intent.enabled && !intent.branchName.trim()) {
    clearWorktreeIntent(chatId)
    return
  }

  const next: WorktreeIntent = {
    enabled: intent.enabled,
    branchName: intent.branchName
  }
  const current = intentsByChatId.get(chatId)
  if (current?.enabled === next.enabled && current.branchName === next.branchName) {
    return
  }

  intentsByChatId.set(chatId, next)
  notifyWorktreeIntentChange()
}

export function setWorktreeIntentEnabled(chatId: string, enabled: boolean): void {
  const current = intentsByChatId.get(chatId)
  setWorktreeIntent(chatId, {
    enabled,
    branchName: current?.branchName ?? ''
  })
}

export function setWorktreeIntentBranchName(chatId: string, branchName: string): void {
  const current = intentsByChatId.get(chatId)
  if (!current?.enabled) {
    return
  }

  setWorktreeIntent(chatId, { enabled: true, branchName })
}

export function toggleWorktreeIntent(chatId: string): void {
  setWorktreeIntentEnabled(chatId, !isWorktreeIntentEnabled(chatId))
}

export function migrateWorktreeIntent(fromId: string, toId: string): void {
  const intent = intentsByChatId.get(fromId)
  if (intent === undefined) {
    return
  }

  intentsByChatId.delete(fromId)
  intentsByChatId.set(toId, intent)
  notifyWorktreeIntentChange()
}

/** True when the composer worktree toggle should be shown (new/empty chat). */
export function canUseWorktreeToggle(messageCount: number): boolean {
  return messageCount === 0
}
