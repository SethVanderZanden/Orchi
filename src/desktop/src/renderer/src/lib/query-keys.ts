export const weatherKeys = {
  all: ['weather'] as const,
  forecast: () => [...weatherKeys.all, 'forecast'] as const
}

export const chatKeys = {
  all: ['chats'] as const,
  lists: () => [...chatKeys.all, 'list'] as const,
  detail: (chatId: string) => [...chatKeys.all, 'detail', chatId] as const
}
