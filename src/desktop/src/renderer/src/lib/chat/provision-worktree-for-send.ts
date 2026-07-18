import type { QueryClient } from '@tanstack/react-query'

import { updateChatWorkspace } from '@/lib/chat/api'
import { applyWorkspaceToChatCache } from '@/lib/chat/apply-workspace-to-chat-cache'
import type { ChatThread } from '@/lib/chat/types'
import {
  clearWorktreeIntent,
  getWorktreeIntent,
  isWorktreeIntentEnabled
} from '@/lib/chat/worktree-intent'
import { createWorktree } from '@/lib/projects/api'
import { chatKeys, projectKeys } from '@/lib/query-keys'
import type { Project } from '@/lib/projects/types'

function resolveProject(
  queryClient: QueryClient,
  projectId: string | null | undefined
): Project | undefined {
  if (!projectId) {
    return undefined
  }

  const projects = queryClient.getQueryData<Project[]>(projectKeys.lists())
  return projects?.find((project) => project.id === projectId)
}

/**
 * If the chat has worktree intent enabled, create a worktree and switch the chat onto it.
 * Returns without changes when intent is off. Throws on provision failure.
 */
export async function provisionWorktreeForSendIfNeeded(
  queryClient: QueryClient,
  chatId: string
): Promise<void> {
  if (!isWorktreeIntentEnabled(chatId)) {
    return
  }

  const intent = getWorktreeIntent(chatId)
  const chat =
    queryClient.getQueryData<ChatThread>(chatKeys.detail(chatId)) ??
    queryClient.getQueryData<ChatThread[]>(chatKeys.lists())?.find((entry) => entry.id === chatId)

  const projectId = chat?.projectId
  if (!projectId) {
    throw new Error('Select a project before using a worktree.')
  }

  const project = resolveProject(queryClient, projectId)
  const workspace = await createWorktree(projectId, {
    baseBranch: project?.defaultBaseBranch ?? null,
    branchName: intent?.branchName.trim() || null
  })

  await queryClient.invalidateQueries({ queryKey: projectKeys.lists() })

  const updated = await updateChatWorkspace(chatId, { workspaceId: workspace.id })
  applyWorkspaceToChatCache(queryClient, chatId, updated.projectId, {
    id: updated.workspaceId ?? workspace.id,
    path: updated.workspacePath
  })

  clearWorktreeIntent(chatId)
}
