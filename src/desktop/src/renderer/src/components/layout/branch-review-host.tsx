import { useEffect, useMemo, useState } from 'react'
import { toast } from 'sonner'

import { BranchReviewDialog } from '@/components/layout/branch-review-dialog'
import {
  OPEN_BRANCH_REVIEW_EVENT,
  type OpenBranchReviewDetail
} from '@/lib/branch-review/events'
import { resolveBranchReviewProjectId } from '@/lib/branch-review/resolve-project-id'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'
import { useProjects } from '@/providers/project-provider'

export function BranchReviewHost(): React.JSX.Element | null {
  const [open, setOpen] = useState(false)
  const [requestedProjectId, setRequestedProjectId] = useState<string | null>(null)
  const { getChat } = useChat()
  const { activeTabId } = useChatTabs()
  const { projects } = useProjects()

  const activeChat = activeTabId ? getChat(activeTabId) : null
  const projectIds = useMemo(() => projects.map((project) => project.id), [projects])

  const projectId = resolveBranchReviewProjectId({
    requestedProjectId,
    activeProjectId: activeChat?.projectId ?? null,
    projectIds
  })

  const project = projects.find((entry) => entry.id === projectId)
  const workspace =
    project?.workspaces.find((entry) => entry.id === activeChat?.workspaceId) ??
    project?.workspaces.find((entry) => entry.isDefault) ??
    project?.workspaces[0]

  useEffect(() => {
    function handleOpenBranchReview(event: Event): void {
      const detail = (event as CustomEvent<OpenBranchReviewDetail>).detail
      const nextProjectId = resolveBranchReviewProjectId({
        requestedProjectId: detail?.projectId ?? null,
        activeProjectId: activeChat?.projectId ?? null,
        projectIds
      })

      if (!nextProjectId) {
        toast.error(
          projects.length === 0
            ? 'Add a project before reviewing a branch.'
            : 'Open a project chat to review a branch.'
        )
        return
      }

      setRequestedProjectId(detail?.projectId?.trim() || nextProjectId)
      setOpen(true)
    }

    window.addEventListener(OPEN_BRANCH_REVIEW_EVENT, handleOpenBranchReview)
    return () => window.removeEventListener(OPEN_BRANCH_REVIEW_EVENT, handleOpenBranchReview)
  }, [activeChat?.projectId, projectIds, projects.length])

  function handleOpenChange(nextOpen: boolean): void {
    setOpen(nextOpen)
    if (!nextOpen) {
      setRequestedProjectId(null)
    }
  }

  if (!projectId || !project) {
    return null
  }

  return (
    <BranchReviewDialog
      open={open}
      onOpenChange={handleOpenChange}
      projectId={projectId}
      defaultBaseBranch={project.defaultBaseBranch || 'main'}
      preferredHeadBranch={workspace?.branch ?? null}
      onSuccess={(message) => toast.success(message)}
      onError={(message) => toast.error(message)}
    />
  )
}
