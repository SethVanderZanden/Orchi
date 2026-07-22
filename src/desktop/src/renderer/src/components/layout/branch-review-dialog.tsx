import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RefreshCw } from 'lucide-react'

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
import { NativeSelect } from '@/components/ui/native-select'
import type { ChatThread } from '@/lib/chat/types'
import { kickOffBranchReview, listProjectBranches } from '@/lib/projects/api'
import { chatKeys, projectBranchKeys } from '@/lib/query-keys'
import { useChat } from '@/providers/chat-context'
import { useChatTabs } from '@/providers/chat-tabs-provider'

type BranchReviewDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  projectId: string
  defaultBaseBranch: string
  preferredHeadBranch?: string | null
  onSuccess: (message: string) => void
  onError: (message: string) => void
}

export function BranchReviewDialog({
  open,
  onOpenChange,
  projectId,
  defaultBaseBranch,
  preferredHeadBranch,
  onSuccess,
  onError
}: BranchReviewDialogProps): React.JSX.Element {
  const queryClient = useQueryClient()
  const { sendMessage, loadChat } = useChat()
  const { openChat } = useChatTabs()
  const [headBranch, setHeadBranch] = useState('')
  const [baseBranch, setBaseBranch] = useState(defaultBaseBranch)
  const [fetchOnLoad, setFetchOnLoad] = useState(true)

  const branchesQuery = useQuery({
    queryKey: projectBranchKeys.list(projectId, fetchOnLoad),
    queryFn: () => listProjectBranches(projectId, { fetch: fetchOnLoad }),
    enabled: open,
    retry: false
  })

  const branchNames = useMemo(
    () => branchesQuery.data?.map((branch) => branch.name) ?? [],
    [branchesQuery.data]
  )

  useEffect(() => {
    if (!open) {
      return
    }

    setBaseBranch(defaultBaseBranch)
    setFetchOnLoad(true)
  }, [defaultBaseBranch, open])

  useEffect(() => {
    if (!open || branchNames.length === 0) {
      return
    }

    const preferred =
      preferredHeadBranch && branchNames.includes(preferredHeadBranch)
        ? preferredHeadBranch
        : (branchesQuery.data?.find((branch) => branch.isCurrent)?.name ?? branchNames[0] ?? '')

    setHeadBranch((current) => (current && branchNames.includes(current) ? current : preferred))

    setBaseBranch((current) => {
      if (current && branchNames.includes(current)) {
        return current
      }

      return branchNames.includes(defaultBaseBranch) ? defaultBaseBranch : (branchNames[0] ?? '')
    })
  }, [branchNames, branchesQuery.data, defaultBaseBranch, open, preferredHeadBranch])

  const submitMutation = useMutation({
    mutationFn: () =>
      kickOffBranchReview(projectId, {
        headBranch: headBranch.trim(),
        baseBranch: baseBranch.trim(),
        fetch: fetchOnLoad
      }),
    onSuccess: async (response) => {
      const reviewChat: ChatThread = {
        id: response.reviewChatId,
        title: `${response.headBranch} review`,
        preview: response.initialPrompt,
        updatedAt: new Date().toISOString(),
        agentId: 'cursor',
        projectId,
        workspaceId: null,
        workspacePath: '',
        mode: 'review',
        modelId: null,
        contextSizeId: null,
        reasoningEffortId: null,
        approvalPolicyId: null,
        parentChatId: null,
        planFilePath: response.reviewFilePath,
        status: 'inProgress',
        lastReadAt: null,
        messages: []
      }

      queryClient.setQueryData<ChatThread[]>(chatKeys.lists(), (current = []) => [
        reviewChat,
        ...current.filter((chat) => chat.id !== reviewChat.id)
      ])
      queryClient.setQueryData(chatKeys.detail(reviewChat.id), reviewChat)

      openChat(reviewChat.id)
      void loadChat(reviewChat.id)

      try {
        await sendMessage(reviewChat.id, response.kickoffMessage, { skipPostMessageBehavior: true })
        onSuccess(`Review started for ${response.headBranch} vs ${response.baseBranch}.`)
        onOpenChange(false)
      } catch (error) {
        onError(error instanceof Error ? error.message : 'Failed to send review kickoff.')
      }
    },
    onError: (error: Error) => onError(error.message)
  })

  const sameBranch =
    headBranch.trim().length > 0 &&
    baseBranch.trim().length > 0 &&
    headBranch.trim().toLowerCase() === baseBranch.trim().toLowerCase()

  const canSubmit =
    !submitMutation.isPending &&
    !branchesQuery.isFetching &&
    headBranch.trim().length > 0 &&
    baseBranch.trim().length > 0 &&
    !sameBranch

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Review branch</DialogTitle>
          <DialogDescription>
            Fetch branches, pick a head branch to review against a base, and Orchi opens a review
            chat with the branch diff.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="flex items-center justify-between gap-3">
            <p className="text-sm text-muted-foreground">
              {branchesQuery.isFetching
                ? 'Refreshing branches…'
                : `${branchNames.length} branch${branchNames.length === 1 ? '' : 'es'} available`}
            </p>
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-8 gap-1.5 px-2.5 text-xs font-normal"
              disabled={branchesQuery.isFetching || submitMutation.isPending}
              onClick={() => {
                setFetchOnLoad(true)
                void branchesQuery.refetch()
              }}
            >
              <RefreshCw className="size-3.5" />
              Fetch &amp; refresh
            </Button>
          </div>

          {branchesQuery.isError ? (
            <div className="rounded-md border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
              {branchesQuery.error instanceof Error
                ? branchesQuery.error.message
                : 'Failed to list branches.'}
            </div>
          ) : null}

          <div className="space-y-2">
            <Label htmlFor="branch-review-head">Head branch (to review)</Label>
            <NativeSelect
              id="branch-review-head"
              value={headBranch}
              onChange={(change) => setHeadBranch(change.target.value)}
              disabled={submitMutation.isPending || branchNames.length === 0}
            >
              {branchNames.map((branch) => (
                <option key={`head-${branch}`} value={branch}>
                  {branch}
                </option>
              ))}
            </NativeSelect>
          </div>

          <div className="space-y-2">
            <Label htmlFor="branch-review-base">Base branch</Label>
            <NativeSelect
              id="branch-review-base"
              value={baseBranch}
              onChange={(change) => setBaseBranch(change.target.value)}
              disabled={submitMutation.isPending || branchNames.length === 0}
            >
              {branchNames.map((branch) => (
                <option key={`base-${branch}`} value={branch}>
                  {branch}
                </option>
              ))}
            </NativeSelect>
          </div>

          {sameBranch ? (
            <p className="text-sm text-destructive">Head and base branches must be different.</p>
          ) : null}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={submitMutation.isPending}
          >
            Cancel
          </Button>
          <Button onClick={() => submitMutation.mutate()} disabled={!canSubmit}>
            {submitMutation.isPending ? 'Starting review…' : 'Start review'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
