import type { ChatThread } from '@/lib/chat/types'

export type ChatTreeNode = {
  chat: ChatThread
  children: ChatThread[]
}

const PLAN_FILE_PATH_PATTERN = /(?:^|[\\/])plan-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$/i
const REVIEW_FILE_PATH_PATTERN = /(?:^|[\\/])review-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$/i

export function planIdFromPlanFilePath(path: string | null): string | null {
  if (!path) {
    return null
  }

  const normalized = path.replace(/\\/g, '/')
  const match = PLAN_FILE_PATH_PATTERN.exec(normalized)
  return match?.[1] ?? null
}

export function formatPlanIdAsTitle(planId: string): string {
  const words = planId.split('-').filter(Boolean)
  if (words.length === 0) {
    return planId
  }

  return words
    .map((word, index) =>
      index === 0 ? word.charAt(0).toUpperCase() + word.slice(1) : word
    )
    .join(' ')
}

export function findChildForPlan(
  planId: string,
  children: ChatThread[]
): ChatThread | undefined {
  return children.find(
    (child) => planIdFromPlanFilePath(child.planFilePath) === planId
  )
}

export function findReviewChildForPlan(
  planId: string,
  children: ChatThread[]
): ChatThread | undefined {
  return children.find(
    (child) => reviewPlanIdFromPlanFilePath(child.planFilePath) === planId
  )
}

export function reviewPlanIdFromPlanFilePath(path: string | null): string | null {
  if (!path) {
    return null
  }

  const normalized = path.replace(/\\/g, '/')
  const match = REVIEW_FILE_PATH_PATTERN.exec(normalized)
  return match?.[1] ?? null
}

export function isImplementationChildChat(chat: ChatThread): boolean {
  if (!chat.parentChatId || !chat.planFilePath) {
    return false
  }

  return (
    (chat.mode === 'default' || chat.mode === 'implementation') &&
    planIdFromPlanFilePath(chat.planFilePath) !== null
  )
}

export function isReviewChildChat(chat: ChatThread): boolean {
  if (!chat.parentChatId) {
    return false
  }

  return chat.mode === 'review' || reviewPlanIdFromPlanFilePath(chat.planFilePath) !== null
}

function sortChatsByUpdatedAtDesc(chats: ChatThread[]): ChatThread[] {
  return [...chats].sort(
    (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
  )
}

export function buildChatTree(chats: ChatThread[]): ChatTreeNode[] {
  const chatById = new Map(chats.map((chat) => [chat.id, chat]))
  const childrenByParentId = new Map<string, ChatThread[]>()

  for (const chat of chats) {
    if (!chat.parentChatId) {
      continue
    }

    const siblings = childrenByParentId.get(chat.parentChatId) ?? []
    siblings.push(chat)
    childrenByParentId.set(chat.parentChatId, siblings)
  }

  const roots: ChatThread[] = []

  for (const chat of chats) {
    if (!chat.parentChatId) {
      roots.push(chat)
      continue
    }

    if (!chatById.has(chat.parentChatId)) {
      roots.push(chat)
    }
  }

  return sortChatsByUpdatedAtDesc(roots).map((chat) => ({
    chat,
    children: sortChatsByUpdatedAtDesc(childrenByParentId.get(chat.id) ?? [])
  }))
}

export function matchesChatQuery(chat: ChatThread, query: string): boolean {
  const normalized = query.trim().toLowerCase()
  if (!normalized) {
    return true
  }

  return (
    chat.title.toLowerCase().includes(normalized) ||
    chat.preview.toLowerCase().includes(normalized)
  )
}

export function filterChatTreeNodes(nodes: ChatTreeNode[], query: string): ChatTreeNode[] {
  const normalized = query.trim().toLowerCase()
  if (!normalized) {
    return nodes
  }

  const filtered: ChatTreeNode[] = []

  for (const node of nodes) {
    const rootMatches = matchesChatQuery(node.chat, normalized)
    const matchingChildren = node.children.filter((child) =>
      matchesChatQuery(child, normalized)
    )

    if (rootMatches) {
      filtered.push(node)
      continue
    }

    if (matchingChildren.length > 0) {
      filtered.push({ chat: node.chat, children: matchingChildren })
    }
  }

  return filtered
}
