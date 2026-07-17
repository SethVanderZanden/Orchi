type ChatDeletedListener = (chatId: string) => void

const listeners = new Set<ChatDeletedListener>()

export function notifyChatDeleted(chatId: string): void {
  for (const listener of listeners) {
    listener(chatId)
  }
}

export function onChatDeleted(listener: ChatDeletedListener): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}
