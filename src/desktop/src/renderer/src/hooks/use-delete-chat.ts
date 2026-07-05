import { useDeleteChatContext, type DeleteChatContextValue } from '@/providers/delete-chat-context'

export function useDeleteChat(): DeleteChatContextValue {
  return useDeleteChatContext()
}
