import { QueryClient } from '@tanstack/react-query'

/** Default staleTime for most queries (chat list/detail, projects). Override per query when needed. */
const DEFAULT_STALE_TIME_MS = 30_000

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: DEFAULT_STALE_TIME_MS,
      retry: 1
    }
  }
})

/**
 * Per-query staleTime conventions (set on individual useQuery calls):
 * - Chat list / detail — default 30s (inherits from queryClient)
 * - Agent modes — Infinity (static config; see chat-mode-dropdown)
 * - Agent models — 60_000 or ONE_HOUR_MS until settings mutation invalidates cache
 */
