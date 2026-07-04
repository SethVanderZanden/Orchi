const CHAT_READ_STATE_KEY = 'orchi.chatReadState.v1'

type ChatReadEntry = {
  updatedAt: string
}

type ChatReadState = Record<string, ChatReadEntry>

function readChatReadState(): ChatReadState {
  try {
    const raw = localStorage.getItem(CHAT_READ_STATE_KEY)
    if (!raw) {
      return {}
    }

    const parsed = JSON.parse(raw) as unknown
    if (!parsed || typeof parsed !== 'object') {
      return {}
    }

    const entries: ChatReadState = {}
    for (const [chatId, value] of Object.entries(parsed)) {
      if (
        typeof value === 'object' &&
        value !== null &&
        'updatedAt' in value &&
        typeof value.updatedAt === 'string'
      ) {
        entries[chatId] = { updatedAt: value.updatedAt }
      }
    }

    return entries
  } catch {
    return {}
  }
}

function writeChatReadState(state: ChatReadState): void {
  try {
    localStorage.setItem(CHAT_READ_STATE_KEY, JSON.stringify(state))
  } catch {
    // ignore persistence errors
  }
}

export function getLastReadUpdatedAt(chatId: string): string | undefined {
  return readChatReadState()[chatId]?.updatedAt
}

export function markChatRead(chatId: string, updatedAt: string): void {
  const state = readChatReadState()
  state[chatId] = { updatedAt }
  writeChatReadState(state)
}

export function isChatUnread(
  chat: { id: string; updatedAt: string },
  activeChatId: string | undefined
): boolean {
  if (chat.id === activeChatId) {
    return false
  }

  const lastRead = getLastReadUpdatedAt(chat.id)
  if (!lastRead) {
    return false
  }

  return chat.updatedAt > lastRead
}
