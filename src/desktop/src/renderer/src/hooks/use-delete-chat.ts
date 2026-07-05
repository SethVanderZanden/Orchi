import { useDeleteChatContext } from '@/providers/delete-chat-provider'

export function useDeleteChat() {
  return useDeleteChatContext()
}
