type ChatStatusListener = () => void

const listeners = new Set<ChatStatusListener>()

export function subscribeChatStatusListeners(listener: ChatStatusListener): () => void {
  listeners.add(listener)
  return () => {
    listeners.delete(listener)
  }
}

export function notifyChatStatusListeners(): void {
  listeners.forEach((listener) => listener())
}
