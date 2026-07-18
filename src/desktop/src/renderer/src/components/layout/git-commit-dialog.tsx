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
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import { runChatGitAction, getSuggestedCommitMessage } from '@/lib/git/api'
import { getCommitDialogSubmitLabel } from '@/lib/git/git-action-labels'
import type { GitCommitDialogMode } from '@/lib/git/types'
import { gitActionKeys } from '@/lib/query-keys'

type GitCommitDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  mode: GitCommitDialogMode
  chatId: string
  onSuccess: (message: string) => void
  onError: (message: string) => void
}

export function GitCommitDialog({
  open,
  onOpenChange,
  mode,
  chatId,
  onSuccess,
  onError
}: GitCommitDialogProps): React.JSX.Element {
  const [message, setMessage] = useState('')
  const [generateFromDiff, setGenerateFromDiff] = useState(true)

  const suggestedQuery = useQuery({
    queryKey: gitActionKeys.suggestedCommitMessage(chatId),
    queryFn: () => getSuggestedCommitMessage(chatId),
    enabled: open
  })

  const resolvedMessage = message || suggestedQuery.data || ''

  function handleOpenChange(nextOpen: boolean): void {
    if (nextOpen) {
      setMessage('')
      setGenerateFromDiff(true)
    }
    onOpenChange(nextOpen)
  }

  const submitMutation = useMutation({
    mutationFn: () =>
      runChatGitAction(chatId, {
        action: mode === 'commitAndPush' ? 'commitAndPush' : 'commit',
        generateCommitMessage: generateFromDiff,
        commitMessage: generateFromDiff ? null : resolvedMessage.trim()
      }),
    onSuccess: (response) => {
      if (!response.succeeded) {
        const failedStep = response.steps.find((step) => !step.succeeded)
        onError(failedStep?.output ?? 'Git action failed.')
        return
      }

      const lastStep = response.steps[response.steps.length - 1]
      onSuccess(lastStep?.output ?? 'Changes committed.')
      handleOpenChange(false)
    },
    onError: (error: Error) => onError(error.message)
  })

  const manualMessageInvalid = !generateFromDiff && !resolvedMessage.trim()
  const submitLabel = getCommitDialogSubmitLabel(mode)

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{submitLabel}</DialogTitle>
          <DialogDescription>
            {mode === 'commitAndPush'
              ? 'Commit your workspace changes and push the current branch.'
              : 'Commit your workspace changes without pushing.'}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="git-commit-message">Commit message</Label>
            <Textarea
              id="git-commit-message"
              value={resolvedMessage}
              onChange={(change) => setMessage(change.target.value)}
              placeholder={
                suggestedQuery.isLoading ? 'Loading suggested message…' : 'Enter a commit message'
              }
              disabled={generateFromDiff || submitMutation.isPending}
              rows={4}
            />
            {suggestedQuery.isError ? (
              <p className="text-xs text-destructive">{suggestedQuery.error.message}</p>
            ) : null}
          </div>

          <label className="flex items-start gap-2 text-sm">
            <input
              type="checkbox"
              className="mt-0.5"
              checked={generateFromDiff}
              onChange={(change) => setGenerateFromDiff(change.target.checked)}
              disabled={submitMutation.isPending}
            />
            <span>Generate from diff on submit</span>
          </label>
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
            disabled={submitMutation.isPending || manualMessageInvalid}
          >
            {submitMutation.isPending ? `${submitLabel}…` : submitLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
