import { useArchiveChatContext, type ArchiveChatContextValue } from '@/providers/archive-chat-context'

export function useArchiveChat(): ArchiveChatContextValue {
  return useArchiveChatContext()
}
