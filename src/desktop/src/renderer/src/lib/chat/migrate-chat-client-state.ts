type ChatIdMigrator = (fromId: string, toId: string) => void

const migrators = new Set<ChatIdMigrator>()

export function registerChatIdMigrator(migrator: ChatIdMigrator): () => void {
  migrators.add(migrator)
  return () => {
    migrators.delete(migrator)
  }
}

export function migrateChatClientState(fromId: string, toId: string): void {
  for (const migrator of migrators) {
    migrator(fromId, toId)
  }
}
