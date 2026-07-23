import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronDown, GitCommit } from 'lucide-react'
import { toast } from 'sonner'

import { GitCommitDialog } from '@/components/layout/git-commit-dialog'
import { GitPullRequestDialog } from '@/components/layout/git-pull-request-dialog'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from '@/components/ui/dropdown-menu'
import { requestOpenBranchReview } from '@/lib/branch-review/events'
import { getGitHostReadiness } from '@/lib/git/api'
import {
  getCreatePullRequestDisabledReason,
  getPrimaryGitActionLabel,
  isGitActionsDisabled
} from '@/lib/git/git-action-labels'
import type { GitCommitDialogMode, GitHostProvider } from '@/lib/git/types'
import { gitActionKeys } from '@/lib/query-keys'

type ChatGitActionsMenuProps = {
  chatId: string
  projectId: string | null
  workspacePath: string
  defaultBaseBranch: string
  gitHostProvider: GitHostProvider
  workspaceBranch: string | null
}

type ActiveDialog = 'commit' | 'pullRequest' | null

export function ChatGitActionsMenu({
  chatId,
  projectId,
  workspacePath,
  defaultBaseBranch,
  gitHostProvider,
  workspaceBranch
}: ChatGitActionsMenuProps): React.JSX.Element {
  const [error, setError] = useState<string | null>(null)
  const [activeDialog, setActiveDialog] = useState<ActiveDialog>(null)
  const [commitMode, setCommitMode] = useState<GitCommitDialogMode>('commitAndPush')

  const disabled = isGitActionsDisabled(workspacePath)

  const readinessQuery = useQuery({
    queryKey: gitActionKeys.readiness(projectId, workspacePath, gitHostProvider),
    queryFn: () =>
      getGitHostReadiness({
        projectId: projectId ?? undefined,
        workspacePath,
        provider: gitHostProvider
      }),
    enabled: !disabled && projectId != null
  })

  const pullRequestDisabledReason = getCreatePullRequestDisabledReason({
    projectId,
    readiness: readinessQuery.data
  })

  function openCommitDialog(mode: GitCommitDialogMode): void {
    setError(null)
    setCommitMode(mode)
    setActiveDialog('commit')
  }

  function openPullRequestDialog(): void {
    if (pullRequestDisabledReason) {
      return
    }

    setError(null)
    setActiveDialog('pullRequest')
  }

  function openBranchReviewDialog(): void {
    if (!projectId) {
      toast.error('Open a project chat to review a branch.')
      return
    }

    setError(null)
    requestOpenBranchReview({ projectId })
  }

  function handleGitSuccess(message: string): void {
    setError(null)
    toast.success(message)
  }

  function handleGitError(message: string): void {
    setError(message)
    toast.error(message)
  }

  function handlePullRequestSuccess(message: string, pullRequestUrl: string | null): void {
    handleGitSuccess(message)
    if (pullRequestUrl) {
      window.open(pullRequestUrl, '_blank', 'noopener,noreferrer')
    }
  }

  return (
    <>
      <div className="flex flex-col items-end gap-1">
        <div className="inline-flex -space-x-px">
          <Button
            variant="outline"
            size="sm"
            disabled={disabled}
            className="h-8 rounded-r-none gap-1.5 px-3 text-xs font-normal"
            aria-label={getPrimaryGitActionLabel()}
            onClick={() => openCommitDialog('commitAndPush')}
          >
            <GitCommit className="size-3.5" />
            {getPrimaryGitActionLabel()}
          </Button>
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                disabled={disabled}
                className="h-8 rounded-l-none px-2"
                aria-label="More git actions"
              >
                <ChevronDown className="size-3.5 opacity-60" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openCommitDialog('commit')}>Commit</DropdownMenuItem>
              <DropdownMenuItem
                disabled={pullRequestDisabledReason != null}
                title={pullRequestDisabledReason ?? undefined}
                onClick={openPullRequestDialog}
              >
                Create pull request
              </DropdownMenuItem>
              <DropdownMenuItem
                disabled={projectId == null}
                title={projectId == null ? 'Select a project chat first.' : undefined}
                onClick={openBranchReviewDialog}
              >
                Review branch…
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
        {error ? <p className="max-w-48 truncate text-[10px] text-destructive">{error}</p> : null}
      </div>

      <GitCommitDialog
        open={activeDialog === 'commit'}
        onOpenChange={(open) => setActiveDialog(open ? 'commit' : null)}
        mode={commitMode}
        chatId={chatId}
        onSuccess={handleGitSuccess}
        onError={handleGitError}
      />

      {projectId ? (
        <GitPullRequestDialog
          open={activeDialog === 'pullRequest'}
          onOpenChange={(open) => setActiveDialog(open ? 'pullRequest' : null)}
          chatId={chatId}
          projectId={projectId}
          workspacePath={workspacePath}
          defaultBaseBranch={defaultBaseBranch}
          gitHostProvider={gitHostProvider}
          headBranch={workspaceBranch}
          onSuccess={handlePullRequestSuccess}
          onError={handleGitError}
        />
      ) : null}
    </>
  )
}
