export function isLocalChat(chatId: string): boolean {
  return chatId === '0' || chatId.startsWith('local:')
}

export function isPersistedChat(chatId: string): boolean {
  return !isLocalChat(chatId)
}
