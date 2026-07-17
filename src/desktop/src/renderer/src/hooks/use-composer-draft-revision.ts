import { useSyncExternalStore } from 'react'

import { subscribeComposerDrafts } from '@/lib/chat/composer-drafts'

function getComposerDraftSnapshot(): number {
  return 0
}

/** Re-renders when any composer draft is stored or cleared. */
export function useComposerDraftRevision(): void {
  useSyncExternalStore(subscribeComposerDrafts, getComposerDraftSnapshot, getComposerDraftSnapshot)
}
