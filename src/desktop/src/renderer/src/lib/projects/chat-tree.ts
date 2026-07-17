import type { ChatThread } from '@/lib/chat/types'

export type ChatTreeNode = {
  chat: ChatThread
  children: ChatTreeNode[]
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
    .map((word, index) => (index === 0 ? word.charAt(0).toUpperCase() + word.slice(1) : word))
    .join(' ')
}

export function findChildForPlan(planId: string, children: ChatThread[]): ChatThread | undefined {
  return children.find((child) => planIdFromPlanFilePath(child.planFilePath) === planId)
}

export function findReviewChildForPlan(
  planId: string,
  children: ChatThread[]
): ChatThread | undefined {
  return children.find((child) => reviewPlanIdFromPlanFilePath(child.planFilePath) === planId)
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

function sortNodesByUpdatedAtDesc(nodes: ChatTreeNode[]): ChatTreeNode[] {
  return [...nodes].sort(
    (a, b) => new Date(b.chat.updatedAt).getTime() - new Date(a.chat.updatedAt).getTime()
  )
}

function toLeafNode(chat: ChatThread): ChatTreeNode {
  return { chat, children: [] }
}

/**
 * Nest review chats under matching implementation siblings by plan id.
 * Unmatched reviews and other children stay as direct siblings.
 */
function nestDirectChildren(directChildren: ChatThread[]): ChatTreeNode[] {
  const implementationChildren: ChatThread[] = []
  const reviewChildren: ChatThread[] = []
  const otherChildren: ChatThread[] = []

  for (const child of directChildren) {
    if (isImplementationChildChat(child)) {
      implementationChildren.push(child)
      continue
    }

    if (isReviewChildChat(child)) {
      reviewChildren.push(child)
      continue
    }

    otherChildren.push(child)
  }

  const matchedReviewIds = new Set<string>()
  const implementationNodes = sortChatsByUpdatedAtDesc(implementationChildren).map((impl) => {
    const planId = planIdFromPlanFilePath(impl.planFilePath)
    const review = planId ? findReviewChildForPlan(planId, reviewChildren) : undefined
    if (review) {
      matchedReviewIds.add(review.id)
    }

    return {
      chat: impl,
      children: review ? [toLeafNode(review)] : []
    }
  })

  const unmatchedReviewNodes = sortChatsByUpdatedAtDesc(
    reviewChildren.filter((review) => !matchedReviewIds.has(review.id))
  ).map(toLeafNode)

  const otherNodes = sortChatsByUpdatedAtDesc(otherChildren).map(toLeafNode)

  return sortNodesByUpdatedAtDesc([...implementationNodes, ...unmatchedReviewNodes, ...otherNodes])
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
    children: nestDirectChildren(childrenByParentId.get(chat.id) ?? [])
  }))
}

export function matchesChatQuery(chat: ChatThread, query: string): boolean {
  const normalized = query.trim().toLowerCase()
  if (!normalized) {
    return true
  }

  return (
    chat.title.toLowerCase().includes(normalized) || chat.preview.toLowerCase().includes(normalized)
  )
}

export function filterChatTreeNodes(nodes: ChatTreeNode[], query: string): ChatTreeNode[] {
  const normalized = query.trim().toLowerCase()
  if (!normalized) {
    return nodes
  }

  const filtered: ChatTreeNode[] = []

  for (const node of nodes) {
    if (matchesChatQuery(node.chat, normalized)) {
      filtered.push(node)
      continue
    }

    const matchingChildren = filterChatTreeNodes(node.children, normalized)
    if (matchingChildren.length > 0) {
      filtered.push({ chat: node.chat, children: matchingChildren })
    }
  }

  return filtered
}

export function chatTreeContainsChat(node: ChatTreeNode, chatId: string): boolean {
  if (node.chat.id === chatId) {
    return true
  }

  return node.children.some((child) => chatTreeContainsChat(child, chatId))
}

/** Ancestor chat ids from root toward the target (excludes the target itself). */
export function findAncestorIdsInChatTree(nodes: ChatTreeNode[], chatId: string): string[] | null {
  for (const node of nodes) {
    if (node.chat.id === chatId) {
      return []
    }

    const nested = findAncestorIdsInChatTree(node.children, chatId)
    if (nested !== null) {
      return [node.chat.id, ...nested]
    }
  }

  return null
}

export function collectChatTreeNodes(nodes: ChatTreeNode[]): ChatTreeNode[] {
  const collected: ChatTreeNode[] = []

  for (const node of nodes) {
    collected.push(node)
    if (node.children.length > 0) {
      collected.push(...collectChatTreeNodes(node.children))
    }
  }

  return collected
}
