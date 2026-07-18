import { useState } from 'react'
import { useMutation, useQuery } from '@tanstack/react-query'

import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { getGitHostReadiness, runChatGitAction } from '@/lib/git/api'
import {
  formatDefaultPullRequestBody,
  formatDefaultPullRequestTitle
} from '@/lib/git/git-action-labels'
import type { GitHostProvider } from '@/lib/git/types'
import { gitActionKeys } from '@/lib/query-keys'

type GitPullRequestDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  chatId: string
  projectId: string
  workspacePath: string
  defaultBaseBranch: string
  gitHostProvider: GitHostProvider
  headBranch?: string | null
  onSuccess: (message: string, pullRequestUrl: string | null) => void
  onError: (message: string) => void
}

export function GitPullRequestDialog({
  open,
  onOpenChange,
  chatId,
  projectId,
  workspacePath,
  defaultBaseBranch,
  gitHostProvider,
  headBranch,
  onSuccess,
  onError
}: GitPullRequestDialogProps): React.JSX.Element {
  const [title, setTitle] = useState(formatDefaultPullRequestTitle(headBranch))
  const [body, setBody] = useState(formatDefaultPullRequestBody())
  const [targetBranch, setTargetBranch] = useState(defaultBaseBranch)

  const readinessQuery = useQuery({
    queryKey: gitActionKeys.readiness(projectId, workspacePath, gitHostProvider),
    queryFn: () =>
      getGitHostReadiness({
        projectId,
        workspacePath,
        provider: gitHostProvider
      }),
    enabled: open
  })

  function handleOpenChange(nextOpen: boolean): void {
    if (nextOpen) {
      setTitle(formatDefaultPullRequestTitle(headBranch))
      setBody(formatDefaultPullRequestBody())
      setTargetBranch(defaultBaseBranch)
    }
    onOpenChange(nextOpen)
  }

  const submitMutation = useMutation({
    mutationFn: () =>
      runChatGitAction(chatId, {
        action: 'createPullRequest',
        pullRequestTitle: title.trim(),
        pullRequestBody: body.trim(),
        targetBranch: targetBranch.trim()
      }),
    onSuccess: (response) => {
      if (!response.succeeded) {
        const failedStep = response.steps.find((step) => !step.succeeded)
        onError(failedStep?.output ?? 'Pull request creation failed.')
        return
      }

      const lastStep = response.steps[response.steps.length - 1]
      onSuccess(lastStep?.output ?? 'Pull request created.', response.pullRequestUrl)
      handleOpenChange(false)
    },
    onError: (error: Error) => onError(error.message)
  })

  const readiness = readinessQuery.data
  const hostNotReady = readiness != null && readiness.status !== 'ready'
  const titleInvalid = !title.trim()
  const targetBranchInvalid = !targetBranch.trim()

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Create pull request</DialogTitle>
          <DialogDescription>
            Open a pull request for the current workspace branch using your configured git host.
          </DialogDescription>
        </DialogHeader>

        {hostNotReady ? (
          <div className="rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
            {readiness.message}
          </div>
        ) : null}

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="git-pr-title">Title</Label>
            <Input
              id="git-pr-title"
              value={title}
              onChange={(change) => setTitle(change.target.value)}
              disabled={submitMutation.isPending}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="git-pr-body">Body</Label>
            <Textarea
              id="git-pr-body"
              value={body}
              onChange={(change) => setBody(change.target.value)}
              disabled={submitMutation.isPending}
              rows={4}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="git-pr-target-branch">Target branch</Label>
            <Input
              id="git-pr-target-branch"
              value={targetBranch}
              onChange={(change) => setTargetBranch(change.target.value)}
              disabled={submitMutation.isPending}
            />
          </div>
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => handleOpenChange(false)}
            disabled={submitMutation.isPending}
          >
            Cancel
          </Button>
          <Button
            onClick={() => submitMutation.mutate()}
            disabled={
              submitMutation.isPending || hostNotReady || titleInvalid || targetBranchInvalid
            }
          >
            {submitMutation.isPending ? 'Creating pull request…' : 'Create pull request'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
